package com.jins_jp.meme.core.chart

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Remove
import androidx.compose.material3.Card
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.PathEffect
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Fill
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.withTransform
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.text.TextMeasurer
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlin.math.abs
import kotlin.math.ceil
import kotlin.math.floor

/** 系列が従属するY軸（左=主軸 / 右=副軸）。 */
enum class Axis { Left, Right }

data class LineSeries(
    val color: Color,
    val values: FloatArray,
    val axis: Axis = Axis.Left,
    val label: String = "",
    // 非 null のとき、この系列のトレンド（呼び出し側で計算した基線など）を系列色の
    // 点線で重ね描きする。要素数は values と同じで、NaN は「値なし」＝線を切る
    // （例: 右端の未確定な半窓）。計算方法は呼び出し側の自由（移動平均・メディアン等）。
    val trend: FloatArray? = null,
)

/** マーカーの図形。◯は輪郭線、三角は塗りで描く。 */
enum class MarkerShape { CircleOutline, TriangleUp, TriangleDown, TriangleLeft, TriangleRight }

/**
 * グラフに重ねる汎用イベントマーカー。
 *
 * @param xFraction 0..1 のグラフ横位置（0=左端の最古点, 1=右端の最新点）。
 * @param value 主軸（左軸）スケールでの y 値。レンジ外は上下端にクランプされる。
 * @param shape 描画する図形。
 * @param color 描画色（ライト/ダークの使い分けは呼び出し側で行う）。
 * @param sizeDp 図形の半径 [dp]。
 */
data class ChartMarker(
    val xFraction: Float,
    val value: Float,
    val shape: MarkerShape,
    val color: Color,
    val sizeDp: Float = 5f,
)

/**
 * グラフの一定の高さ（[LiveLineChart] の bandValue）に重ねる色帯の 1 区間。
 *
 * @param startFraction 0..1 のグラフ横位置（区間開始）。
 * @param endFraction 0..1 のグラフ横位置（区間終了）。
 * @param color 区間の色。
 */
data class ChartBandSegment(
    val startFraction: Float,
    val endFraction: Float,
    val color: Color,
)

/**
 * グラフに重ねる文字ラベル。
 *
 * 横書き（rotated=false）はチャート上端付近に [row] で段違い配置（重なり回避）。
 * 90°回転（rotated=true）は [yFraction] を基点に下から上へ読む向きで描く。
 * 高頻度に出るラベルは回転させると横方向の重なりを避けられる。
 *
 * @param xFraction 0..1 のグラフ横位置（0=左端の最古点, 1=右端の最新点）。
 * @param text 表示文字。
 * @param color 描画色（ライト/ダークの使い分けは呼び出し側で行う）。
 * @param rotated true のとき -90°回転（下から上へ読む）。
 * @param row 横書き時の段違い行（0=最上段, 1=ひとつ下, …）。
 * @param yFraction 回転時の基点（0=上端, 1=下端のグラフ縦位置）。
 * @param bold 太字にするか。
 * @param fontSizeSp フォントサイズ [sp]。
 */
data class ChartTextLabel(
    val xFraction: Float,
    val text: String,
    val color: Color,
    val rotated: Boolean = false,
    val row: Int = 0,
    val yFraction: Float = 0.30f,
    val bold: Boolean = false,
    val fontSizeSp: Float = 11f,
)

/** チャート下部に出す凡例の 1 項目（色帯の状態凡例など）。 */
data class LegendEntry(val color: Color, val label: String)

/**
 * Lightweight, allocation-friendly Canvas chart for live data.
 * Vico is great for static datasets, but we re-render every packet (50–100Hz);
 * a hand-rolled Canvas keeps overhead down without adding Choreographer skips.
 *
 * 左端内側に主軸の目盛・目盛ラベルを描画する。[rightYMin]/[rightYMax] を与えると
 * 右端内側に副軸の目盛ラベルも描画し、[Axis.Right] の系列を副軸レンジでスケールする。
 */
