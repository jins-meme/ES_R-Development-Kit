package com.jins_jp.meme.core.web

import java.io.File
import java.io.RandomAccessFile
import java.text.Normalizer
import java.util.zip.CRC32
import java.util.zip.DataFormatException
import java.util.zip.Inflater

/**
 * グラフ画面の zip（webview/BRIDGE.md の「Limits」）を安全に展開する。Mac の ZipExtractor.swift と同じ規則。
 *
 * 展開する前に zip の目次（セントラルディレクトリ）を全部読んで検査し、1 つでも通らなければ何も書かない:
 *  - パス: 空・絶対パス（/…、C:…）・「\」・制御文字・「.」「..」の要素・Windows で使えない名前 → zip slip を防ぐ
 *  - 重複: 同じ名前、大文字小文字・Unicode の正規化の違いだけの名前、ファイルとフォルダの同名
 *  - 種類: シンボリックリンク・暗号化・ZIP64・分割 zip・「無圧縮 / Deflate」以外の圧縮
 *  - 大きさ: ファイル数・1 ファイル・合計（宣言値）・圧縮率の上限 → zip 爆弾を防ぐ
 * 展開は java.util.zip.ZipFile に任せず自分で行い、宣言より多く出てきたら途中で止める。CRC-32 も照合する。
 */
object ZipExtractor {

    data class Limits(
        val maxZipBytes: Long = 100_000_000,
        val maxTotal: Long = 100_000_000,
        val maxEntry: Long = 50_000_000,
        val maxFiles: Int = 2000,
        val maxRatio: Long = 200,
        val maxNameBytes: Int = 255,
        val maxDepth: Int = 16,
    )

    class Failure(message: String) : Exception(message)

    private data class Entry(
        val name: String, val isDir: Boolean, val method: Int, val crc: Long,
        val csize: Long, val usize: Long, val localOffset: Long,
    )

    /** [zip] を [dir]（空のフォルダ）へ展開する。検査に通らなければ [Failure]（その前には何も書かない）。 */
    fun extract(zip: File, dir: File, limits: Limits = Limits()): Pair<Int, Long> {
        if (zip.length() > limits.maxZipBytes) {
            throw Failure("The zip is too large: ${zip.length() / 1_000_000} MB, limit ${limits.maxZipBytes / 1_000_000} MB.")
        }
        RandomAccessFile(zip, "r").use { f ->
            val entries = readDirectory(f, limits)
            var files = 0
            var total = 0L
            val base = dir.canonicalFile
            for (e in entries) {
                val out = File(base, e.name)
                // 念押し: 規則で弾いているので外へは出ないはず
                if (!out.canonicalPath.startsWith(base.path + File.separator)) throw Failure("The zip contains a path that is not allowed: ${e.name}")
                if (e.isDir) { out.mkdirs(); continue }
                out.parentFile?.mkdirs()
                val body = readBody(f, e)
                val crc = CRC32().apply { update(body) }.value
                if (crc != e.crc) throw Failure("The zip is damaged: ${e.name}: CRC mismatch.")
                if (out.exists()) throw Failure("The zip contains the same path twice: ${e.name}")
                out.writeBytes(body)
                files++
                total += e.usize
            }
            return files to total
        }
    }

