package com.jins_jp.meme.core.ui.main

import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.rememberScrollState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableLongStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.jins_jp.meme.core.chart.ChartEventLine
import com.jins_jp.meme.core.chart.GraphBuffer
import com.jins_jp.meme.core.chart.LineSeries

/** ARTIFACT イベントライン色（オレンジ）。ライト/ダーク両テーマで視認できる。 */
val ArtifactLineColor = Color(0xFFFF9800)

/**
 * 可視窓に入る [ArtifactEvent] をチャートのイベントライン（オレンジ縦線＋文字）へ
 * 変換する。[rightSeconds] はチャート右端の秒（X 軸表示 xRightSeconds と同じ値）、
 * [windowSeconds] は可視窓の幅（(プロット点数-1)/プロットHz）。全チャートで X 軸を
 * 共有しているので、ペインは 1 回計算して各チャートへ同じリストを渡せばよい。
 */
fun artifactEventLines(
    events: List<ArtifactEvent>,
    rightSeconds: Float,
    windowSeconds: Float,
): List<ChartEventLine> {
    if (events.isEmpty() || windowSeconds <= 0f) return emptyList()
    val lines = ArrayList<ChartEventLine>()
    for (e in events) {
        val f = 1f - (rightSeconds - e.sec.toFloat()) / windowSeconds
        if (f in 0f..1f) lines.add(ChartEventLine(f, e.text, ArtifactLineColor))
    }
    return lines
}

/** 1 チャート分の宣言（タイトル・系列・Y レンジ）。ペイン実装が列挙して描く。 */
data class ChartSpec(
    val key: String,
    val title: String,
    val series: List<LineSeries>,
    val yMin: Float,
    val yMax: Float,
    val rightYMin: Float? = null,
    val rightYMax: Float? = null,
)

/**
 * 基本センサ系列（EOG/加速度/ジャイロ）の [GraphBuffer] 集合。[GraphEvent] の基本イベント
 * （Reset/Eog/Acc/Gyro）を蓄積する。プラグイン固有の系列（[GraphEvent.Custom]）はペイン側で扱う。
 */
class SensorGraphBuffers(graphLen: Int) {
    val eogVh = GraphBuffer(graphLen)
    val eogVv = GraphBuffer(graphLen)
    val accX = GraphBuffer(graphLen)
    val accY = GraphBuffer(graphLen)
    val accZ = GraphBuffer(graphLen)
    val gyroX = GraphBuffer(graphLen)
    val gyroY = GraphBuffer(graphLen)
    val gyroZ = GraphBuffer(graphLen)

    /** EOG プロット点の発行数（欠落サンプルを数えない）。マーカーの emitX 座標系。 */
    var emitCount: Long by mutableLongStateOf(0L)
        private set

    /** 最新プロット点の通番 ev.x（欠落を含む理論値）。経過秒表示用。 */
    var latestX: Long by mutableLongStateOf(0L)
        private set

    /** [Reset]/[Eog]/[Acc]/[Gyro] を蓄積する。[Custom] は何もしない（呼び出し側が処理）。 */
    fun onEvent(ev: GraphEvent) {
        when (ev) {
            GraphEvent.Reset -> {
                eogVh.clear(); eogVv.clear()
                accX.clear(); accY.clear(); accZ.clear()
                gyroX.clear(); gyroY.clear(); gyroZ.clear()
                emitCount = 0L
                latestX = 0L
            }
            is GraphEvent.Eog -> {
                eogVh.add(ev.vh); eogVv.add(ev.vv)
                emitCount++
                latestX = ev.x
            }
            is GraphEvent.Acc -> {
                accX.add(ev.x1); accY.add(ev.y); accZ.add(ev.z)
            }
            is GraphEvent.Gyro -> {
                gyroX.add(ev.x1); gyroY.add(ev.y); gyroZ.add(ev.z)
            }
            is GraphEvent.Custom -> Unit
        }
    }
}

/** 最小化されたチャートのチップ行。1 つも無ければ何も描かない。 */
@Composable
fun MinimizedChartsRow(
    charts: List<ChartSpec>,
    minimized: Set<String>,
    onRestore: (String) -> Unit,
) {
    val items = charts.filter { it.key in minimized }
    if (items.isEmpty()) return
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .horizontalScroll(rememberScrollState()),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        items.forEach { spec ->
            MinimizedChartChip(
                title = spec.title,
                onClick = { onRestore(spec.key) },
            )
        }
    }
}
