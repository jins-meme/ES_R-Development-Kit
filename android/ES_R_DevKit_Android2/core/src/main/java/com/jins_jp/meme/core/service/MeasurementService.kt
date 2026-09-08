package com.jins_jp.meme.core.service

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
import android.util.Log
import androidx.core.app.NotificationCompat
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.data.LocationSampler
import com.jins_jp.meme.core.data.SettingsStore

private const val TAG = "MeasurementService"

/**
 * 計測中にプロセスと CPU を生かし続けるためのフォアグラウンドサービス。
 *
 * バックグラウンド／スリープ時、OS はアプリのプロセスをアイドル扱いにして
 * kill したり CPU を眠らせたりするため、BLE のデータ受信が途切れる。これを防ぐため:
 *   - 常駐通知付きの Foreground Service（type=connectedDevice）でプロセスを保護し、
 *   - PARTIAL_WAKE_LOCK で画面 OFF 中も CPU を回して GATT コールバックを届かせる。
 *
 * 位置記録が ON のときは type に location も足す（[foregroundServiceType]）。
 *
 * BLE 接続そのものは [com.jins_jp.meme.core.App] スコープの
 * [com.jins_jp.meme.core.ble.MemeBleRepository] が保持しており、このサービスは
 * 「プロセスを生かす」ことだけを担う。開始／停止は計測ライフサイクルに合わせて
 * アプリ側の ViewModel から [start]/[stop] で制御する。
 */
class MeasurementService : Service() {

    private var wakeLock: PowerManager.WakeLock? = null

    // いま startForeground 済みの type。既に同じ型で前面化しているなら呼び直さない
    // （理由は [onStartCommand]）。サービスのインスタンスと寿命が同じ。
    private var currentType: Int? = null

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        createChannel()
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        // minSdk 31 のため 3 引数版 startForeground が常に使える。
        val type = foregroundServiceType()
        // 型が変わらないなら呼び直さない。自動再接続は計測中の切断からの続きで、
        // サービスを止めずに維持したままバックグラウンドから [start] を呼び直すため、
        // ここで location 型を**付け直そうとする**と「バックグラウンドから
        // while-in-use 型の FGS を開始した」と見なされて SecurityException になり得る。
        // 既に location 付きで前面化しているものをそのままにすれば、その判定に触れない。
        if (currentType == type) {
            acquireWakeLock()
            return START_NOT_STICKY
        }
        try {
            startForeground(NOTIFICATION_ID, buildNotification(), type)
            currentType = type
        } catch (e: SecurityException) {
            // location 型は「開始時点で位置権限がある」ことを OS が検査する。権限を
            // 取り消した直後などで弾かれても計測そのものは続けたいので、位置を諦めて
            // connectedDevice だけで上げ直す。
            Log.w(TAG, "startForeground(type=$type) rejected; retry without location", e)
            startForeground(
                NOTIFICATION_ID,
                buildNotification(),
                ServiceInfo.FOREGROUND_SERVICE_TYPE_CONNECTED_DEVICE,
            )
            currentType = ServiceInfo.FOREGROUND_SERVICE_TYPE_CONNECTED_DEVICE
        }
        acquireWakeLock()
        // プロセス死後の自動復帰は無意味（BLE 接続も一緒に失われるため）。
        return START_NOT_STICKY
    }

    /**
     * このサービスを上げるときの foregroundServiceType。
     *
     * **location を足さないと、画面 OFF 中の測位が丸ごと落ちる。**
     * ACCESS_FINE_LOCATION も COARSE と同じく while-in-use（appop が foreground）なので、アプリが
     * 前面でなくなった時点で位置の capability を失う。type=connectedDevice だけだと
     * FGS 中も「前面ではない」扱いになり、LocationManager は要求を不活性のまま置いて
     * 例外も出さずに null を返す（実機の appops で、fgsvc 中は MONITOR_LOCATION の
     * 記録が 1 件も付かないことを確認済み）。結果、CSV に残る位置は Start を押した
     * 瞬間の 1 点だけになる。
     *
     * 位置権限が無い／設定が OFF のときに location を渡すと [SecurityException] に
     * なるため、両方そろったときだけ足す。設定を計測中に切り替えたときは
     * MainViewModel が [start] を呼び直してここを再評価させる。
     */
    private fun foregroundServiceType(): Int {
        val useLocation =
            SettingsStore(this).loadLocationLogging() && LocationSampler(this).hasPermission()
        return if (useLocation) {
            ServiceInfo.FOREGROUND_SERVICE_TYPE_CONNECTED_DEVICE or
                ServiceInfo.FOREGROUND_SERVICE_TYPE_LOCATION
        } else {
            ServiceInfo.FOREGROUND_SERVICE_TYPE_CONNECTED_DEVICE
        }
    }

    override fun onTaskRemoved(rootIntent: Intent?) {
        // タスクをスワイプで消したら計測も畳む（孤児サービスを残さない）。
        stopSelf()
        super.onTaskRemoved(rootIntent)
    }

    override fun onDestroy() {
        currentType = null
        releaseWakeLock()
        super.onDestroy()
    }

    private fun buildNotification(): Notification {
        // core はアプリの Activity を知らないため、通知タップはランチャーインテントで開く。
        val open = packageManager.getLaunchIntentForPackage(packageName)?.let { launch ->
            launch.flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP
            PendingIntent.getActivity(this, 0, launch, PendingIntent.FLAG_IMMUTABLE)
        }
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
