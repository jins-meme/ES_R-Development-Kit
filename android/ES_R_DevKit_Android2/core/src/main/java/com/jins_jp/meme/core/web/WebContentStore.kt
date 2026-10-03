package com.jins_jp.meme.core.web

import android.content.Context
import android.net.Uri
import org.json.JSONObject
import java.io.File
import java.security.MessageDigest
import java.util.UUID

/**
 * グラフ画面（WebView）の中身の置き場。中身は zip で、アプリに同梱した標準版（assets/webview/standard.zip）か、
 * 設定で選んだ zip（高機能版など）を展開して使う。仕様は DevKit の webview/README.md・BRIDGE.md。Mac の
 * WebContentStore.swift と同じ作り。
 *
 *  - 展開先: filesDir/webcontent/{bundled,custom}/
 *  - 同梱の標準版は、zip の中身が変わったとき（SHA-256 で見る）だけ展開し直す。
 *  - 選んだ zip は [ZipExtractor] で検査しながら一時フォルダへ展開し、manifest.json・bridgeApi・入口を確かめてから
 *    差し替える。通らなければ元のまま。前の中身は新しいものを置けてから消す。
 */
class WebContentStore(private val context: Context) {

    /** runInBackground: アプリが裏に回っても(画面オフなど)サンプルを送り続けてほしいページ(BRIDGE.md の Running in the background) */
    data class Manifest(val name: String, val title: String?, val version: String, val bridgeApi: Int, val entry: String,
                        val runInBackground: Boolean = false) {
        val displayName: String get() = "${title ?: name} $version"
    }

    enum class Source { Bundled, Custom }

    private val prefs = context.getSharedPreferences("webcontent", Context.MODE_PRIVATE)
    val root = File(context.filesDir, "webcontent").apply { mkdirs() }
    private val bundledDir get() = File(root, "bundled")
    private val customDir get() = File(root, "custom")

    var source = Source.Bundled
        private set
    var manifest: Manifest? = null
        private set

    /** 今使う中身のフォルダ（仮想ホストの根） */
    val activeDir: File get() = if (source == Source.Custom) customDir else bundledDir

    init { prepare() }

    fun prepare() {
        runCatching { extractBundledIfNeeded() }.onFailure { android.util.Log.w(TAG, "bundled: $it") }
        val wanted = prefs.getString(KEY_SOURCE, Source.Bundled.name)
        val custom = if (wanted == Source.Custom.name) runCatching { readManifest(customDir) }.getOrNull() else null
        if (custom != null) { source = Source.Custom; manifest = custom }
        else { source = Source.Bundled; manifest = runCatching { readManifest(bundledDir) }.getOrNull() }
    }

    private fun extractBundledIfNeeded() {
        val data = context.assets.open(BUNDLED_ASSET).use { it.readBytes() }
        val hash = MessageDigest.getInstance("SHA-256").digest(data).joinToString("") { "%02x".format(it) }
        val mark = File(root, "bundled.sha256")
        if (mark.exists() && mark.readText() == hash && bundledDir.exists()) return
        val zip = File(context.cacheDir, "standard-${UUID.randomUUID()}.zip")
        try {
            zip.writeBytes(data)
            val tmp = extractAndValidate(zip)
            replace(bundledDir, tmp)
            mark.writeText(hash)
        } finally {
            zip.delete()
        }
    }

    /** SAF で選んだ zip を取り込む（上限を超えたらコピーの途中でやめる）。 */
    fun importZip(uri: Uri): Manifest {
        val zip = File(context.cacheDir, "import-${UUID.randomUUID()}.zip")
        try {
            val limit = ZipExtractor.Limits().maxZipBytes
            context.contentResolver.openInputStream(uri)?.use { ins ->
                zip.outputStream().use { out ->
                    val buf = ByteArray(1 shl 16)
                    var n = 0L
                    while (true) {
                        val k = ins.read(buf); if (k < 0) break
                        n += k
                        if (n > limit) throw ZipExtractor.Failure("The zip is too large: more than ${limit / 1_000_000} MB.")
                        out.write(buf, 0, k)
                    }
                }
            } ?: throw ZipExtractor.Failure("The file could not be opened.")
            return importZip(zip)
        } finally {
            zip.delete()
        }
    }