    private fun readDirectory(f: RandomAccessFile, limits: Limits): List<Entry> {
        val len = f.length()
        if (len < 22) throw Failure("Not a zip file (too short).")
        // End of central directory（末尾から探す。コメントは最大 65535 バイト）
        val tailLen = minOf(len, 22L + 65535).toInt()
        val tail = ByteArray(tailLen)
        f.seek(len - tailLen); f.readFully(tail)
        var p = tailLen - 22
        while (p >= 0 && u32(tail, p) != 0x06054b50L) p--
        if (p < 0) throw Failure("Not a zip file (no end of central directory).")
        val eocd = len - tailLen + p
        val disk = u16(tail, p + 4); val cdDisk = u16(tail, p + 6)
        val nDisk = u16(tail, p + 8); val n = u16(tail, p + 10)
        val cdSize = u32(tail, p + 12); val cdOff = u32(tail, p + 16)
        if (disk != 0 || cdDisk != 0 || nDisk != n) throw Failure("The zip uses a feature this app does not accept: split zip.")
        if (n == 0xFFFF || cdSize == 0xFFFFFFFFL || cdOff == 0xFFFFFFFFL) throw Failure("The zip uses a feature this app does not accept: ZIP64.")
        if (n > limits.maxFiles) throw Failure("The zip is too large: $n entries, limit ${limits.maxFiles}.")
        if (cdOff + cdSize > eocd) throw Failure("The zip is damaged: central directory out of range.")
        val cd = ByteArray(cdSize.toInt())
        f.seek(cdOff); f.readFully(cd)

        val out = ArrayList<Entry>(n)
        val seen = HashSet<String>()
        val dirsNeeded = HashSet<String>()
        var total = 0L
        var q = 0
        repeat(n) {
            if (q + 46 > cd.size || u32(cd, q) != 0x02014b50L) throw Failure("The zip is damaged: central directory entry.")
            val madeBy = u16(cd, q + 4) shr 8
            val flags = u16(cd, q + 8); val method = u16(cd, q + 10)
            val crc = u32(cd, q + 16); val csize = u32(cd, q + 20); val usize = u32(cd, q + 24)
            val nameLen = u16(cd, q + 28); val extraLen = u16(cd, q + 30); val commentLen = u16(cd, q + 32)
            val ext = u32(cd, q + 38); val local = u32(cd, q + 42)
            if (q + 46 + nameLen + extraLen + commentLen > cd.size) throw Failure("The zip is damaged: central directory entry.")
            val raw = decodeUtf8(cd, q + 46, nameLen) ?: throw Failure("The zip contains a path that is not allowed: (not UTF-8)")
            q += 46 + nameLen + extraLen + commentLen

            if (flags and 0x0001 != 0 || flags and 0x0040 != 0) throw Failure("The zip uses a feature this app does not accept: encryption ($raw).")
            if (csize == 0xFFFFFFFFL || usize == 0xFFFFFFFFL || local == 0xFFFFFFFFL) throw Failure("The zip uses a feature this app does not accept: ZIP64 ($raw).")
            // Unix で作った zip は外部属性の上位 16 ビットが mode。S_IFLNK(0o120000)はシンボリックリンク
            if (madeBy == 3 && ((ext shr 16) and 0xF000L) == 0xA000L) throw Failure("The zip contains a path that is not allowed: $raw (symbolic link)")
            val isDir = raw.endsWith("/")
            val name = Normalizer.normalize(if (isDir) raw.dropLast(1) else raw, Normalizer.Form.NFC)
            checkName(name, raw, limits)
            if (isDir) {
                if (usize != 0L) throw Failure("The zip is damaged: $raw: directory with data.")
            } else {
                if (method != 0 && method != 8) throw Failure("The zip uses a feature this app does not accept: compression method $method ($raw).")
                if (method == 0 && csize != usize) throw Failure("The zip is damaged: $raw: stored size mismatch.")
                if (usize > limits.maxEntry) throw Failure("The zip is too large: $raw is ${usize / 1_000_000} MB, limit ${limits.maxEntry / 1_000_000} MB.")
                if (usize > 1_000_000 && usize > csize * limits.maxRatio) {
                    throw Failure("The zip is too large: $raw expands ${usize / maxOf(csize, 1)}×, limit ${limits.maxRatio}×.")
                }
                total += usize
                if (total > limits.maxTotal) throw Failure("The zip is too large: more than ${limits.maxTotal / 1_000_000} MB when extracted.")
            }
            val key = name.lowercase()
            if (!seen.add(key) && !(isDir && key in dirsNeeded)) throw Failure("The zip contains the same path twice: $raw")
            val parts = key.split("/")
            var acc = ""
            for (part in parts.dropLast(1)) {
                acc = if (acc.isEmpty()) part else "$acc/$part"
                dirsNeeded += acc; seen += acc
            }
            if (local + 30 > len) throw Failure("The zip is damaged: $raw: local header out of range.")
            out += Entry(name, isDir, method, crc, csize, usize, local)
        }
        for (e in out) if (!e.isDir && e.name.lowercase() in dirsNeeded) throw Failure("The zip contains the same path twice: ${e.name} (file and folder)")
        return out
    }

