package com.jins_jp.meme.core.web

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Handler
import android.os.Looper
import android.provider.DocumentsContract
import android.provider.DocumentsContract.Document
import android.util.Log
import org.json.JSONArray
import org.json.JSONObject
import java.util.concurrent.Executors

/**
 * グラフ画面の日別キャッシュ(Logic4 などの 15 秒窓 + マーカー)を、ページの求めで一覧・読み・書きする
 * (ページ → アプリの cacheList / cacheRead / cacheWrite、返りは jmasHost.cacheResult。仕様は webview/BRIDGE.md の Cache files)。
 *
 * 置き場所は `<データフォルダ>/logic4-cache/<device>/<YYYY-MM-DD>.json`(データフォルダ = Download/ESR Logger)。
 * PC と Syncthing で同期する前提(python-processing-core docs/porting-html/advanced2-v2/index.md §5)。
 *
 * **読み書きは SAF(Storage Access Framework)のフォルダの許可で行う。** データの CSV は MediaStore で書いているが、MediaStore では
 * アプリが自分で作ったファイルしか見えず(Android 11 以降)、Syncthing が PC から運んできたファイルを読めない・上書きできないため。
 * 利用者が設定から一度だけ「Download/ESR Logger」(か、その中の logic4-cache)を選び、その許可を覚えておく([setTree])。
 * 許可が無いとき・取り消されたときは `error: "no_access"` を返す(ページは「設定でフォルダを選んで」と出す)。全ファイルへのアクセス権は使わない。
 *
 * 書くときは `<name>.tmp` に全部書いてから `<name>` へ名前を変える(Syncthing に書きかけを渡さない)。SAF の名前の変更は
 * 同じ名前があると「name (1)」に変えてしまうので、先に古い `<name>` を消してから変える(消して変えるまでの一瞬だけ、ファイルが無い)。
 * このフォルダの外は読み書きしない(device / name は形を決めて検査し、子を名前で探すだけで、パスはつながない)。
 *
 * 入出力は 1 本の別スレッドで順に行い(受信中は 15 秒ごとに書くので UI スレッドを止めない・同じファイルへの書き込みを重ねない)、
 * 結果はメインスレッドで [done] に返す。
 */
class CacheStore(private val context: Context) {

    private val main = Handler(Looper.getMainLooper())
    private val io = Executors.newSingleThreadExecutor { r -> Thread(r, "logic4-cache").apply { isDaemon = true } }
    private val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
    private val resolver get() = context.contentResolver

    /** 選ばれたフォルダ(許可が今も有効なときだけ) */
    val tree: Uri?
        get() {
            val u = prefs.getString(KEY_TREE, null)?.let(Uri::parse) ?: return null
            val ok = resolver.persistedUriPermissions.any { it.uri == u && it.isReadPermission && it.isWritePermission }
            return if (ok) u else null
        }

    /** 設定で選んだフォルダを覚える(読み書きの許可を持ち続ける)。前のフォルダの許可は返す */
    fun setTree(uri: Uri) {
        val flags = Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_GRANT_WRITE_URI_PERMISSION
        resolver.takePersistableUriPermission(uri, flags)
        val old = prefs.getString(KEY_TREE, null)?.let(Uri::parse)
        if (old != null && old != uri) runCatching { resolver.releasePersistableUriPermission(old, flags) }
        prefs.edit().putString(KEY_TREE, uri.toString()).apply()
    }

    /** 設定に出す文字(選んだフォルダの名前。未設定 / 許可切れは null) */
    fun folderLabel(): String? {
        val t = tree ?: return null
        val root = DocumentsContract.getTreeDocumentId(t)
        return root.substringAfter(':', root).ifEmpty { root }
    }

    /** ページからのメッセージ(kind = cacheList / cacheRead / cacheWrite)。結果は id を付けてメインスレッドで返す */
    fun handle(o: JSONObject, done: (JSONObject) -> Unit) {
        val id = o.opt("id")
        val kind = o.optString("kind")
        val device = o.optString("device")
        val name = o.optString("name")
        val text = if (kind == "cacheWrite") o.optString("text") else ""
        io.execute {
            val r = runCatching { run(kind, device, name, text) }.getOrElse {
                Log.w(TAG, "$kind $device/$name failed", it)
                err("io_error")
            }
            r.put("id", id)
            main.post { done(r) }
        }
    }

    private fun run(kind: String, device: String, name: String, text: String): JSONObject {
        if (!validDevice(device)) return err("bad_name")
        if (kind != "cacheList" && !validName(name)) return err("bad_name")
        val t = tree ?: return err("no_access")
        return when (kind) {
            "cacheList" -> {
                val dir = deviceDir(t, device, create = false) ?: return ok().put("files", JSONArray())
                val files = JSONArray()
                for (c in children(t, dir)) {
                    if (c.dir || !validName(c.name)) continue
                    files.put(JSONObject().put("name", c.name).put("size", c.size).put("mtime", c.mtime))
                }
                ok().put("files", files)
            }
            "cacheRead" -> {
                val dir = deviceDir(t, device, create = false) ?: return err("not_found")
                val f = children(t, dir).firstOrNull { !it.dir && it.name == name } ?: return err("not_found")
                if (f.size > MAX_BYTES) return err("too_large")
                val bytes = resolver.openInputStream(doc(t, f.id))?.use { it.readBytes() } ?: return err("io_error")
                if (bytes.size > MAX_BYTES) return err("too_large")
                ok().put("text", String(bytes, Charsets.UTF_8))
            }
            "cacheWrite" -> {
                val bytes = text.toByteArray(Charsets.UTF_8)
                if (bytes.size > MAX_BYTES) return err("too_large")
                val dir = deviceDir(t, device, create = true) ?: return err("io_error")
                write(t, dir, name, bytes)
            }
            else -> err("bad_kind")
        }
    }

