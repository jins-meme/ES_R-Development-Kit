package com.jins_jp.meme.core.ui.main

import android.net.Uri
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

/**
 * CSV 再生への出入り。再生そのもの（読み込み・再生・一時停止・巻き戻し・早送り・再生速度）はグラフ画面のページが
 * 受け持つ（DevKit webview/BRIDGE.md の openReplay。python-processing-core docs/porting-html/devkit-webview.md §5）。
 * アプリは選んだ CSV をページに読ませ、終わり（Disconnect）でページで付けたアーティファクトを再生元の CSV へ
 * 書き戻すだけ。UI 状態は [ui]（isReplaying = 再生中）へ直接反映する。
 */
internal class PlaybackController(
    private val scope: CoroutineScope,
    private val ui: MutableStateFlow<MainUiState>,
    private val reconnect: ReconnectController,
    private val stopMeasurement: () -> Unit,
    private val displayName: (Uri) -> String,
    // ページに CSV を読ませる / 再生を閉じる（WebBridge.openReplay / closeReplay）
    private val openReplay: (Uri, String) -> Unit,
    private val closeReplay: () -> Unit,
    // 再生元の CSV へアーティファクトを書き戻す（データ行の番号で対応付け）
    private val mergeLabels: suspend (Uri) -> Unit,
) {
    /** 再生中の CSV の URI（OpenDocument 経由なので書き込み権限もある）。書き戻し先 */
    var sourceUri: Uri? = null
        private set

    /** Play ボタン: 選んだ CSV をページで再生する。null はファイル選択のキャンセル */
    fun start(uri: Uri?) {
        if (uri == null) return
        reconnect.cancel()
        if (ui.value.isMeasuring) stopMeasurement()
        if (ui.value.isReplaying) exit()          // 再生中に別のファイルを選んだら、前のぶんを書き戻してから
        sourceUri = uri
        val name = displayName(uri)
        ui.update { it.copy(isReplaying = true, replayName = name) }
        openReplay(uri, name)
    }

    /** 再生を終える（Disconnect）。ページで付けたアーティファクトを再生元へ書き戻す */
    fun exit() {
        if (!ui.value.isReplaying) return
        val src = sourceUri
        closeReplay()
        sourceUri = null
        ui.update { it.copy(isReplaying = false, replayName = null) }
        if (src != null) scope.launch { mergeLabels(src) }
    }
}