    private val reserved = setOf("con", "prn", "aux", "nul") + (1..9).flatMap { listOf("com$it", "lpt$it") }

    /** zip の中のパスとして許すか（3 アプリで同じ規則。webview/BRIDGE.md の Limits） */
    fun checkName(name: String, raw: String = name, limits: Limits = Limits()) {
        fun bad(): Nothing = throw Failure("The zip contains a path that is not allowed: $raw")
        if (name.isEmpty() || name.toByteArray().size > limits.maxNameBytes) bad()
        if (name.startsWith("/") || name.contains('\\') || name.contains(':')) bad()
        if (name.any { it.code < 0x20 || it.code == 0x7F }) bad()
        val parts = name.split("/")
        if (parts.size > limits.maxDepth) bad()
        for (p in parts) {
            if (p.isEmpty() || p == "." || p == "..") bad()
            if (p.endsWith(".") || p.endsWith(" ")) bad()
            if (p.substringBefore('.').lowercase() in reserved) bad()
        }
    }

    private fun readBody(f: RandomAccessFile, e: Entry): ByteArray {
        val h = ByteArray(30)
        f.seek(e.localOffset); f.readFully(h)
        if (u32(h, 0) != 0x04034b50L) throw Failure("The zip is damaged: ${e.name}: local header.")
        val nameLen = u16(h, 26); val extraLen = u16(h, 28)
        val nameBytes = ByteArray(nameLen); f.readFully(nameBytes)
        // 目次と中身の見出しで名前が違う zip は受けない（展開の道具によって違う名前で書かれるのを避ける）
        val local = Normalizer.normalize(decodeUtf8(nameBytes, 0, nameLen) ?: "", Normalizer.Form.NFC)
        if (local != e.name && local != e.name + "/") throw Failure("The zip is damaged: ${e.name}: names differ.")
        val start = e.localOffset + 30 + nameLen + extraLen
        if (start + e.csize > f.length()) throw Failure("The zip is damaged: ${e.name}: data out of range.")
        val comp = ByteArray(e.csize.toInt())
        f.seek(start); f.readFully(comp)
        if (e.method == 0) return comp
        return inflate(comp, e.usize.toInt(), e.name)
    }

    /** 生の Deflate を展開する。[expected] を 1 バイトでも超えたら止める */
    private fun inflate(src: ByteArray, expected: Int, name: String): ByteArray {
        val out = ByteArray(expected + 1)
        val inf = Inflater(true)
        try {
            inf.setInput(src)
            var n = 0
            while (n < out.size && !inf.finished()) {
                val k = try { inf.inflate(out, n, out.size - n) } catch (e: DataFormatException) {
                    throw Failure("The zip is damaged: $name: could not be decompressed.")
                }
                if (k == 0 && (inf.needsInput() || inf.needsDictionary())) break
                n += k
            }
            if (n > expected) throw Failure("The zip is too large: $name expands beyond its declared size.")
            if (n != expected) throw Failure("The zip is damaged: $name: could not be decompressed.")
            return out.copyOf(expected)
        } finally {
            inf.end()
        }
    }

    private fun decodeUtf8(b: ByteArray, off: Int, len: Int): String? {
        val dec = Charsets.UTF_8.newDecoder()
        return try { dec.decode(java.nio.ByteBuffer.wrap(b, off, len)).toString() } catch (e: java.nio.charset.CharacterCodingException) { null }
    }

    private fun u16(b: ByteArray, i: Int): Int = (b[i].toInt() and 0xFF) or ((b[i + 1].toInt() and 0xFF) shl 8)
    private fun u32(b: ByteArray, i: Int): Long =
        (u16(b, i).toLong()) or (u16(b, i + 2).toLong() shl 16)
}