@Composable
fun LiveLineChart(
    title: String,
    series: List<LineSeries>,
    yMin: Float,
    yMax: Float,
    onMinimize: () -> Unit,
    modifier: Modifier = Modifier,
    height: Dp = 171.dp,
    rightYMin: Float? = null,
    rightYMax: Float? = null,
    // 汎用イベントマーカー（主軸スケールで重ね描き）。
    markers: List<ChartMarker> = emptyList(),
    // 汎用文字ラベル（横書き=上端の段違い / 回転=yFraction 基点）。
    textLabels: List<ChartTextLabel> = emptyList(),
    // 色帯。[bandValue] が非 null のとき、その左軸 y 値の高さに区間を色分けして描く。
    bandSegments: List<ChartBandSegment> = emptyList(),
    bandValue: Float? = null,
    // 非空のとき、チャート下部に色凡例を折り返し表示する（色帯の状態凡例など）。
    bandLegend: List<LegendEntry> = emptyList(),
    // 軸名ラベル。非 null のとき軸上端付近（最上段の目盛ラベルの下）に、その軸に
    // 属する先頭系列の色で描く（例: 左="h" / 右="v"。どちらの軸か一目で分かるように）。
    leftAxisName: String? = null,
    rightAxisName: String? = null,
    headerAction: (@Composable () -> Unit)? = null,
    onTapFraction: ((Float) -> Unit)? = null,
    // X軸に流す経過時間。右端(最新点)の経過秒数と、1プロット点あたりの秒数。
    // null の場合は時間軸を描かない。
    xRightSeconds: Float? = null,
    xSecondsPerPoint: Float = 0.04f,
) {
    val gridColor = MaterialTheme.colorScheme.outlineVariant
    val axisColor = MaterialTheme.colorScheme.onSurfaceVariant
    val textMeasurer = rememberTextMeasurer()
    Card(modifier = modifier.fillMaxWidth()) {
        Column(Modifier.padding(12.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text(
                    text = title,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.primary,
                )
                // Legend, laid out in a single horizontal row to the right of the title.
                Row(
                    modifier = Modifier
                        .weight(1f)
                        .padding(horizontal = 8.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    series.forEach { s ->
                        if (s.label.isNotEmpty()) LegendItem(color = s.color, label = s.label)
                    }
                }
                headerAction?.invoke()
                MinimizeButton(onClick = onMinimize)
            }
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(height)
                    .padding(top = 8.dp)
                    .then(
                        if (onTapFraction != null) {
                            // 水平方向の padding は無いので、タップ x はそのまま系列の
                            // 描画範囲(0..幅)に対応する。f=0:左端の最古点 / f=1:右端の最新点。
                            Modifier.pointerInput(Unit) {
                                detectTapGestures { offset ->
                                    val f = (offset.x / size.width.toFloat()).coerceIn(0f, 1f)
                                    onTapFraction(f)
                                }
                            }
                        } else {
                            Modifier
                        },
                    ),
            ) {
                Canvas(modifier = Modifier.matchParentSize()) {
                    val w = size.width
                    val h = size.height
                    val span = (yMax - yMin).coerceAtLeast(1f)
                    for (i in 1..3) {
                        val y = h * i / 4f
                        drawLine(gridColor, Offset(0f, y), Offset(w, y), strokeWidth = 1f)
                    }
                    series.forEach { s ->
                        if (s.values.size < 2) return@forEach
                        val useRight = s.axis == Axis.Right && rightYMin != null && rightYMax != null
                        val lo = if (useRight) rightYMin else yMin
                        val hi = if (useRight) rightYMax else yMax
                        val sp = (hi - lo).coerceAtLeast(1f)
                        val step = w / (s.values.size - 1).toFloat()
                        // レンジ内/外で描き分ける。レンジ外は端にクランプした上で同色・薄め
                        // (alpha)で描く。区間の端点いずれかがレンジ外なら薄線扱いにする。
                        val inPath = Path()
                        val outPath = Path()
                        var prevX = 0f
                        var prevY = 0f
                        var prevOut = false
                        // NaN は「値なし」＝線を切る（未確定な右端半窓など）。
                        var prevNan = true
                        s.values.forEachIndexed { i, v ->
                            if (v.isNaN()) {
                                prevNan = true
                                return@forEachIndexed
                            }
                            val out = v < lo || v > hi
                            val ny = ((v - lo) / sp).coerceIn(0f, 1f)
                            val x = i * step
                            val y = h - ny * h
                            if (i > 0 && !prevNan) {
                                val seg = if (out || prevOut) outPath else inPath
                                seg.moveTo(prevX, prevY)
                                seg.lineTo(x, y)
                            }
                            prevX = x; prevY = y; prevOut = out; prevNan = false
                        }
                        drawPath(inPath, color = s.color, style = Stroke(width = 2f))
                        drawPath(outPath, color = s.color.copy(alpha = 0.25f), style = Stroke(width = 2f))
                    }
                    // トレンド（呼び出し側で計算済みの基線など）を系列色の点線で重ねる。
                    series.forEach { s ->
                        val trend = s.trend ?: return@forEach
                        if (trend.size < 2) return@forEach
                        val useRight = s.axis == Axis.Right && rightYMin != null && rightYMax != null
                        val lo = if (useRight) rightYMin else yMin
                        val hi = if (useRight) rightYMax else yMax
                        drawTrendLine(trend, lo, hi, s.color)
                    }
                    // 目盛・目盛ラベル（軸は系列より前面に描く）
                    drawAxisTicks(yMin, yMax, right = false, textMeasurer, axisColor)
                    if (rightYMin != null && rightYMax != null) {
                        drawAxisTicks(rightYMin, rightYMax, right = true, textMeasurer, axisColor)
                    }
                    // 軸名ラベル（軸上端付近、系列色）
                    if (leftAxisName != null) {
                        val c = series.firstOrNull { it.axis == Axis.Left }?.color ?: axisColor
                        drawAxisName(leftAxisName, right = false, textMeasurer, c)
                    }
                    if (rightAxisName != null && rightYMin != null && rightYMax != null) {
                        val c = series.firstOrNull { it.axis == Axis.Right }?.color ?: axisColor
                        drawAxisName(rightAxisName, right = true, textMeasurer, c)
                    }
                    // 経過時間(理論値)の1秒グリッド＋ラベル。データが進むと右→左へ流れる。
                    if (xRightSeconds != null) {
                        drawTimeAxis(
                            pointCount = series.firstOrNull()?.values?.size ?: 0,
                            rightSeconds = xRightSeconds,
                            secondsPerPoint = xSecondsPerPoint,
                            textMeasurer = textMeasurer,
                            gridColor = gridColor,
                            textColor = axisColor,
                        )
                    }
                    // イベントマーカー（主軸スケールで重ね描き）。範囲（±レンジ）を超えた
                    // マーカーは端（±レンジの位置）に張り付ける＝coerceIn で上下端にクランプ。
                    if (markers.isNotEmpty()) {
                        for (m in markers) {
                            val x = (m.xFraction * w).coerceIn(0f, w)
                            val ny = ((m.value - yMin) / span).coerceIn(0f, 1f)
                            drawMarker(m.shape, x, h - ny * h, m.color, m.sizeDp.dp.toPx())
                        }
                    }
                    // 文字ラベル（横書き=上端の段違い / 回転=yFraction 基点）。
                    if (textLabels.isNotEmpty()) {
                        for (l in textLabels) drawTextLabel(l, textMeasurer)
                    }
                    // 色帯（区間ごとに色分け）。[bandValue] の左軸 y へ描く。
                    // レンジ外（チャート下端より下）なら端に張り付けて全幅見えるようにクランプする。
                    if (bandValue != null && bandSegments.isNotEmpty()) {
                        val bandStroke = 6.dp.toPx()
                        val ny = ((bandValue - yMin) / span).coerceIn(0f, 1f)
                        val cy = (h - ny * h).coerceIn(bandStroke / 2f, h - bandStroke / 2f)
                        for (seg in bandSegments) {
                            val x0 = (seg.startFraction * w).coerceIn(0f, w)
                            val x1 = (seg.endFraction * w).coerceIn(0f, w)
                            if (x1 <= x0) continue
                            drawLine(
                                seg.color,
                                Offset(x0, cy), Offset(x1, cy),
                                strokeWidth = bandStroke, cap = StrokeCap.Butt,
                            )
                        }
                    }
                }
            }
            // 色帯の凡例（チャート下部）。凡例を与えたグラフだけに出す。
            if (bandLegend.isNotEmpty()) {
                BandLegend(bandLegend)
            }
        }
    }
}

