package com.jins_jp.meme.core.ui.main

import android.Manifest
import android.annotation.SuppressLint
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.os.SystemClock
import android.util.Log
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.data.formatVasArtifact
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch

private const val TAG = "VasRecorder"

// 次の実施時刻が来たかを見る間隔。時刻は elapsedRealtime で持つので、ここが多少ずれても周期はずれない。
private const val VAS_TICK_MS = 1_000L

/**
 * 画面に出している VAS の入力ポップアップ 1 回分。
 * [key] は通知を出した時点のサンプル通し番号(NUM)で、値はこの行の ARTIFACT 列へ載る（手動ラベルと同じく停止時に統合）。
 * [memos] は項目ごとのメモ（表示だけ。CSV には書かない）、[promptedAt] は通知を出した壁時計の時刻。
 */
data class VasPrompt(
    val id: Long,
    val key: Long,
    val memos: List<String>,
    val promptedAt: Long,
)

/**
 * 設定「Record VAS」。計測開始の直後に 1 回目、その後 [com.jins_jp.meme.core.data.VasSettings.intervalMinutes] 分ごとに
 * 通知を出し、アプリ上に入力ポップアップ（[MainUiState.vasPrompt]）を出す。OK で全項目の値を "vas1:50;vas2:30" にして
 * [onRecorded] へ渡す（通知を出した時点の NUM と一緒に。セッションが手動ラベルと同じ経路で停止時に CSV へ統合する）。
 *
 * - 前のポップアップに答えないまま次の時刻が来たら、前のものは記録せずに新しいものへ差し替える
 *   （記録は通知を出した時刻で残すので、古い問いへの答えを後の時刻の値として残さない）。
 * - 計測が終わったら、答えていないポップアップと通知は消す（統合済みの CSV へは載せられないため）。
 * - 予期しない切断からの自動再接続で計測が再開したとき（[start] の resume）は周期を引き継ぐ。切断で消した
 *   ポップアップがあれば、再開した直後に出し直す。
 */
internal class VasRecorder(
    private val context: Context,
    private val scope: CoroutineScope,
    private val ui: MutableStateFlow<MainUiState>,
    private val currentKey: () -> Long,
    private val onRecorded: (key: Long, text: String) -> Unit,
) {
    private var job: Job? = null

    // 次に VAS を求める時刻(elapsedRealtime)。null は「まだ始めていない」＝次の開始ですぐ求める。
    private var nextDueAt: Long? = null

    private var nextId = 1L

    /**
     * 計測の開始で呼ぶ。[resume] が false（ユーザーの Start）ならすぐ 1 回目を求め、true（自動再接続での再開）なら
     * 前の周期を引き継ぐ（過ぎていればすぐ求める）。設定が OFF・項目が無いなら何もしない。
     */
    fun start(resume: Boolean) {
        job?.cancel(); job = null
        val settings = ui.value.vasSettings
        if (!settings.active) { nextDueAt = null; return }
        val now = SystemClock.elapsedRealtime()
        if (!resume || nextDueAt == null) nextDueAt = now
        job = scope.launch {
            while (isActive) {
                val t = SystemClock.elapsedRealtime()
                val due = nextDueAt ?: t
                if (t >= due) {
                    prompt()
                    var next = due + settings.intervalMs
                    while (next <= t) next += settings.intervalMs
                    nextDueAt = next
                }
                delay(VAS_TICK_MS)
            }
        }
    }

    /**
     * 計測の終わりで呼ぶ。答えていないポップアップと通知を消す。[keepSchedule] が true（自動再接続を待つ切断）なら
     * 周期を残し、消したポップアップは再開の直後に出し直すようにする。false なら周期も捨てる。
     */
    fun stop(keepSchedule: Boolean) {
        job?.cancel(); job = null
        val open = ui.value.vasPrompt
        if (open != null) {
            ui.update { it.copy(vasPrompt = null) }
            cancelNotification()
            if (keepSchedule) nextDueAt = SystemClock.elapsedRealtime()
        }
        if (!keepSchedule) nextDueAt = null
    }

    /** ポップアップの OK。[values] は項目の順の値（0〜100） */
    fun submit(id: Long, values: List<Int>) {
        val p = ui.value.vasPrompt ?: return
        if (p.id != id) return
        ui.update { it.copy(vasPrompt = null) }
        cancelNotification()
        if (!ui.value.isMeasuring) return
        onRecorded(p.key, formatVasArtifact(values.take(p.memos.size)))
    }

    private fun prompt() {
        val settings = ui.value.vasSettings
        if (!ui.value.isMeasuring || !settings.active) return
        if (ui.value.vasPrompt != null) Log.i(TAG, "previous VAS prompt was not answered; replaced")
        val p = VasPrompt(
            id = nextId++,
            key = currentKey(),
            memos = settings.items.map { it.memo },
            promptedAt = System.currentTimeMillis(),
        )
        ui.update { it.copy(vasPrompt = p) }
        postNotification(p)
    }

    @SuppressLint("MissingPermission")    // canPost で権限を見ている
    private fun postNotification(p: VasPrompt) {
        if (!canPost()) { Log.i(TAG, "VAS notification not shown (no permission)"); return }
        createChannel()
        val open = context.packageManager.getLaunchIntentForPackage(context.packageName)?.let { launch ->
            launch.flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP
            PendingIntent.getActivity(context, 0, launch, PendingIntent.FLAG_IMMUTABLE)
        }
        val n = NotificationCompat.Builder(context, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_stat_measure)
            .setContentTitle(context.getString(R.string.notification_vas_title))
            .setContentText(context.getString(R.string.notification_vas_text))
            .setContentIntent(open)
            .setAutoCancel(true)
            .setWhen(p.promptedAt)
            .setShowWhen(true)
            .setCategory(NotificationCompat.CATEGORY_REMINDER)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .build()
        NotificationManagerCompat.from(context).notify(NOTIFY_ID, n)
    }

    private fun cancelNotification() {
        NotificationManagerCompat.from(context).cancel(NOTIFY_ID)
    }

    private fun canPost(): Boolean {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) return false
        return NotificationManagerCompat.from(context).areNotificationsEnabled()
    }

    private fun createChannel() {
        val nm = context.getSystemService(NotificationManager::class.java)
        if (nm.getNotificationChannel(CHANNEL_ID) != null) return
        nm.createNotificationChannel(NotificationChannel(CHANNEL_ID, context.getString(R.string.notification_channel_vas),
            NotificationManager.IMPORTANCE_HIGH))
    }

    private companion object {
        const val CHANNEL_ID = "vas"
        const val NOTIFY_ID = 3001
    }
}