    /** `<name>.tmp` に書いてから `<name>` へ(古い `<name>` は先に消す。SAF の名前の変更は同名を「name (1)」にするため) */
    private fun write(t: Uri, dir: String, name: String, bytes: ByteArray): JSONObject {
        val tmpName = "$name$TMP"
        var kids = children(t, dir)
        kids.firstOrNull { it.name == tmpName }?.let { DocumentsContract.deleteDocument(resolver, doc(t, it.id)) }
        // octet-stream にすると、プロバイダが名前の末尾に拡張子を足さない
        val tmp = DocumentsContract.createDocument(resolver, doc(t, dir), "application/octet-stream", tmpName) ?: return err("io_error")
        resolver.openOutputStream(tmp, "wt")?.use { it.write(bytes) } ?: return err("io_error")
        kids = children(t, dir)
        kids.firstOrNull { it.name == name && !it.dir }?.let { DocumentsContract.deleteDocument(resolver, doc(t, it.id)) }
        DocumentsContract.renameDocument(resolver, tmp, name)
        kids = children(t, dir)
        if (kids.none { it.name == name && !it.dir } || kids.any { it.name == tmpName }) {
            Log.w(TAG, "rename to $name did not take effect: ${kids.joinToString { it.name }}")
            return err("io_error")
        }
        return ok().put("size", bytes.size)
    }

    /** logic4-cache/<device> の文書 ID。選んだフォルダが logic4-cache ならその中、それ以外(ESR Logger)なら logic4-cache を探す / 作る */
    private fun deviceDir(t: Uri, device: String, create: Boolean): String? {
        val rootId = DocumentsContract.getTreeDocumentId(t)
        val rootName = rootId.substringAfterLast('/').substringAfterLast(':')
        val cache = if (rootName == CACHE_DIR) rootId else childDir(t, rootId, CACHE_DIR, create) ?: return null
        return childDir(t, cache, device, create)
    }

    private fun childDir(t: Uri, parent: String, name: String, create: Boolean): String? {
        children(t, parent).firstOrNull { it.dir && it.name == name }?.let { return it.id }
        if (!create) return null
        val u = DocumentsContract.createDocument(resolver, doc(t, parent), Document.MIME_TYPE_DIR, name) ?: return null
        return DocumentsContract.getDocumentId(u)
    }

    private data class Child(val id: String, val name: String, val dir: Boolean, val size: Long, val mtime: Long)

    private fun children(t: Uri, parent: String): List<Child> {
        val out = ArrayList<Child>()
        val cols = arrayOf(Document.COLUMN_DOCUMENT_ID, Document.COLUMN_DISPLAY_NAME, Document.COLUMN_MIME_TYPE,
            Document.COLUMN_SIZE, Document.COLUMN_LAST_MODIFIED)
        resolver.query(DocumentsContract.buildChildDocumentsUriUsingTree(t, parent), cols, null, null, null)?.use { c ->
            while (c.moveToNext()) {
                out += Child(c.getString(0), c.getString(1) ?: "", c.getString(2) == Document.MIME_TYPE_DIR,
                    if (c.isNull(3)) 0 else c.getLong(3), if (c.isNull(4)) 0 else c.getLong(4))
            }
        }
        return out
    }

    private fun doc(t: Uri, id: String) = DocumentsContract.buildDocumentUriUsingTree(t, id)

    companion object {
        private const val TAG = "CacheStore"
        private const val PREFS = "logic4_cache"
        private const val KEY_TREE = "tree"
        const val CACHE_DIR = "logic4-cache"
        private const val TMP = ".tmp"
        /** 1 ファイルの上限(バイト。BRIDGE.md) */
        const val MAX_BYTES = 16 * 1024 * 1024
        /** start の features・openReplay の features に入れる名前 */
        const val FEATURE = "cache"
        private val DEVICE = Regex("^[0-9A-F]{12}$")
        private val NAME = Regex("^\\d{4}-\\d{2}-\\d{2}\\.json$")

        fun validDevice(s: String) = DEVICE.matches(s)
        fun validName(s: String) = NAME.matches(s)

        /** 端末のアドレス(AA:BB:…)→ device(コロンなしの大文字 12 桁) */
        fun deviceOf(address: String): String = address.replace(":", "").uppercase()

        /** データの CSV 名の先頭(<MAC>_YYYYmmddHHMMSS…)→ device。取れなければ null */
        fun deviceOfFileName(name: String): String? =
            Regex("^([0-9A-Fa-f]{12})_").find(name)?.groupValues?.get(1)?.uppercase()

        /** 設定のフォルダ選びで最初に開く場所(Download/ESR Logger) */
        val INITIAL_URI: Uri
            get() = DocumentsContract.buildDocumentUri("com.android.externalstorage.documents", "primary:Download/ESR Logger")

        private fun ok() = JSONObject().put("ok", true)
        private fun err(code: String) = JSONObject().put("ok", false).put("error", code)
    }
}