/**
 * 色凡例をチャート下部に折り返し表示する。
 *
 * `FlowRow` は端末の Compose Foundation 版とコンパイル時の ABI がズレて `NoSuchMethodError` に
 * なり得る（新しい `FlowRowOverflow` 付きオーバーロードが実行時に無い）ので、長期安定な
 * [Layout] だけで横並び＋幅オーバーで折り返す簡易フローを自前で組む。
 */
@Composable
private fun BandLegend(entries: List<LegendEntry>) {
    Layout(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = 6.dp),
        content = {
            for (e in entries) LegendItem(color = e.color, label = e.label)
        },
    ) { measurables, constraints ->
        val hGap = 10.dp.roundToPx()
        val vGap = 2.dp.roundToPx()
        val maxW = constraints.maxWidth
        val placeables = measurables.map { it.measure(constraints.copy(minWidth = 0)) }
        var x = 0; var y = 0; var rowH = 0
        val positions = ArrayList<IntOffset>(placeables.size)
        for (p in placeables) {
            if (x > 0 && x + p.width > maxW) { x = 0; y += rowH + vGap; rowH = 0 }
            positions.add(IntOffset(x, y))
            x += p.width + hGap
            rowH = maxOf(rowH, p.height)
        }
        layout(maxW, y + rowH) {
            placeables.forEachIndexed { i, p -> p.place(positions[i]) }
        }
    }
}

