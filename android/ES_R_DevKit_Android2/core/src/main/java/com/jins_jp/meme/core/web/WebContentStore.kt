package com.jins_jp.meme.core.web

import android.content.Context
import android.net.Uri
import org.json.JSONObject
import java.io.File
import java.security.MessageDigest
import java.util.UUID

/**
 * グラフ画面（WebView）の中身（Display Engine）の置き場。中身は zip で、アプリに同梱した標準版（assets/webview/standard.zip）と、
 * Display Engine ダイアログで取り込んだ zip（高機能版など、いくつでも）を展開して持っておき、そのうち 1 つだけを使う。
 * 仕様は DevKit の webview/README.md・BRIDGE.md。Mac の WebContentStore.swift と同じ作り。
 *
 *  - 展開先: filesDir/webcontent/
 *      bundled/          同梱の標準版。消せない（ID は "standard"）
 *      zips/<ID>/        取り込んだ zip。ID は取り込んだときに振る UUID
 *      zips/<ID>.sha256  取り込んだ zip ファイルの SHA-256（同じファイルを 2 度取り込まないため）
 *      zips/<ID>.order   一覧の並び（取り込んだ時刻。ファイルの作成時刻は取れないため）
 *    使っている 1 つの ID は SharedPreferences "webcontent" の active。
 *  - 同梱の標準版は、zip の中身が変わったとき（SHA-256 で見る）だけ展開し直す。
 *  - 取り込む zip は [ZipExtractor] で検査しながら一時フォルダへ展開し、manifest.json・bridgeApi・入口を確かめてから置く。
 *    通らなければ何も変わらない。前の中身は新しいものを置けてから消す。
 *  - 同じファイル（SHA-256 が同じ）をもう一度取り込んでも何もしない（一覧に重ならない）。
 *  - manifest の name が同じ zip を取り込んだら、新しい版として置き換える（ID・一覧での位置・使っているかどうかはそのまま）。
 *  - 以前の版の custom/（選んだ zip を 1 つだけ持てた）は、起動時に zips/ の 1 つへ移す。
 *  - 取り込んだ zip が落ちる場合の保護（[fallBackToBuiltIn]）: zip のページは GPU ドライバの不具合などでアプリごと落とすことがある
 *    （Android の WebView は GPU の処理をアプリのプロセスの中で動かす）。使う中身は保存されるので、そのままだと起動のたびに落ちて
 *    Display Engine を開いて戻すこともできない。前回の終わり方がネイティブのクラッシュで、その時に取り込んだ zip を使っていたら、
 *    起動時に標準版へ戻して知らせる（[takeFallbackNotice]）。グラフ画面のプロセスだけが続けて落ちたときは WebBridge が戻す。
 */
class WebContentStore(private val context: Context) {

    /** runInBackground: アプリが裏に回っても(画面オフなど)サンプルを送り続けてほしいページ(BRIDGE.md の Running in the background) */
    data class Manifest(val name: String, val title: String?, val version: String, val bridgeApi: Int, val entry: String,
                        val runInBackground: Boolean = false) {
        val displayName: String get() = "${title ?: name} $version"
    }

    /** 持っている中身 1 つ（Display Engine ダイアログの 1 行） */
    data class Entry(val id: String, val manifest: Manifest) {
        val isBuiltIn: Boolean get() = id == BUILT_IN_ID
    }

    enum class AddOutcome { Added, Replaced, AlreadyAdded }

    /** [add] の結果。[replacedActive] が true なら、使っている中身が変わったのでグラフ画面を読み込み直すこと */
    data class AddResult(val entry: Entry, val outcome: AddOutcome, val replacedActive: Boolean)

    private val prefs = context.getSharedPreferences("webcontent", Context.MODE_PRIVATE)
    val root = File(context.filesDir, "webcontent").apply { mkdirs() }
    private val bundledDir get() = File(root, "bundled")
    private val zipsDir get() = File(root, "zips")
    /** 以前の版の「選んだ zip」の置き場（移したら無くなる） */
    private val legacyCustomDir get() = File(root, "custom")

    private fun dirOf(id: String) = if (id == BUILT_IN_ID) bundledDir else File(zipsDir, id)
    private fun hashFileOf(id: String) = File(zipsDir, "$id.sha256")
    private fun orderFileOf(id: String) = File(zipsDir, "$id.order")

    /** 持っている中身。先頭が同梱の標準版、あとは取り込んだ順 */
    @Volatile var entries: List<Entry> = emptyList()
        private set
    /** 使っている中身の ID */
    @Volatile var activeId: String = BUILT_IN_ID
        private set

