package com.jins_jp.meme.core.ui.main

import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.rememberScrollState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableLongStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.chart.GraphBuffer
import com.jins_jp.meme.core.chart.LineSeries
import com.jins_jp.meme.core.chart.LiveLineChart
import com.jins_jp.meme.core.theme.AccBlue
import com.jins_jp.meme.core.theme.AccGreen
import com.jins_jp.meme.core.theme.AccRed
import com.jins_jp.meme.core.theme.EogBlue
import com.jins_jp.meme.core.theme.EogRed
import com.jins_jp.meme.core.theme.GyroBlue
import com.jins_jp.meme.core.theme.GyroGreen
import com.jins_jp.meme.core.theme.GyroRed

// 6 seconds at the 25 Hz plot rate (100 Hz ÷ 4 = 50 Hz ÷ 2 = 25 Hz).
private const val GRAPH_LEN = 150

// プロット点のレート(Hz)。経過時間の換算に使う（GRAPH_LEN / PLOT_HZ = 6秒の可視窓）。
private const val PLOT_HZ = 25

/**
 * デフォルトの基本チャートペイン（EOG / 加速度 / ジャイロ）。プラグインを持たない
 * アプリではこれがそのまま [MainScreen] の `charts` スロットの既定値になる。
 */
@Composable
fun SensorChartsPane(viewModel: MainViewModel, ui: MainUiState) {
    // Live graph buffers
    val eogVh = remember { GraphBuffer(GRAPH_LEN) }
    val eogVv = remember { GraphBuffer(GRAPH_LEN) }
    val accX = remember { GraphBuffer(GRAPH_LEN) }
    val accY = remember { GraphBuffer(GRAPH_LEN) }
    val accZ = remember { GraphBuffer(GRAPH_LEN) }
    val gyroX = remember { GraphBuffer(GRAPH_LEN) }
    val gyroY = remember { GraphBuffer(GRAPH_LEN) }
    val gyroZ = remember { GraphBuffer(GRAPH_LEN) }
    var bumper by remember { mutableIntStateOf(0) }
    var minimizedCharts by remember { mutableStateOf(setOf<String>()) }
    // 最新プロット点の通番(= totalCount / graphSkipCount)。経過秒の換算に使う。
    var emitX by remember { mutableLongStateOf(0L) }

    LaunchedEffect(Unit) {
        viewModel.graph.collect { ev ->
            when (ev) {
                GraphEvent.Reset -> {
                    eogVh.clear(); eogVv.clear()
                    accX.clear(); accY.clear(); accZ.clear()
                    gyroX.clear(); gyroY.clear(); gyroZ.clear()
                    emitX = 0L
                }
                is GraphEvent.Eog -> {
                    eogVh.add(ev.vh); eogVv.add(ev.vv)
                    emitX = ev.x
                }
                is GraphEvent.Acc -> {
                    accX.add(ev.x1); accY.add(ev.y); accZ.add(ev.z)
                }
                is GraphEvent.Gyro -> {
                    gyroX.add(ev.x1); gyroY.add(ev.y); gyroZ.add(ev.z)
                }
                // プラグイン発の任意ペイロード。基本ペインはプラグインを知らないので無視する。
                is GraphEvent.Custom -> Unit
            }
            bumper++
        }
    }

    if (ui.isMeasuring || emitX > 0L) {
        bumper // ensure recomposition keys
        val charts = listOf(
            ChartSpec(
                key = "eog",
                title = stringResource(R.string.eog_graph_title),
                series = listOf(
                    LineSeries(EogBlue, eogVv.snapshotY(), label = "Vv"),
                    LineSeries(EogRed, eogVh.snapshotY(), label = "Vh"),
                ),
                yMin = -400f, yMax = 400f,
            ),
            ChartSpec(
                key = "acc",
                title = stringResource(R.string.acc_graph_title),
                series = listOf(
                    LineSeries(AccBlue, accX.snapshotY(), label = "X"),
                    LineSeries(AccGreen, accY.snapshotY(), label = "Y"),
                    LineSeries(AccRed, accZ.snapshotY(), label = "Z"),
                ),
                yMin = -35000f, yMax = 35000f,
            ),
            ChartSpec(
                key = "gyro",
                title = stringResource(R.string.gyro_graph_title),
                series = listOf(
                    LineSeries(GyroBlue, gyroX.snapshotY(), label = "X"),
                    LineSeries(GyroGreen, gyroY.snapshotY(), label = "Y"),
                    LineSeries(GyroRed, gyroZ.snapshotY(), label = "Z"),
                ),
                yMin = -35000f, yMax = 35000f,
            ),
        )
        charts.forEach { spec ->
            if (spec.key !in minimizedCharts) {
                LiveLineChart(
                    title = spec.title,
                    series = spec.series,
                    yMin = spec.yMin,
                    yMax = spec.yMax,
                    onMinimize = { minimizedCharts = minimizedCharts + spec.key },
                    // 経過時間(理論値): 右端=最新点の経過秒、1点=1/PLOT_HZ 秒。
                    xRightSeconds = emitX / PLOT_HZ.toFloat(),
                    xSecondsPerPoint = 1f / PLOT_HZ,
                )
            }
        }
        if (minimizedCharts.isNotEmpty()) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .horizontalScroll(rememberScrollState()),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                charts.filter { it.key in minimizedCharts }.forEach { spec ->
                    MinimizedChartChip(
                        title = spec.title,
                        onClick = { minimizedCharts = minimizedCharts - spec.key },
                    )
                }
            }
        }
    }
}

private data class ChartSpec(
    val key: String,
    val title: String,
    val series: List<LineSeries>,
    val yMin: Float,
    val yMax: Float,
)