    /** zip を取り込んで「選んだ zip」に切り替える。検査に通らなければ投げ、今の中身はそのまま。 */
    fun importZip(zip: File): Manifest {
        val tmp = extractAndValidate(zip)
        replace(customDir, tmp)
        val m = readManifest(customDir)
        source = Source.Custom; manifest = m
        prefs.edit().putString(KEY_SOURCE, Source.Custom.name).apply()
        return m
    }

    /** 同梱の標準版に戻す（取り込んだ zip のフォルダは消す）。 */
    fun useBundled() {
        customDir.deleteRecursively()
        source = Source.Bundled
        manifest = runCatching { readManifest(bundledDir) }.getOrNull()
        prefs.edit().putString(KEY_SOURCE, Source.Bundled.name).apply()
    }

    private fun extractAndValidate(zip: File): File {
        val tmp = File(root, "tmp-${UUID.randomUUID()}")
        tmp.mkdirs()
        try {
            ZipExtractor.extract(zip, tmp)
            val m = readManifest(tmp)
            if (m.bridgeApi != BRIDGE_API) throw ZipExtractor.Failure("This zip needs bridge API ${m.bridgeApi}, but this app supports $BRIDGE_API.")
            val entryOk = runCatching { ZipExtractor.checkName(m.entry) }.isSuccess && File(tmp, m.entry).isFile
            if (!entryOk) throw ZipExtractor.Failure("The entry page ${m.entry} is missing.")
            return tmp
        } catch (e: Exception) {
            tmp.deleteRecursively()
            throw e
        }
    }

    private fun replace(dest: File, tmp: File) {
        val old = File(root, "old-${UUID.randomUUID()}")
        val hadOld = dest.exists()
        if (hadOld && !dest.renameTo(old)) throw ZipExtractor.Failure("Could not replace the current page.")
        if (!tmp.renameTo(dest)) {
            if (hadOld) old.renameTo(dest)
            tmp.deleteRecursively()
            throw ZipExtractor.Failure("Could not replace the current page.")
        }
        if (hadOld) old.deleteRecursively()
    }

    companion object {
        private const val TAG = "WebContentStore"
        const val BRIDGE_API = 1
        const val BUNDLED_ASSET = "webview/standard.zip"
        private const val KEY_SOURCE = "source"

        fun readManifest(dir: File): Manifest {
            val f = File(dir, "manifest.json")
            if (!f.isFile) throw ZipExtractor.Failure("manifest.json was not found at the top of the zip.")
            if (f.length() > 64_000) throw ZipExtractor.Failure("manifest.json could not be read. too large")
            val o = try { JSONObject(f.readText()) } catch (e: Exception) {
                throw ZipExtractor.Failure("manifest.json could not be read. ${e.message}")
            }
            fun str(k: String): String? = if (o.has(k) && !o.isNull(k)) o.optString(k) else null
            val m = Manifest(
                name = str("name") ?: throw ZipExtractor.Failure("manifest.json could not be read. name is missing"),
                title = str("title"),
                version = str("version") ?: throw ZipExtractor.Failure("manifest.json could not be read. version is missing"),
                bridgeApi = if (o.has("bridgeApi")) o.optInt("bridgeApi", -1) else throw ZipExtractor.Failure("manifest.json could not be read. bridgeApi is missing"),
                entry = str("entry") ?: throw ZipExtractor.Failure("manifest.json could not be read. entry is missing"),
                runInBackground = o.optBoolean("runInBackground", false),
            )
            // 設定画面にそのまま出すので、短く・制御文字なし
            for ((k, v) in listOf("name" to m.name, "title" to (m.title ?: ""), "version" to m.version)) {
                if (v.length > 64 || v.any { it.code < 0x20 || it.code == 0x7F }) {
                    throw ZipExtractor.Failure("manifest.json could not be read. $k must be at most 64 characters without control characters")
                }
            }
            return m
        }
    }
}
