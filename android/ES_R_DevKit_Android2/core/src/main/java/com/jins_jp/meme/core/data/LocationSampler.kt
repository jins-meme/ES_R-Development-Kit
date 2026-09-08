package com.jins_jp.meme.core.data

import android.Manifest
import android.annotation.SuppressLint
import android.content.Context
import android.content.pm.PackageManager
import android.location.Location
import android.location.LocationManager
import android.location.LocationRequest
import android.os.CancellationSignal
import android.util.Log
import androidx.core.content.ContextCompat
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.joinAll
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withTimeoutOrNull
import java.util.Locale
import kotlin.coroutines.resume
import kotlin.math.abs
import kotlin.math.cos

private const val TAG = "LocationSampler"

/** ARTIFACT 列へ書く位置情報の接頭辞。例: "lc:35.6802_139.7521"。 */
const val LOCATION_ARTIFACT_PREFIX = "lc:"

/** 1 回の取得を諦めるまでの時間。次の周期(1 分)より十分短くする。 */
private const val FIX_TIMEOUT_MS = 20_000L

/** 緯度 1 度あたりの距離[m]。経度は緯度に応じて cos で縮む。 */
private const val METERS_PER_DEGREE_LATITUDE = 111_320.0

/** 測位 1 回分の結果。整形前の生の緯度経度で持ち、移動量の判定にも使う。 */
data class LocationFix(val latitude: Double, val longitude: Double)

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
 * [next] が [prev] から**緯度方向・経度方向のどちらか**で [meters] 以上動いたか。
 * [prev] が無い(セッション最初の測位)なら常に true＝必ず 1 件残す。
 *
 * 直線距離ではなく南北・東西の成分で見るのは、記録したいのが「同じ場所に留まって
 * いるか、別の場所へ移ったか」だけで、斜め移動の厳密さが要らないため。緯度方向は
 * 定数倍、経度方向は緯度に応じて cos で縮める（日本付近で 1 度 ≒ 91km）。
 */
fun movedAtLeast(prev: LocationFix?, next: LocationFix, meters: Double): Boolean {
    if (prev == null) return true
    val northSouth = abs(next.latitude - prev.latitude) * METERS_PER_DEGREE_LATITUDE
    // 日付変更線をまたぐ差(359.9 度など)は短い側で測る。
    val rawLonDelta = abs(next.longitude - prev.longitude)
    val lonDelta = if (rawLonDelta > 180.0) 360.0 - rawLonDelta else rawLonDelta
    val midLatRad = Math.toRadians((prev.latitude + next.latitude) / 2.0)
    val eastWest = lonDelta * METERS_PER_DEGREE_LATITUDE * abs(cos(midLatRad))
    return northSouth >= meters || eastWest >= meters
}

/**
 * 計測中の大まかな現在地(Approximate Location)を 1 回だけ取り、緯度経度を返す。
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
     * 現在地を 1 回取得する。取れなければ null。呼び出し側を待たせ続けないよう、
     * 全体で必ず [FIX_TIMEOUT_MS] で打ち切る。
     */
    suspend fun sample(): LocationFix? {
        // 測位できない時に黙って捨てると原因が分からない（画面 OFF 中に位置が
        // 1 点も残らない不具合の切り分けに、実機の appops を読む羽目になった）。
        // 失敗の理由だけは必ず 1 行残す。
        if (!hasPermission()) {
            Log.w(TAG, "skip: ACCESS_COARSE_LOCATION not granted")
            return null
        }
        val lm = context.getSystemService(Context.LOCATION_SERVICE) as? LocationManager ?: return null
        val providers = PROVIDERS.filter {
            runCatching { lm.isProviderEnabled(it) }.getOrDefault(false)
        }
        if (providers.isEmpty()) {
            Log.w(TAG, "skip: no enabled provider")
            return null
        }

        val fix = fixFrom(lm, providers)
        if (fix == null) {
            // フォアグラウンドサービスの type に location が無いと、要求は登録されても
            // 不活性のまま何も返らずここへ落ちる（[MeasurementService] を参照）。
            Log.w(TAG, "no fix within ${FIX_TIMEOUT_MS}ms from $providers")
        } else {
            Log.i(TAG, "fix ${formatLocationArtifact(fix.latitude, fix.longitude)}")
        }
        return fix
    }

    /**
     * [providers] へ同時に投げ、最初に返った有効な測位を返す。取れなければ null。
     *
     * 順番に試さず**同時に**投げるのは、fused が有効なのにコールバックを返さない
     * 環境が実際にあり（Android エミュレータで確認）、直列だと 1 つ目で待ち時間を
     * 使い切って gps まで辿り着かないため。最初の 1 つが返った時点で残りは打ち切る
     * ので、fused がキャッシュから即答する通常の端末では gps は起動しない。
     */
    private suspend fun fixFrom(lm: LocationManager, providers: List<String>): LocationFix? {
        return withTimeoutOrNull(FIX_TIMEOUT_MS) {
            coroutineScope {
                val fix = CompletableDeferred<LocationFix?>()
                val attempts = providers.map { provider ->
                    launch {
                        val location = currentLocation(lm, provider) ?: return@launch
                        if (!location.latitude.isFinite() || !location.longitude.isFinite()) {
                            return@launch
                        }
                        fix.complete(LocationFix(location.latitude, location.longitude))
                    }
                }
                // 全プロバイダが測位に失敗したら、タイムアウトを待たずに諦める。
                val watchdog = launch { attempts.joinAll(); fix.complete(null) }
                val result = fix.await()
                // 残りの測位要求を畳む（CancellationSignal 経由で実際に止まる）。
                attempts.forEach { it.cancel() }
                watchdog.cancel()
                result
            }
        }
    }

    /**
     * [provider] で 1 回だけ測位する。測位できなければ（エラー含め）null。
     *
     * 精度は [LocationRequest.QUALITY_BALANCED_POWER_ACCURACY]。街区レベルの位置が
     * 分かれば十分（記録は小数 4 桁・移動判定は 50m 単位）で、GPS を回して精密な
     * 測位をする必要が無いため。1 回の測位に使う時間も [FIX_TIMEOUT_MS] で切る。
     */
    @SuppressLint("MissingPermission")
    private suspend fun currentLocation(lm: LocationManager, provider: String): Location? =
        suspendCancellableCoroutine { cont ->
            val signal = CancellationSignal()
            cont.invokeOnCancellation { runCatching { signal.cancel() } }
            runCatching {
                lm.getCurrentLocation(
                    provider,
                    balancedRequest,
                    signal,
                    context.mainExecutor,
                ) { loc -> if (cont.isActive) cont.resume(loc) }
            }.onFailure { if (cont.isActive) cont.resume(null) }
        }

    private val balancedRequest: LocationRequest =
        LocationRequest.Builder(FIX_TIMEOUT_MS)
            .setQuality(LocationRequest.QUALITY_BALANCED_POWER_ACCURACY)
            .setDurationMillis(FIX_TIMEOUT_MS)
            .setMaxUpdates(1)
            .build()

    private companion object {
        val PROVIDERS = listOf(
            LocationManager.FUSED_PROVIDER,
            LocationManager.NETWORK_PROVIDER,
            LocationManager.GPS_PROVIDER,
        )
    }
}
