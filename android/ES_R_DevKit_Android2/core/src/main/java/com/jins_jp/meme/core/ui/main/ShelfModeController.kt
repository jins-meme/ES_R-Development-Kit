package com.jins_jp.meme.core.ui.main

import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.ble.MemeCommands
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.withTimeoutOrNull

// Shelf 移行の 1 段目（CONFIG モードへの遷移）の ACK を待つ時間。通常は
// 100ms 台で返る。ここで諦めても SHELF コマンドは送らないので端末は無傷。
internal const val SHELF_CONFIG_ACK_TIMEOUT_MS = 3_000L

// SHELF コマンド送信後、端末が自ら切断するのを待つ時間（切断＝移行成功）。
internal const val SHELF_DISCONNECT_TIMEOUT_MS = 5_000L

/**
 * 端末を Shelf mode（保管モード）へ移す。Disconnect の長押しで確認ダイアログを開き（[request]）、Yes で [confirm]。
 * Web Bluetooth 版 SDK と同じ順序で (1) CONFIG モードへの遷移を送り (2) その ACK を待ってから (3) SHELF を送る。
 * 受理されると端末は自分から切断するので、切断が来たら成功と見なす。復帰は充電のみで、アプリからは戻せない。
 *
 * [send] は暗号化して端末へ送る口、[connection] は接続状態。ACK は [onResp] で受け取る（AUP_REPORT_RESP）。
 */
internal class ShelfModeController(
    private val scope: CoroutineScope,
    private val ui: MutableStateFlow<MainUiState>,
    private val connection: StateFlow<ConnectionState>,
    private val reconnect: ReconnectController,
    private val send: (ByteArray) -> Unit,
) {
    /**
     * 直前に送ったコマンドの AUP_REPORT_RESP(ACK/NACK)を 1 件だけ受け取るための待ち合わせ。
     * 「CONFIG への遷移が成功してから SHELF を送る」順序のため、送信の直前に置いて [onResp] から完了させる。
     */
    private var pendingAck: CompletableDeferred<Boolean>? = null

    /**
     * Shelf mode へ移行できる状態か。SHELF コマンドは実機が接続済みで計測していない
     * ときだけ受理されるので、CSV 再生中と計測中は対象外。
     */
    fun canEnter(): Boolean {
        val st = ui.value
        return st.connection == ConnectionState.ServicesReady &&
            !st.isMeasuring && !st.isReplaying && !st.isEnteringShelf
    }

    /** 確認ダイアログを開く。移行できる状態でなければ何も起きない */
    fun request() {
        if (!canEnter()) return
        ui.update { it.copy(showShelfDialog = true) }
    }

    fun dismiss() { ui.update { it.copy(showShelfDialog = false) } }

    fun confirm() {
        ui.update { it.copy(showShelfDialog = false) }
        if (!canEnter()) return
        scope.launch {
            ui.update { it.copy(isEnteringShelf = true) }
            // 移行後の切断はこちらの意図した切断。自動再接続に拾わせない。
            reconnect.noteUserDisconnect()
            reconnect.cancel()

            val ack = CompletableDeferred<Boolean>()
            // 送信より先に置く（ACK の取りこぼしを構造的に無くしておく）。
            pendingAck = ack
            send(MemeCommands.setConfigMode())
            val configured = withTimeoutOrNull(SHELF_CONFIG_ACK_TIMEOUT_MS) { ack.await() } == true
            pendingAck = null
            if (!configured) {
                // SHELF はまだ送っていないので端末は通常モードのまま。
                ui.update { it.copy(isEnteringShelf = false, toast = "Failed to enter shelf mode") }
                return@launch
            }

            send(MemeCommands.shelf())
            val disconnected = withTimeoutOrNull(SHELF_DISCONNECT_TIMEOUT_MS) {
                connection.first { it == ConnectionState.Disconnected }
            } != null
            ui.update {
                it.copy(
                    isEnteringShelf = false,
                    toast = if (disconnected) "Entered shelf mode" else "Failed to enter shelf mode",
                )
            }
        }
    }

    /** AUP_REPORT_RESP を受けた（[ok] = ACK）。待っている処理があれば結果を渡す */
    fun onResp(ok: Boolean) {
        pendingAck?.complete(ok)
    }
}