    /** 今使う中身の manifest */
    val manifest: Manifest? get() = entries.firstOrNull { it.id == activeId }?.manifest

    /** 今使う中身のフォルダ（仮想ホストの根） */
    val activeDir: File get() = dirOf(activeId)

    /** 標準版へ戻したときの知らせ([takeFallbackNotice])。init で入れるので init より前に置く */
    @Volatile private var fallbackNotice: String? = null

    // 前回の終わり方を見るのはプロセスが始まったときの 1 回だけ(prepare は自己テストの片付けでも呼ばれ、
    // その時は「読み込み中」の印が残っていても落ちたわけではない)
    init { prepare(); checkLastExit() }

    /** 起動時: 同梱の標準版を必要なら展開し、持っている中身を読み、設定で有効になっている 1 つを使う。 */
    @Synchronized
    fun prepare() {
        zipsDir.mkdirs()
        runCatching { extractBundledIfNeeded() }.onFailure { android.util.Log.w(TAG, "bundled: $it") }
        dropLeftovers()
        migrateLegacyCustom()
        reloadEntries()
        val wanted = prefs.getString(KEY_ACTIVE, BUILT_IN_ID)
        activeId = if (entries.any { it.id == wanted }) wanted!! else BUILT_IN_ID
        if (activeId != wanted) prefs.edit().putString(KEY_ACTIVE, activeId).apply()
    }

    // ---- 取り込んだ zip が落ちる場合の保護 ----

    /** 標準版へ戻したときの知らせ(1 度だけ返す)。ViewModel が画面に出す */
    fun takeFallbackNotice(): String? = fallbackNotice.also { fallbackNotice = null }

    /**
     * 使っている取り込んだ zip をやめて標準版へ戻す(落ちる zip から抜けるため)。戻したら true。
     * [why] は知らせの文の後半(例: "crashed the app")。
     */
    @Synchronized
    fun fallBackToBuiltIn(why: String): Boolean {
        if (activeId == BUILT_IN_ID) return false
        val name = manifest?.displayName ?: activeId
        activate(BUILT_IN_ID)
        fallbackNotice = "$name $why, so the built-in Standard is used now. You can choose it again in Display Engine."
        android.util.Log.w(TAG, "fall back to built-in: $name $why")
        return true
    }

    /**
     * 取り込んだ zip のページを読み込み始めた(WebBridge.load)。[markEngineStable] まで「読み込み中」の印を残す。
     * 落ちた直後に OS がアプリをすぐ開き直すと、落ちたプロセスの終わり方(ApplicationExitInfo)がまだ記録されていないことがあるので、
     * その場合はこの印で落ちたと見る([checkLastExit])。
     */
    fun markEngineLoading() {
        if (activeId != BUILT_IN_ID) prefs.edit().putLong(KEY_LOADING_SINCE, System.currentTimeMillis()).commit()
    }

    /** ページが読み込めてしばらく落ちなかった(WebBridge が ready の後に呼ぶ)。印を消す */
    fun markEngineStable() {
        if (prefs.contains(KEY_LOADING_SINCE)) prefs.edit().remove(KEY_LOADING_SINCE).apply()
    }

    /**
     * 前回このアプリのプロセスがネイティブのクラッシュで終わっていて、その時に取り込んだ zip を使っていたら標準版へ戻す。
     *  - その zip を有効にした時刻より後の終わり方だけを見る(前に別の理由で落ちたものを、新しく選んだ zip のせいにしない)。
     *    同じ終わり方は 1 度だけ見る。Java の例外で落ちたものはアプリの不具合なので見ない。
     *  - 終わり方の記録がまだ無く、ページを読み込み中の印が残っていたら、落ちてすぐ OS に開き直されたと見て戻す
     *    (Pixel 10 Pro XL で、落ちた直後の自動の開き直しでは記録が間に合わず、同じ zip でもう 1 度落ちていた)。
     *    ユーザーが閉じた・OS が止めたときは記録が残るので、こちらには来ない。
     */
    private fun checkLastExit() {
        val loadingSince = prefs.getLong(KEY_LOADING_SINCE, 0L)
        prefs.edit().remove(KEY_LOADING_SINCE).apply()
        if (activeId == BUILT_IN_ID) return
        val am = context.getSystemService(android.app.ActivityManager::class.java) ?: return
        val last = runCatching { am.getHistoricalProcessExitReasons(context.packageName, 0, 5) }.getOrNull()
            ?.firstOrNull { it.processName == context.packageName }
        val handled = prefs.getLong(KEY_HANDLED_EXIT, 0L)
        val activatedAt = prefs.getLong(KEY_ACTIVATED_AT, 0L)
        if (last != null && last.timestamp > handled) {
            prefs.edit().putLong(KEY_HANDLED_EXIT, last.timestamp).apply()
            if (last.reason == android.app.ApplicationExitInfo.REASON_CRASH_NATIVE && last.timestamp > activatedAt) {
                fallBackToBuiltIn("crashed the app")
            }
            return
        }
        if (loadingSince > 0L && loadingSince >= activatedAt) fallBackToBuiltIn("crashed the app")
    }

