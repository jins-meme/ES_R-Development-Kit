package com.jins_jp.meme.core.data

import android.Manifest
import android.annotation.SuppressLint
import android.content.Context
import android.content.pm.PackageManager
import android.location.Location
import android.location.LocationManager
import android.os.CancellationSignal
import androidx.core.content.ContextCompat
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.joinAll
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withTimeoutOrNull
import java.util.Locale
import kotlin.coroutines.resume

/** ARTIFACT 列へ書く位置情報の接頭辞。例: "lc:35.6802_139.7521"。 */
const val LOCATION_ARTIFACT_PREFIX = "lc:"

/** 1 回の取得を諦めるまでの時間。次の周期(1 分)より十分短くする。 */
private const val FIX_TIMEOUT_MS = 20_000L

/**
 * 緯度経度を ARTIFACT 列の 1 エントリ ("lc:35.6802_139.7521") にする。無効値なら null。
 * 小数 4 桁(約 11m)は Approximate Location の粒度に対して十分。CSV を壊さないよう、
 * 小数点がカンマになるロケールを避けて [Locale.US] で整形する。
 */
fun formatLocationArtifact(latitude: Double, longitude: Double): String? {
    if (!latitude.isFinite() || !longitude.isFinite()) return null
    if (latitude !in -90.0..90.0 || longitude !in -180.0..180.0) return null
    return LOCATION_ARTIFACT_PREFIX + "%.4f_%.4f".format(Locale.US, latitude, longitude)
}

/**
 * 計測中の大まかな現在地(Approximate Location)を 1 回だけ取り、ARTIFACT 列へ
 * 書ける 1 行の文字列にする。
 *
 * 権限は [Manifest.permission.ACCESS_COARSE_LOCATION] だけを要求する前提。
 * COARSE しか無い状態ではどのプロバイダも街区レベルに丸めた座標を返すので、
 * 精密な位置は取れない（それでよい）。取得できない場合(権限なし・プロバイダ
 * 無効・タイムアウト・無効値)は一貫して null を返し、呼び出し側は何も記録しない。
 */
class LocationSampler(private val context: Context) {

    /** 大まかな位置の取得が許可されているか。FINE があれば COARSE も満たされる。 */
    fun hasPermission(): Boolean {
        val granted = { p: String ->
            ContextCompat.checkSelfPermission(context, p) == PackageManager.PERMISSION_GRANTED
        }
        return granted(Manifest.permission.ACCESS_COARSE_LOCATION) ||
            granted(Manifest.permission.ACCESS_FINE_LOCATION)
    }

    /**
     * 現在地を 1 回取得して "lc:35.6802_139.7521" 形式で返す。取れなければ null。
     * 呼び出し側を待たせ続けないよう、全体で必ず [FIX_TIMEOUT_MS] で打ち切る。
     *
     * 有効なプロバイダは順番に試すのではなく**同時に**投げ、最初に返った有効値を
     * 採る。fused が有効なのにコールバックを返さない環境が実際にあり（Android
     * エミュレータで確認）、直列だと 1 つ目で待ち時間を使い切って gps まで
     * 辿り着かないため。
     */
    suspend fun sample(): String? {
        if (!hasPermission()) return null
        val lm = context.getSystemService(Context.LOCATION_SERVICE) as? LocationManager ?: return null
        val providers = PROVIDERS.filter {
            runCatching { lm.isProviderEnabled(it) }.getOrDefault(false)
        }
        if (providers.isEmpty()) return null

        return withTimeoutOrNull(FIX_TIMEOUT_MS) {
            coroutineScope {
                val fix = CompletableDeferred<String?>()
                val attempts = providers.map { provider ->
                    launch {
                        val location = currentLocation(lm, provider) ?: return@launch
                        val text = formatLocationArtifact(location.latitude, location.longitude)
                        if (text != null) fix.complete(text)
                    }
                }
                // 全プロバイダが測位に失敗したら、タイムアウトを待たずに諦める。
                val watchdog = launch { attempts.joinAll(); fix.complete(null) }
                val text = fix.await()
                // 残りの測位要求を畳む（CancellationSignal 経由で実際に止まる）。
                attempts.forEach { it.cancel() }
                watchdog.cancel()
                text
            }
        }
    }

    /** [provider] で 1 回だけ測位する。測位できなければ（エラー含め）null。 */
    @SuppressLint("MissingPermission")
    private suspend fun currentLocation(lm: LocationManager, provider: String): Location? =
        suspendCancellableCoroutine { cont ->
            val signal = CancellationSignal()
            cont.invokeOnCancellation { runCatching { signal.cancel() } }
            runCatching {
                lm.getCurrentLocation(
                    provider,
                    signal,
                    context.mainExecutor,
                ) { loc -> if (cont.isActive) cont.resume(loc) }
            }.onFailure { if (cont.isActive) cont.resume(null) }
        }

    private companion object {
        val PROVIDERS = listOf(
            LocationManager.FUSED_PROVIDER,
            LocationManager.NETWORK_PROVIDER,
            LocationManager.GPS_PROVIDER,
        )
    }
}