@Composable
private fun LegendItem(color: Color, label: String) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Box(
            modifier = Modifier
                .size(10.dp)
                .clip(RoundedCornerShape(2.dp))
                .background(color),
        )
        Spacer(Modifier.width(4.dp))
        Text(
            text = label,
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}

/** White, semi-transparent minus button in the top-right that minimizes a card/chart. */
@Composable
fun MinimizeButton(onClick: () -> Unit) {
    Box(
        modifier = Modifier
            .size(28.dp)
            .clip(CircleShape)
            .background(Color.White.copy(alpha = 0.5f))
            .clickable(onClick = onClick),
        contentAlignment = Alignment.Center,
    ) {
        Icon(
            imageVector = Icons.Filled.Remove,
            contentDescription = "Minimize chart",
            tint = Color.Black.copy(alpha = 0.6f),
            modifier = Modifier.size(18.dp),
        )
    }
}

/** 図形マーカーを (cx, cy) 中心・半径 r で描く。◯は輪郭線、三角は塗り。 */
private fun DrawScope.drawMarker(shape: MarkerShape, cx: Float, cy: Float, color: Color, r: Float) {
    when (shape) {
        MarkerShape.CircleOutline ->
            drawCircle(color, radius = r, center = Offset(cx, cy), style = Stroke(width = 2f))
        MarkerShape.TriangleUp -> drawPath(triangle(cx, cy, r, Dir.UP), color, style = Fill)
        MarkerShape.TriangleDown -> drawPath(triangle(cx, cy, r, Dir.DOWN), color, style = Fill)
        MarkerShape.TriangleRight -> drawPath(triangle(cx, cy, r, Dir.RIGHT), color, style = Fill)
        MarkerShape.TriangleLeft -> drawPath(triangle(cx, cy, r, Dir.LEFT), color, style = Fill)
    }
}

