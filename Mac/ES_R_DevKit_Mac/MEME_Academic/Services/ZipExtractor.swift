//
//  ZipExtractor.swift
//  MEME_Academic
//
//  グラフ画面の zip(webview/BRIDGE.md の「Limits」)を安全に展開する。
//
//  展開する前に zip の目次(セントラルディレクトリ)を全部読んで検査し、1 つでも通らなければ何も書かない:
//    - パス: 空・絶対パス(/…、C:…)・「\」・NUL や制御文字・「.」「..」の要素・Windows で使えない名前
//            (CON など、末尾の「.」や空白、「:」)→ zip slip(展開先の外への書き込み)を防ぐ
//    - 重複: 同じ名前、大文字小文字・Unicode の正規化(NFC/NFD)の違いだけの名前、ファイルとフォルダの同名
//    - 種類: シンボリックリンク(外部属性)・暗号化・ZIP64・分割 zip・圧縮方式が「無圧縮 / Deflate」以外
//    - 大きさ: ファイル数・1 ファイル・合計(宣言値)の上限、圧縮率の上限 → zip 爆弾を防ぐ
//  展開は /usr/bin/ditto に任せず自分で行う。宣言された大きさを超えて出てきたら途中で止める(大きさを偽った zip 爆弾)。
//  CRC-32 も照合する。
//

import Foundation
import Compression

enum ZipExtractor {

    struct Limits {
        var maxZipBytes: Int64 = 100_000_000       // zip ファイルそのもの
        var maxTotal: Int64 = 100_000_000          // 展開後の合計
        var maxEntry: Int64 = 50_000_000           // 1 ファイル
        var maxFiles = 2000
        var maxRatio: Int64 = 200                  // 展開後 / 圧縮後(1 MB を超えるファイルだけ見る)
        var maxNameBytes = 255
        var maxDepth = 16
    }

    enum Failure: LocalizedError {
        case notZip(String)
        case unsupported(String)
        case unsafeName(String)
        case duplicate(String)
        case tooLarge(String)
        case corrupt(String)

        var errorDescription: String? {
            switch self {
            case .notZip(let s): return "Not a zip file (\(s))."
            case .unsupported(let s): return "The zip uses a feature this app does not accept: \(s)."
            case .unsafeName(let s): return "The zip contains a path that is not allowed: \(s)"
            case .duplicate(let s): return "The zip contains the same path twice: \(s)"
            case .tooLarge(let s): return "The zip is too large: \(s)."
            case .corrupt(let s): return "The zip is damaged: \(s)."
            }
        }
    }

    struct Entry {
        let name: String            // NFC
        let isDir: Bool
        let method: UInt16
        let crc: UInt32
        let csize: Int64
        let usize: Int64
        let localOffset: Int
    }

    /// zip を dir(空のフォルダ)へ展開する。検査に通らなければ投げる(その前には何も書かない)。
    static func extract(_ zip: URL, to dir: URL, limits: Limits = Limits()) throws -> (files: Int, bytes: Int64) {
        let size = (try? FileManager.default.attributesOfItem(atPath: zip.path)[.size] as? Int64) ?? 0
        if size > limits.maxZipBytes { throw Failure.tooLarge("\(size / 1_000_000) MB, limit \(limits.maxZipBytes / 1_000_000) MB") }
        let data = try Data(contentsOf: zip, options: .mappedIfSafe)
        let entries = try readDirectory(data, limits: limits)
        var files = 0
        var total: Int64 = 0
        let fm = FileManager.default
        for e in entries {
            let url = dir.appendingPathComponent(e.name, isDirectory: e.isDir)
            if e.isDir {
                try fm.createDirectory(at: url, withIntermediateDirectories: true)
                continue
            }
            try fm.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
            let body = try readBody(data, e)
            if Int64(body.count) != e.usize { throw Failure.corrupt("\(e.name): size differs from the directory") }
            if crc32(body) != e.crc { throw Failure.corrupt("\(e.name): CRC mismatch") }
            // 作ったばかりの空のフォルダの中なので、既存のリンクを辿って外へ書くことはない(withoutOverwriting で念押し)
            try body.write(to: url, options: [.withoutOverwriting])
            files += 1
            total += e.usize
        }
        return (files, total)
    }

    // MARK: - 目次