    /** 途中で止まった取り込み・置き換えの残り（tmp-* / old-*）を消す。起動時は何も取り込んでいないので、残っていれば全部ゴミ。 */
    private fun dropLeftovers() {
        root.listFiles { f -> f.name.startsWith("tmp-") || f.name.startsWith("old-") }?.forEach { it.deleteRecursively() }
    }

    /** zips/ の中を読み直す。manifest を読めないフォルダ（途中で止まった取り込みの残りなど）は消す。 */
    private fun reloadEntries() {
        val list = mutableListOf<Entry>()
        runCatching { readManifest(bundledDir) }.getOrNull()?.let { list += Entry(BUILT_IN_ID, it) }
        val imported = mutableListOf<Pair<Long, Entry>>()
        for (d in zipsDir.listFiles { f -> f.isDirectory } ?: emptyArray()) {
            val m = runCatching { readManifest(d) }.getOrNull()
            if (m == null) {
                android.util.Log.w(TAG, "drop unreadable ${d.name}")
                d.deleteRecursively(); hashFileOf(d.name).delete(); orderFileOf(d.name).delete()
                continue
            }
            val order = runCatching { orderFileOf(d.name).readText().trim().toLong() }.getOrDefault(d.lastModified())
            imported += order to Entry(d.name, m)
        }
        list += imported.sortedBy { it.first }.map { it.second }
        entries = list
    }

    /** 以前の版の custom/ を zips/ の 1 つへ移す。使っていたなら、移した先を使う設定にする。 */
    private fun migrateLegacyCustom() {
        val legacy = prefs.getString(KEY_LEGACY_SOURCE, null)
        if (legacy != null) prefs.edit().remove(KEY_LEGACY_SOURCE).apply()
        if (!legacyCustomDir.exists()) return
        val id = UUID.randomUUID().toString()
        if (legacyCustomDir.renameTo(dirOf(id))) {
            orderFileOf(id).writeText(System.currentTimeMillis().toString())
            if (legacy == "Custom") prefs.edit().putString(KEY_ACTIVE, id).apply()
        } else {
            android.util.Log.w(TAG, "migrate custom: rename failed")
        }
    }

    private fun extractBundledIfNeeded() {
        val data = context.assets.open(BUNDLED_ASSET).use { it.readBytes() }
        val hash = sha256(data)
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

    // ---- 取り込み・有効化・削除（Display Engine ダイアログ） ----

    /** SAF で選んだ zip を取り込む（上限を超えたらコピーの途中でやめる）。有効にはしない。 */
    fun add(uri: Uri): AddResult {
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
            return add(zip)
        } finally {
            zip.delete()
        }
    }

    /**
     * zip を検査して取り込み、一覧に足す（有効にはしない）。検査に通らなければ投げ、何も変わらない。
     *  - 同じファイルを既に取り込んでいれば何もしない（[AddOutcome.AlreadyAdded]。一覧に重ならない）。
     *  - manifest の name が同じものを持っていれば、それを置き換える（[AddOutcome.Replaced]。ID・位置・使っているかどうかはそのまま）。
     */
    @Synchronized
    fun add(zip: File): AddResult {
        val hash = sha256(zip)
        entries.firstOrNull { !it.isBuiltIn && runCatching { hashFileOf(it.id).readText() }.getOrNull() == hash }
            ?.let { return AddResult(it, AddOutcome.AlreadyAdded, false) }
        val tmp = extractAndValidate(zip)
        val m = readManifest(tmp)
        val existing = entries.firstOrNull { !it.isBuiltIn && it.manifest.name == m.name }?.id
        val id = existing ?: UUID.randomUUID().toString()
        replace(dirOf(id), tmp)
        hashFileOf(id).writeText(hash)
        // 一覧は取り込んだ順。置き換えても並びが変わらないよう、前の時刻のままにする
        if (existing == null || !orderFileOf(id).exists()) orderFileOf(id).writeText(System.currentTimeMillis().toString())
        reloadEntries()
        val entry = entries.firstOrNull { it.id == id } ?: throw ZipExtractor.Failure("manifest.json was not found at the top of the zip.")
        return AddResult(entry, if (existing == null) AddOutcome.Added else AddOutcome.Replaced, existing != null && id == activeId)
    }