/**
 * 文字ラベルを描く。
 * 横書き = チャート上端付近に [ChartTextLabel.row] で段違い配置。
 * 回転 = [ChartTextLabel.yFraction] を基点に -90°回転（下から上へ読む）。
 */
private fun DrawScope.drawTextLabel(
    label: ChartTextLabel,
    textMeasurer: TextMeasurer,
) {
    val w = size.width
    val h = size.height
    val x = (label.xFraction * w).coerceIn(0f, w)
    val style = TextStyle(
        fontSize = label.fontSizeSp.sp,
        color = label.color,
        fontWeight = if (label.bold) FontWeight.Bold else FontWeight.Normal,
    )
    val layout = textMeasurer.measure(label.text, style)
    if (!label.rotated) {
        val tx = (x - layout.size.width / 2f)
            .coerceIn(0f, (w - layout.size.width).coerceAtLeast(0f))
        val ty = 2.dp.toPx() + label.row * (layout.size.height + 1.dp.toPx())
        drawText(layout, topLeft = Offset(tx, ty))
    } else {
        // -90°回転で topLeft=pivot の文字は x∈[px, px+文字高], y∈[yBase-文字幅, yBase] を
        // 占める（下から上へ読む）。x 中心に合わせ、yBase から上へ伸ばす。
        val yBase = h * label.yFraction
        val pivot = Offset(x - layout.size.height / 2f, yBase)
        withTransform({ rotate(degrees = -90f, pivot = pivot) }) {
            drawText(layout, topLeft = pivot)
        }
    }
}

private enum class Dir { UP, DOWN, LEFT, RIGHT }

private fun triangle(cx: Float, cy: Float, r: Float, dir: Dir): Path = Path().apply {
    when (dir) {
        Dir.UP -> { moveTo(cx, cy - r); lineTo(cx - r, cy + r); lineTo(cx + r, cy + r) }
        Dir.DOWN -> { moveTo(cx, cy + r); lineTo(cx - r, cy - r); lineTo(cx + r, cy - r) }
        Dir.RIGHT -> { moveTo(cx + r, cy); lineTo(cx - r, cy - r); lineTo(cx - r, cy + r) }
        Dir.LEFT -> { moveTo(cx - r, cy); lineTo(cx + r, cy - r); lineTo(cx + r, cy + r) }
    }
    close()
}

/**
 * 系列のトレンド（呼び出し側で計算済み）を系列色の点線で描く。
 * NaN は「値なし」＝線を切る（例: 未確定な右端の先読みぶん）。
 * 毎フレーム呼ばれるが n≈150 で軽い。
 */
private fun DrawScope.drawTrendLine(
    trend: FloatArray,
    yMin: Float,
    yMax: Float,
    color: Color,
) {
    val n = trend.size
    val w = size.width
    val h = size.height
    val span = (yMax - yMin).coerceAtLeast(1f)
    val step = w / (n - 1).toFloat()
    val path = Path()
    var prevValid = false
    for (i in 0 until n) {
        val v = trend[i]
        if (v.isNaN()) {
            prevValid = false
            continue
        }
        val ny = ((v - yMin) / span).coerceIn(0f, 1f)
        val x = i * step
        val y = h - ny * h
        if (prevValid) path.lineTo(x, y) else path.moveTo(x, y)
        prevValid = true
    }
    drawPath(
        path,
        color = color.copy(alpha = 0.85f),
        style = Stroke(
            width = 2f,
            pathEffect = PathEffect.dashPathEffect(floatArrayOf(8f, 8f)),
        ),
    )
}