    static func readDirectory(_ d: Data, limits: Limits) throws -> [Entry] {
        guard d.count >= 22 else { throw Failure.notZip("too short") }
        // End of central directory(末尾から探す。コメントは最大 65535 バイト)
        var eocd = -1
        let lo = max(0, d.count - 22 - 65535)
        var p = d.count - 22
        while p >= lo { if u32(d, p) == 0x0605_4b50 { eocd = p; break }; p -= 1 }
        guard eocd >= 0 else { throw Failure.notZip("no end of central directory") }
        let disk = u16(d, eocd + 4), cdDisk = u16(d, eocd + 6)
        let nDisk = Int(u16(d, eocd + 8)), n = Int(u16(d, eocd + 10))
        let cdSize = Int(u32(d, eocd + 12)), cdOff = Int(u32(d, eocd + 16))
        if disk != 0 || cdDisk != 0 || nDisk != n { throw Failure.unsupported("split zip") }
        if n == 0xFFFF || cdSize == 0xFFFF_FFFF || cdOff == 0xFFFF_FFFF { throw Failure.unsupported("ZIP64") }
        if n > limits.maxFiles { throw Failure.tooLarge("\(n) entries, limit \(limits.maxFiles)") }
        guard cdOff + cdSize <= eocd else { throw Failure.corrupt("central directory out of range") }

        var out: [Entry] = []
        var seen = Set<String>()           // 大文字小文字・正規化をそろえた名前(ファイルとフォルダ)
        var dirsNeeded = Set<String>()     // ファイルの親フォルダ(同名のファイルがあってはならない)
        var total: Int64 = 0
        var q = cdOff
        for _ in 0..<n {
            guard q + 46 <= eocd, u32(d, q) == 0x0201_4b50 else { throw Failure.corrupt("central directory entry") }
            let madeBy = u16(d, q + 4) >> 8
            let flags = u16(d, q + 8), method = u16(d, q + 10)
            let crc = u32(d, q + 16)
            let csize = Int64(u32(d, q + 20)), usize = Int64(u32(d, q + 24))
            let nameLen = Int(u16(d, q + 28)), extraLen = Int(u16(d, q + 30)), commentLen = Int(u16(d, q + 32))
            let ext = u32(d, q + 38), local = Int(u32(d, q + 42))
            guard q + 46 + nameLen + extraLen + commentLen <= eocd else { throw Failure.corrupt("central directory entry") }
            let raw = d.subdata(in: (q + 46)..<(q + 46 + nameLen))
            q += 46 + nameLen + extraLen + commentLen

            guard let name0 = String(data: raw, encoding: .utf8) else { throw Failure.unsafeName("(not UTF-8)") }
            if flags & 0x0001 != 0 || flags & 0x0040 != 0 { throw Failure.unsupported("encryption (\(name0))") }
            if csize == 0xFFFF_FFFF || usize == 0xFFFF_FFFF || local == 0xFFFF_FFFF { throw Failure.unsupported("ZIP64 (\(name0))") }
            // Unix で作った zip は外部属性の上位 16 ビットが mode。S_IFLNK(0o120000)はシンボリックリンク
            if madeBy == 3, (ext >> 16) & 0o170000 == 0o120000 { throw Failure.unsafeName("\(name0) (symbolic link)") }
            let isDir = name0.hasSuffix("/")
            let name = String(isDir ? name0.dropLast() : Substring(name0)).precomposedStringWithCanonicalMapping
            try checkName(name, raw: name0, limits: limits)
            if isDir {
                if usize != 0 { throw Failure.corrupt("\(name0): directory with data") }
            } else {
                if method != 0 && method != 8 { throw Failure.unsupported("compression method \(method) (\(name0))") }
                if method == 0 && csize != usize { throw Failure.corrupt("\(name0): stored size mismatch") }
                if usize > limits.maxEntry { throw Failure.tooLarge("\(name0) is \(usize / 1_000_000) MB, limit \(limits.maxEntry / 1_000_000) MB") }
                if usize > 1_000_000 && usize > csize * limits.maxRatio {
                    throw Failure.tooLarge("\(name0) expands \(usize / max(csize, 1))×, limit \(limits.maxRatio)×")
                }
                total += usize
                if total > limits.maxTotal { throw Failure.tooLarge("more than \(limits.maxTotal / 1_000_000) MB when extracted") }
            }
            let key = name.lowercased()
            if !seen.insert(key).inserted {
                if !(isDir && dirsNeeded.contains(key)) { throw Failure.duplicate(name0) }   // フォルダは「ファイルの親」として先に数えたもの
            }
            // 親のフォルダはファイルであってはならない(a と a/b)
            var parts = key.split(separator: "/").map(String.init)
            parts.removeLast()
            var acc = ""
            for part in parts {
                acc = acc.isEmpty ? part : acc + "/" + part
                dirsNeeded.insert(acc)
                seen.insert(acc)
            }
            guard local + 30 <= d.count else { throw Failure.corrupt("\(name0): local header out of range") }
            out.append(Entry(name: name, isDir: isDir, method: method, crc: crc, csize: csize, usize: usize, localOffset: local))
        }
        // ファイルとして出てくる名前が、別のファイルの親フォルダになっていないか
        for e in out where !e.isDir && dirsNeeded.contains(e.name.lowercased()) {
            throw Failure.duplicate("\(e.name) (file and folder)")
        }
        return out
    }

