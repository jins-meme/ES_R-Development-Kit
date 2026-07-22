package com.jins_jp.meme.core.ui.main

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.res.stringResource
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.chart.LineSeries
import com.jins_jp.meme.core.chart.LiveLineChart
import com.jins_jp.meme.core.theme.chartSeriesColors

// 6 seconds at the 25 Hz plot rate (100 Hz ÷ 4 = 50 Hz ÷ 2 = 25 Hz).
private const val GRAPH_LEN = 150

// プロット点のレート(Hz)。経過時間の換算に使う（GRAPH_LEN / PLOT_HZ = 6秒の可視窓）。
private const val PLOT_HZ = 25

// 加速度・ジャイロの既定レンジ。ヘッダの拡大/縮小ボタンで ±2k / ±4k / ±8k / ±16k / ±32k
// （= 既定から ±[MAX_ZOOM_LEVEL] 段）に切り替わる。
private const val IMU_RANGE = 8000f

/**
 * デフォルトの基本チャートペイン（EOG / 加速度 / ジャイロ）。プラグインを持たない
 * アプリではこれがそのまま [MainScreen] の `charts` スロットの既定値になる。
 */
@Composable
fun SensorChartsPane(viewModel: MainViewModel, ui: MainUiState) {
    // Live graph buffers
    val buffers = remember { SensorGraphBuffers(GRAPH_LEN) }
    var bumper by remember { mutableIntStateOf(0) }
    var minimizedCharts by remember { mutableStateOf(setOf<String>()) }
    val zoom = remember { ChartZoomState() }

    LaunchedEffect(Unit) {
        viewModel.graph.collect { ev ->
            buffers.onEvent(ev)
            bumper++
        }
    }

    if (ui.isMeasuring || buffers.latestX > 0L) {
        bumper // ensure recomposition keys
        // ダークモードでは暗い背景に埋もれない明るい系列色を使う。
        val colors = chartSeriesColors()
        val charts = listOf(
            ChartSpec(
                key = "eog",
                title = stringResource(R.string.eog_graph_title),
                series = listOf(
                    LineSeries(colors.eogBlue, buffers.eogVv.snapshotY(), label = "Vv"),
                    LineSeries(colors.eogRed, buffers.eogVh.snapshotY(), label = "Vh"),
                ),
                yMin = -400f, yMax = 400f,
            ),
            ChartSpec(
                key = "acc",
                title = stringResource(R.string.acc_graph_title),
                series = listOf(
                    LineSeries(colors.accBlue, buffers.accX.snapshotY(), label = "X"),
                    LineSeries(colors.accGreen, buffers.accY.snapshotY(), label = "Y"),
                    LineSeries(colors.accRed, buffers.accZ.snapshotY(), label = "Z"),
                ),
                yMin = -IMU_RANGE, yMax = IMU_RANGE,
            ),
            ChartSpec(
                key = "gyro",
                title = stringResource(R.string.gyro_graph_title),
                series = listOf(
                    LineSeries(colors.gyroBlue, buffers.gyroX.snapshotY(), label = "X"),
                    LineSeries(colors.gyroGreen, buffers.gyroY.snapshotY(), label = "Y"),
                    LineSeries(colors.gyroRed, buffers.gyroZ.snapshotY(), label = "Z"),
                ),
                yMin = -IMU_RANGE, yMax = IMU_RANGE,
            ),
        )
        // X 軸右端の秒: 再生モードでは CSV ソース位置（<< / >> で戻る・進むが数字に
        // 表れる）。実機計測では経過時間(理論値)。全チャートで共有する。
        val rightSeconds = if (ui.mockEnabled) ui.replayPositionSec.toFloat()
        else buffers.latestX / PLOT_HZ.toFloat()
        // ARTIFACT イベント（タップラベル/再生CSVのARTIFACT列）のオレンジ縦線。
        val eventLines = artifactEventLines(
            events = ui.artifactEvents,
            rightSeconds = rightSeconds,
            windowSeconds = (GRAPH_LEN - 1) / PLOT_HZ.toFloat(),
        )
        charts.forEach { spec0 ->
            if (spec0.key !in minimizedCharts) {
                // ヘッダの拡大/縮小ボタンで選んだ段を既定レンジに適用する。
                val spec = spec0.zoomed(zoom.level(spec0.key))
                LiveLineChart(
                    title = spec.title,
                    series = spec.series,
                    yMin = spec.yMin,
                    yMax = spec.yMax,
                    onMinimize = { minimizedCharts = minimizedCharts + spec.key },
                    onZoomIn = { zoom.zoomIn(spec.key) },
                    onZoomOut = { zoom.zoomOut(spec.key) },
                    canZoomIn = zoom.canZoomIn(spec.key),
                    canZoomOut = zoom.canZoomOut(spec.key),
                    // どのチャートでもタップでラベル入力ダイアログを開ける。
                    onTapFraction = { f -> viewModel.markTap(f, GRAPH_LEN) },
                    eventLines = eventLines,
                    xRightSeconds = rightSeconds,
                    xSecondsPerPoint = 1f / PLOT_HZ,
                )
            }
        }
        MinimizedChartsRow(
            charts = charts,
            minimized = minimizedCharts,
            onRestore = { key -> minimizedCharts = minimizedCharts - key },
        )
    }
}