/** 軸名（例: "h"/"v"）を軸上端付近＝最上段の目盛ラベルの下に系列色で描く。 */
private fun DrawScope.drawAxisName(
    name: String,
    right: Boolean,
    textMeasurer: TextMeasurer,
    color: Color,
) {
    val tickLen = 6.dp.toPx()
    val pad = 2.dp.toPx()
    val style = TextStyle(fontSize = 10.sp, color = color, fontWeight = FontWeight.Bold)
    val layout = textMeasurer.measure(name, style)
    // 最上段の目盛ラベル（9sp）の下に重ならないよう1行分下げる
    val ty = 12.sp.toPx() + pad
    val tx = if (right) size.width - tickLen - pad - layout.size.width else tickLen + pad
    drawText(layout, topLeft = Offset(tx, ty))
}

/** 左端(または右端)内側に5段の目盛線と目盛ラベルを描画する。 */
private fun DrawScope.drawAxisTicks(
    yMin: Float,
    yMax: Float,
    right: Boolean,
    textMeasurer: TextMeasurer,
    color: Color,
) {
    val w = size.width
    val h = size.height
    val tickLen = 6.dp.toPx()
    val pad = 2.dp.toPx()
    val style = TextStyle(fontSize = 9.sp, color = color)
    for (i in 0..4) {
        val y = h * i / 4f
        val value = yMax - (yMax - yMin) * i / 4f
        if (right) {
            drawLine(color, Offset(w - tickLen, y), Offset(w, y), strokeWidth = 1f)
        } else {
            drawLine(color, Offset(0f, y), Offset(tickLen, y), strokeWidth = 1f)
        }
        val layout = textMeasurer.measure(formatTick(value), style)
        val ty = (y - layout.size.height / 2f).coerceIn(0f, h - layout.size.height.toFloat())
        val tx = if (right) w - tickLen - pad - layout.size.width else tickLen + pad
        drawText(layout, topLeft = Offset(tx, ty))
    }
}

/**
 * X軸に経過時間（秒）の1秒グリッドとラベルを描く。右端(=最新点)が [rightSeconds]、
 * 1プロット点あたり [secondsPerPoint] 秒。データが進むとラベルは右から左へ流れる。
 * 縦線は控えめなグリッド色、ラベルはチャート下端に描く。負の秒数は描かない。
 */
private fun DrawScope.drawTimeAxis(
    pointCount: Int,
    rightSeconds: Float,
    secondsPerPoint: Float,
    textMeasurer: TextMeasurer,
    gridColor: Color,
    textColor: Color,
) {
    if (pointCount < 2 || secondsPerPoint <= 0f) return
    val w = size.width
    val h = size.height
    val spanSec = (pointCount - 1) * secondsPerPoint
    if (spanSec <= 0f) return
    val leftSeconds = rightSeconds - spanSec
    val style = TextStyle(fontSize = 9.sp, color = textColor)
    val pad = 2.dp.toPx()
    var s = ceil(leftSeconds).toInt().coerceAtLeast(0)
    val sEnd = floor(rightSeconds).toInt()
    while (s <= sEnd) {
        val x = ((s - leftSeconds) / spanSec) * w
        drawLine(gridColor, Offset(x, 0f), Offset(x, h), strokeWidth = 1f)
        val layout = textMeasurer.measure("$s", style)
        val tx = (x + pad).coerceIn(0f, (w - layout.size.width).coerceAtLeast(0f))
        drawText(layout, topLeft = Offset(tx, h - layout.size.height - pad))
        s++
    }
}

/** レンジの大きさに応じて桁数を切り替える目盛ラベル整形。 */
private fun formatTick(v: Float): String {
    val a = abs(v)
    return when {
        a < 1e-4f -> "0"
        a >= 100f -> "%.0f".format(v)
        a >= 1f -> "%.1f".format(v)
        else -> "%.2f".format(v)
    }
}