    private static let reserved: Set<String> = ["con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"]

    /// zip の中のパスとして許すか(3 アプリで同じ規則。webview/BRIDGE.md の Limits)
    static func checkName(_ name: String, raw: String, limits: Limits) throws {
        let bad = { throw Failure.unsafeName(raw) }
        if name.isEmpty || name.utf8.count > limits.maxNameBytes { try bad() }
        if name.hasPrefix("/") || name.contains("\\") || name.contains(":") { try bad() }
        if name.unicodeScalars.contains(where: { $0.value < 0x20 || $0.value == 0x7F }) { try bad() }
        let parts = name.split(separator: "/", omittingEmptySubsequences: false)
        if parts.count > limits.maxDepth { try bad() }
        for p in parts {
            if p.isEmpty || p == "." || p == ".." { try bad() }
            if p.hasSuffix(".") || p.hasSuffix(" ") { try bad() }
            let stem = p.split(separator: ".", maxSplits: 1).first.map { String($0).lowercased() } ?? ""
            if reserved.contains(stem) { try bad() }
        }
    }

    // MARK: - 中身

    private static func readBody(_ d: Data, _ e: Entry) throws -> Data {
        let h = e.localOffset
        guard u32(d, h) == 0x0403_4b50 else { throw Failure.corrupt("\(e.name): local header") }
        let nameLen = Int(u16(d, h + 26)), extraLen = Int(u16(d, h + 28))
        let start = h + 30 + nameLen + extraLen
        guard start + Int(e.csize) <= d.count else { throw Failure.corrupt("\(e.name): data out of range") }
        // 目次と中身の見出しで名前が違う zip は受けない(展開の道具によって違う名前で書かれるのを避ける)
        let local = String(data: d.subdata(in: (h + 30)..<(h + 30 + nameLen)), encoding: .utf8)?
            .precomposedStringWithCanonicalMapping ?? ""
        guard local == e.name || local == e.name + "/" else { throw Failure.corrupt("\(e.name): names differ") }
        let comp = d.subdata(in: start..<(start + Int(e.csize)))
        if e.method == 0 { return comp }
        return try inflate(comp, expected: Int(e.usize), name: e.name)
    }

    /// 生の Deflate を展開する。expected を 1 バイトでも超えたら止める
    private static func inflate(_ src: Data, expected: Int, name: String) throws -> Data {
        if expected == 0 { return Data() }
        var out = Data(count: expected + 1)          // 1 バイト余分に取り、超えたことを見分ける
        let n: Int = out.withUnsafeMutableBytes { (dst: UnsafeMutableRawBufferPointer) -> Int in
            src.withUnsafeBytes { (s: UnsafeRawBufferPointer) -> Int in
                compression_decode_buffer(dst.bindMemory(to: UInt8.self).baseAddress!, expected + 1,
                                          s.bindMemory(to: UInt8.self).baseAddress!, src.count, nil, COMPRESSION_ZLIB)
            }
        }
        if n > expected { throw Failure.tooLarge("\(name) expands beyond its declared size") }
        if n != expected { throw Failure.corrupt("\(name): could not be decompressed") }
        out.count = n
        return out
    }

    // MARK: - 小物

    private static func u16(_ d: Data, _ i: Int) -> UInt16 {
        guard i >= 0, i + 2 <= d.count else { return 0 }
        return UInt16(d[d.startIndex + i]) | UInt16(d[d.startIndex + i + 1]) << 8
    }
    private static func u32(_ d: Data, _ i: Int) -> UInt32 {
        guard i >= 0, i + 4 <= d.count else { return 0 }
        let b = d.startIndex + i
        return UInt32(d[b]) | UInt32(d[b + 1]) << 8 | UInt32(d[b + 2]) << 16 | UInt32(d[b + 3]) << 24
    }

    private static let crcTable: [UInt32] = (0..<256).map { i -> UInt32 in
        var c = UInt32(i)
        for _ in 0..<8 { c = c & 1 != 0 ? 0xEDB8_8320 ^ (c >> 1) : c >> 1 }
        return c
    }
    static func crc32(_ d: Data) -> UInt32 {
        var c: UInt32 = 0xFFFF_FFFF
        d.withUnsafeBytes { (p: UnsafeRawBufferPointer) in
            for b in p { c = crcTable[Int((c ^ UInt32(b)) & 0xFF)] ^ (c >> 8) }
        }
        return c ^ 0xFFFF_FFFF
    }
}
