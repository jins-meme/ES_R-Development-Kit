package com.jins_jp.meme.core.ui.main

import com.jins_jp.meme.core.data.LocationFix
import com.jins_jp.meme.core.data.formatLocationArtifact
import com.jins_jp.meme.core.data.movedAtLeast
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch

// 計測中に大まかな現在地の取得をトリガーする間隔（計測開始時が 1 回目）。
internal const val LOCATION_INTERVAL_MS = 60_000L

// 前回記録した地点からこれだけ動いていたら記録する（緯度方向・経度方向のどちらか）。
// 止まっている間は同じ座標を毎分書かない＝ARTIFACT 列が位置情報で埋まらない。
internal const val LOCATION_MOVE_THRESHOLD_M = 50.0

/**
 * 計測中の大まかな位置の記録（設定「計測中の位置を記録」）。[LOCATION_INTERVAL_MS] ごとに現在地を取り、前回記録した
 * 地点から [LOCATION_MOVE_THRESHOLD_M] 以上動いていれば "lc:35.6802_139.7521" を**次に書くデータ行**の ARTIFACT 列へ
 * 載せる（[takePendingArtifact] で受け取る）。停止時にまとめて統合していた頃と違い、CSV を読み直さないので長時間計測でも
 * 停止が重くならない。
 *
 * 測位は別コルーチンで走るが、載せるのも受け取るのも [scope] の既定ディスパッチャ(Main)上なので、受け渡しに排他は要らない。
 * [sample] は現在地を 1 回取る（取れなければ null）、[onRecorded] は記録した文字列をグラフ画面へ印として出す。
 */
internal class LocationRecorder(
    private val scope: CoroutineScope,
    private val ui: StateFlow<MainUiState>,
    private val sample: suspend () -> LocationFix?,
    private val onRecorded: (String) -> Unit,
) {
    private var job: Job? = null

    // 最後に ARTIFACT 列へ書いた地点。次の測位がここから離れた時だけ記録する。
    private var lastFix: LocationFix? = null

    // 次に書くデータ行の ARTIFACT 列へ載せる位置情報（1 行受け取ったら空に戻る）。
    private var pending: String? = null

    /** 取得のループを（張り直して）始める。設定が OFF なら何もしない。1 回目はすぐ取る */
    fun start() {
        stop()
        reset()
        if (!ui.value.locationLogging) return
        job = scope.launch {
            while (isActive) {
                // 取得自体は子コルーチンへ投げるので、測位に時間がかかっても次の周期はずれない。
                launch { recordOnce() }
                delay(LOCATION_INTERVAL_MS)
            }
        }
    }

    fun stop() {
        job?.cancel(); job = null
    }

    /**
     * 記録の状態を初期化する。セッションの開始で呼び、最初の 1 回は「前回地点なし」＝必ず記録される状態から始める。
     * 前のセッションで書けなかった位置が新しい CSV の先頭行へ紛れ込まないよう、持ち越しも捨てる。
     */
    fun reset() {
        lastFix = null
        pending = null
    }

    /** 次のデータ行の ARTIFACT 列に載せる文字列（無ければ空）。受け取ったら空に戻る */
    fun takePendingArtifact(): String {
        val text = pending ?: return ""
        pending = null
        return text
    }

    private suspend fun recordOnce() {
        val fix = runCatching { sample() }.getOrNull() ?: return
        // 測位が返るまでの間に計測が終わっていたら、載せる行が無いので捨てる。
        if (!ui.value.isMeasuring) return
        if (!movedAtLeast(lastFix, fix, LOCATION_MOVE_THRESHOLD_M)) return
        val text = formatLocationArtifact(fix.latitude, fix.longitude) ?: return
        lastFix = fix
        pending = text
        onRecorded(text)
    }
}
