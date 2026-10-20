//
//  Gzip.swift
//  MEME_Academic
//
//  gzip（RFC 1952）形式の圧縮・展開。CSV を .csv.gz として保存／読み込みするために使う。
//  zlib は macOS SDK に system module として入っているので、追加の依存は無い。
//

import Foundation
import zlib

enum GzipError: Error {
    /// zlib が返したエラーコード付き。
    case compressFailed(Int32)
    case decompressFailed(Int32)
}

enum Gzip {

    /// 一度に zlib と受け渡しするバッファサイズ。
    private static let chunkSize = 64 * 1024

    /// gzip の magic number（1F 8B）で始まるか。拡張子ではなく中身で判定したいときに使う。
    static func isGzipped(_ data: Data) -> Bool {
        guard data.count >= 2 else { return false }
        let start = data.startIndex
        return data[start] == 0x1F && data[start + 1] == 0x8B
    }

    /// data を gzip 形式の1メンバーへ圧縮する。空なら空を返す。
    ///
    /// 連結して使える（gzip は複数メンバーの連結を1ファイルとして許す）ことを前提に、
    /// 呼び出し側は計測中の書き足しごとに1メンバーを append していく。
    /// CsvManager の説明も参照。
    static func compress(_ data: Data, level: Int32 = Z_DEFAULT_COMPRESSION) throws -> Data {
        guard !data.isEmpty else { return Data() }

        var stream = z_stream()
        // windowBits に 16 を足すと zlib ヘッダではなく gzip ヘッダ／トレーラを付ける。
        let status = deflateInit2_(&stream, level, Z_DEFLATED, 15 + 16, 8, Z_DEFAULT_STRATEGY,
                                   ZLIB_VERSION, Int32(MemoryLayout<z_stream>.size))
        guard status == Z_OK else { throw GzipError.compressFailed(status) }
        defer { deflateEnd(&stream) }

        var output = Data()
        var buffer = [UInt8](repeating: 0, count: chunkSize)

        try data.withUnsafeBytes { (raw: UnsafeRawBufferPointer) in
            guard let base = raw.bindMemory(to: UInt8.self).baseAddress else { return }
            stream.next_in = UnsafeMutablePointer(mutating: base)
            stream.avail_in = uInt(data.count)

            while true {
                let result: Int32 = buffer.withUnsafeMutableBufferPointer { out in
                    stream.next_out = out.baseAddress
                    stream.avail_out = uInt(chunkSize)
                    return deflate(&stream, Z_FINISH)
                }
                output.append(contentsOf: buffer[0..<(chunkSize - Int(stream.avail_out))])
                if result == Z_STREAM_END { break }
                // Z_OK は「出力バッファが埋まったので続きがある」。それ以外は異常。
                guard result == Z_OK else { throw GzipError.compressFailed(result) }
            }
        }
        return output
    }

    /// gzip 形式のデータを展開する。連結された複数メンバーもまとめて展開する。
    ///
    /// 末尾が壊れていても（書き込み中にアプリが落ちた等）、完結しているメンバーまでを返す。
    /// 長時間計測のログが1バイトの欠けで全部読めなくなるのを避けるため。
    static func decompress(_ data: Data) throws -> Data {
        guard !data.isEmpty else { return Data() }

        var stream = z_stream()
        // windowBits に 32 を足すと gzip / zlib のヘッダを自動判別する。
        let status = inflateInit2_(&stream, 15 + 32, ZLIB_VERSION, Int32(MemoryLayout<z_stream>.size))
        guard status == Z_OK else { throw GzipError.decompressFailed(status) }
        defer { inflateEnd(&stream) }

        var output = Data()
        /// 直近の Z_STREAM_END 時点の出力長。壊れた末尾を切り捨てて返すために控える。
        var completedCount = 0
        var buffer = [UInt8](repeating: 0, count: chunkSize)

        try data.withUnsafeBytes { (raw: UnsafeRawBufferPointer) in
            guard let base = raw.bindMemory(to: UInt8.self).baseAddress else { return }
            stream.next_in = UnsafeMutablePointer(mutating: base)
            stream.avail_in = uInt(data.count)

            while true {
                let result: Int32 = buffer.withUnsafeMutableBufferPointer { out in
                    stream.next_out = out.baseAddress
                    stream.avail_out = uInt(chunkSize)
                    return inflate(&stream, Z_NO_FLUSH)
                }
                output.append(contentsOf: buffer[0..<(chunkSize - Int(stream.avail_out))])

                if result == Z_STREAM_END {
                    completedCount = output.count
                    // 続きが無ければ完了。あれば次のメンバーとして読み直す。
                    if stream.avail_in == 0 { return }
                    let reset = inflateReset(&stream)
                    guard reset == Z_OK else { throw GzipError.decompressFailed(reset) }
                    continue
                }
                if result == Z_OK { continue }

                // ここに来るのは末尾が欠けている／後ろにゴミが付いている場合。
                // 完結したメンバーが1つでもあればそこまでを結果として返す。
                guard completedCount > 0 else { throw GzipError.decompressFailed(result) }
                output = Data(output.prefix(completedCount))
                return
            }
        }
        return output
    }
}
