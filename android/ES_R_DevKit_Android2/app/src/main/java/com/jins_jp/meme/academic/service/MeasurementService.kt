package com.jins_jp.meme.academic.service

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.IBinder
import android.os.PowerManager
import androidx.core.app.NotificationCompat
import com.jins_jp.meme.academic.MainActivity
import com.jins_jp.meme.academic.R

/**
 * 計測中にプロセスと CPU を生かし続けるためのフォアグラウンドサービス。
 *
 * バックグラウンド／スリープ時、OS はアプリのプロセスをアイドル扱いにして
 * kill したり CPU を眠らせたりするため、BLE のデータ受信が途切れる。これを防ぐため:
 *   - 常駐通知付きの Foreground Service（type=connectedDevice）でプロセスを保護し、
 *   - PARTIAL_WAKE_LOCK で画面 OFF 中も CPU を回して GATT コールバックを届かせる。
 *
 * BLE 接続そのものは [com.jins_jp.meme.academic.App] スコープの
 * [com.jins_jp.meme.academic.ble.MemeBleRepository] が保持しており、このサービスは
 * 「プロセスを生かす」ことだけを担う。開始／停止は計測ライフサイクルに合わせて
 * [MainViewModel][com.jins_jp.meme.academic.ui.main.MainViewModel] から [start]/[stop] で制御する。
 */
class MeasurementService : Service() {

    private var wakeLock: PowerManager.WakeLock? = null

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        createChannel()
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        // minSdk 31 のため 3 引数版 startForeground が常に使える。
        startForeground(
            NOTIFICATION_ID,
            buildNotification(),
            ServiceInfo.FOREGROUND_SERVICE_TYPE_CONNECTED_DEVICE,
        )
        acquireWakeLock()
        // プロセス死後の自動復帰は無意味（BLE 接続も一緒に失われるため）。
        return START_NOT_STICKY
    }

    override fun onTaskRemoved(rootIntent: Intent?) {
        // タスクをスワイプで消したら計測も畳む（孤児サービスを残さない）。
        stopSelf()
        super.onTaskRemoved(rootIntent)
    }

    override fun onDestroy() {
        releaseWakeLock()
        super.onDestroy()
    }

    private fun buildNotification(): Notification {
        val open = PendingIntent.getActivity(
            this,
            0,
            Intent(this, MainActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP
            },
            PendingIntent.FLAG_IMMUTABLE,
        )
        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle(getString(R.string.notification_measuring_title))
            .setContentText(getString(R.string.notification_measuring_text))
            .setSmallIcon(R.drawable.ic_stat_measure)
            .setContentIntent(open)
            .setOngoing(true)
            .setForegroundServiceBehavior(NotificationCompat.FOREGROUND_SERVICE_IMMEDIATE)
            .setCategory(NotificationCompat.CATEGORY_SERVICE)
            .build()
    }

    private fun acquireWakeLock() {
        if (wakeLock?.isHeld == true) return
        val pm = getSystemService(Context.POWER_SERVICE) as PowerManager
        wakeLock = pm.newWakeLock(PowerManager.PARTIAL_WAKE_LOCK, WAKELOCK_TAG).apply {
            setReferenceCounted(false)
            acquire()
        }
    }

    private fun releaseWakeLock() {
        wakeLock?.let { if (it.isHeld) it.release() }
        wakeLock = null
    }

    private fun createChannel() {
        val nm = getSystemService(NotificationManager::class.java)
        if (nm.getNotificationChannel(CHANNEL_ID) != null) return
        val channel = NotificationChannel(
            CHANNEL_ID,
            getString(R.string.notification_channel_measurement),
            NotificationManager.IMPORTANCE_LOW,
        ).apply { setShowBadge(false) }
        nm.createNotificationChannel(channel)
    }

    companion object {
        private const val CHANNEL_ID = "measurement"
        private const val NOTIFICATION_ID = 1001
        private const val WAKELOCK_TAG = "esr_devkit:measurement"

        /** 計測中の常駐保護を開始する。すでに起動済みなら通知の再配信のみ（冪等）。 */
        fun start(context: Context) {
            context.startForegroundService(Intent(context, MeasurementService::class.java))
        }

        /** 常駐保護を終了する。未起動でも安全（no-op）。 */
        fun stop(context: Context) {
            context.stopService(Intent(context, MeasurementService::class.java))
        }
    }
}