    /** 使う中身を切り替える（1 つだけ）。呼んだ側でグラフ画面を読み込み直すこと。 */
    @Synchronized
    fun activate(id: String) {
        if (entries.none { it.id == id }) return
        activeId = id
        // 有効にした時刻(これより前の落ち方を、この zip のせいにしないため。checkLastExit)
        prefs.edit().putString(KEY_ACTIVE, id).putLong(KEY_ACTIVATED_AT, System.currentTimeMillis()).apply()
    }

    /** 取り込んだ zip を消す。同梱の標準版は消せない。使っていたものを消したら標準版に戻す（戻り値 true。グラフ画面を読み込み直すこと）。 */
    @Synchronized
    fun remove(id: String): Boolean {
        if (id == BUILT_IN_ID || entries.none { it.id == id }) return false
        val wasActive = id == activeId
        if (wasActive) activate(BUILT_IN_ID)
        if (!dirOf(id).deleteRecursively()) android.util.Log.w(TAG, "could not delete all of $id")
        hashFileOf(id).delete(); orderFileOf(id).delete()
        reloadEntries()
        return wasActive
    }

    /** 取り込んで、すぐ使う（自己テスト用の近道） */
    fun addAndActivate(zip: File): Manifest {
        val r = add(zip)
        activate(r.entry.id)
        return r.entry.manifest
    }

    // ---- 自己テスト用（DebugAutoTest） ----

    /** 取り込んだ zip 全部と使っている ID を、ほかの場所へ写す（自己テストが取り込む前に。同じ name だと置き換えるため）。写せなければ null */
    internal fun copyImportedAside(): Pair<File, String>? {
        val dst = File(context.cacheDir, "autotest-zips-${UUID.randomUUID()}")
        return if (runCatching { zipsDir.copyRecursively(dst) }.getOrDefault(false)) dst to activeId
        else { dst.deleteRecursively(); null }
    }

    /** [copyImportedAside] で写したものを zips/ へ戻し、使っていた ID に戻す */
    @Synchronized
    internal fun restoreImported(kept: Pair<File, String>) {
        zipsDir.deleteRecursively()
        if (!kept.first.renameTo(zipsDir)) {
            runCatching { kept.first.copyRecursively(zipsDir, overwrite = true) }
            kept.first.deleteRecursively()
        }
        prefs.edit().putString(KEY_ACTIVE, kept.second).apply()
        prepare()
    }

    /** 自己テスト用: 以前の版の形（custom/ と source = "Custom"）を作る。dir の中身を custom/ へ写す */
    internal fun plantLegacyCustom(dir: File) {
        dir.copyRecursively(legacyCustomDir, overwrite = true)
        prefs.edit().putString(KEY_LEGACY_SOURCE, "Custom").remove(KEY_ACTIVE).apply()
    }

    internal fun legacyLeft(): Boolean = legacyCustomDir.exists() || prefs.contains(KEY_LEGACY_SOURCE)

    internal fun setActiveForTest(id: String) = prefs.edit().putString(KEY_ACTIVE, id).apply()

    internal val bundledDirForTest: File get() = bundledDir

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
        if (hadOld && !dest.renameTo(old)) {
            tmp.deleteRecursively()   // 前の中身をどけられなければ、展開したものも残さない
            throw ZipExtractor.Failure("Could not replace the current page.")
        }
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
        /** 同梱の標準版の ID（規定。消せない） */
        const val BUILT_IN_ID = "standard"
        private const val KEY_ACTIVE = "active"
        private const val KEY_ACTIVATED_AT = "activatedAt"
        private const val KEY_HANDLED_EXIT = "handledExit"
        private const val KEY_LOADING_SINCE = "engineLoadingSince"
        /** 以前の版の "Bundled" / "Custom"。新しい形へ移したら消す */
        private const val KEY_LEGACY_SOURCE = "source"

        private fun sha256(data: ByteArray): String =
            MessageDigest.getInstance("SHA-256").digest(data).joinToString("") { "%02x".format(it) }

        private fun sha256(file: File): String {
            val md = MessageDigest.getInstance("SHA-256")
            file.inputStream().use { ins ->
                val buf = ByteArray(1 shl 16)
                while (true) { val k = ins.read(buf); if (k < 0) break; md.update(buf, 0, k) }
            }
            return md.digest().joinToString("") { "%02x".format(it) }
        }

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
