package com.jins_jp.meme.academic.ui.graph

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
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
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.TextMeasurer
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import kotlin.math.abs
import kotlin.math.ceil
import kotlin.math.floor
import kotlin.math.roundToInt

data class LineSeries(val color: Color, val values: FloatArray, val label: String = "")

/**
 * Lightweight, allocation-friendly Canvas chart for live data.
 * Vico is great for static datasets, but we re-render every packet (50–100Hz);
 * a hand-rolled Canvas keeps overhead down without adding Choreographer skips.
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
    // X軸に流す経過時間。右端(最新点)の経過秒数と、1プロット点あたりの秒数。
    // null の場合は時間軸を描かない。
    xRightSeconds: Float? = null,
    xSecondsPerPoint: Float = 0.04f,
) {
    val gridColor = MaterialTheme.colorScheme.outlineVariant
    val textMeasurer = rememberTextMeasurer()
    val labelStyle = MaterialTheme.typography.labelSmall.copy(
        color = MaterialTheme.colorScheme.onSurfaceVariant,
    )
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
                MinimizeButton(onClick = onMinimize)
            }
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(height)
                    .padding(top = 8.dp),
            ) {
                Canvas(modifier = Modifier.matchParentSize()) {
                    val w = size.width
                    val h = size.height
                    val span = (yMax - yMin).coerceAtLeast(1f)

                    // Y-axis tick labels down the left edge: yMax at top … yMin at bottom.
                    // 4 divisions => 5 ticks; the plot area is shifted right by the gutter.
                    val ticks = 4
                    val labels = Array(ticks + 1) {
                        textMeasurer.measure(formatTick(yMax - (yMax - yMin) * it / ticks), labelStyle)
                    }
                    val labelGap = 4.dp.toPx()
                    val gutter = labels.maxOf { it.size.width }.toFloat() + labelGap
                    val plotWidth = (w - gutter).coerceAtLeast(1f)

                    for (i in 0..ticks) {
                        val y = h * i / ticks
                        // Inner gridlines only; top & bottom are the chart frame edges.
                        if (i in 1 until ticks) {
                            drawLine(gridColor, Offset(gutter, y), Offset(w, y), strokeWidth = 1f)
                        }
                        val label = labels[i]
                        val ty = (y - label.size.height / 2f).coerceIn(0f, h - label.size.height)
                        drawText(label, topLeft = Offset(gutter - labelGap - label.size.width, ty))
                    }

                    series.forEach { s ->
                        if (s.values.size < 2) return@forEach
                        val step = plotWidth / (s.values.size - 1).toFloat()
                        // レンジ内/外で描き分ける。レンジ外は端にクランプした上で同色・薄め
                        // (alpha)で描く。区間の端点いずれかがレンジ外なら薄線扱いにする。
                        val inPath = Path()
                        val outPath = Path()
                        var prevX = 0f
                        var prevY = 0f
                        var prevOut = false
                        s.values.forEachIndexed { i, v ->
                            val out = v < yMin || v > yMax
                            val ny = ((v - yMin) / span).coerceIn(0f, 1f)
                            val x = gutter + i * step
                            val y = h - ny * h
                            if (i > 0) {
                                val seg = if (out || prevOut) outPath else inPath
                                seg.moveTo(prevX, prevY)
                                seg.lineTo(x, y)
                            }
                            prevX = x; prevY = y; prevOut = out
                        }
                        drawPath(inPath, color = s.color, style = Stroke(width = 2f))
                        drawPath(outPath, color = s.color.copy(alpha = 0.25f), style = Stroke(width = 2f))
                    }

                    // 経過時間(理論値)の1秒グリッド＋ラベル。データが進むと右→左へ流れる。
                    if (xRightSeconds != null) {
                        drawTimeAxis(
                            pointCount = series.firstOrNull()?.values?.size ?: 0,
                            gutter = gutter,
                            plotWidth = plotWidth,
                            rightSeconds = xRightSeconds,
                            secondsPerPoint = xSecondsPerPoint,
                            textMeasurer = textMeasurer,
                            style = labelStyle,
                            gridColor = gridColor,
                        )
                    }
                }
            }
        }
    }
}

/**
 * X軸に経過時間（秒）の1秒グリッドとラベルを描く。右端(=最新点)が [rightSeconds]、
 * 1プロット点あたり [secondsPerPoint] 秒。データが進むとラベルは右から左へ流れる。
 * プロット領域 [[gutter], width] の範囲に収め、ラベルはチャート下端に描く。
 */
private fun DrawScope.drawTimeAxis(
    pointCount: Int,
    gutter: Float,
    plotWidth: Float,
    rightSeconds: Float,
    secondsPerPoint: Float,
    textMeasurer: TextMeasurer,
    style: TextStyle,
    gridColor: Color,
) {
    if (pointCount < 2 || secondsPerPoint <= 0f || plotWidth <= 0f) return
    val h = size.height
    val spanSec = (pointCount - 1) * secondsPerPoint
    if (spanSec <= 0f) return
    val leftSeconds = rightSeconds - spanSec
    val pad = 2.dp.toPx()
    var s = ceil(leftSeconds).toInt().coerceAtLeast(0)
    val sEnd = floor(rightSeconds).toInt()
    while (s <= sEnd) {
        val x = gutter + ((s - leftSeconds) / spanSec) * plotWidth
        drawLine(gridColor, Offset(x, 0f), Offset(x, h), strokeWidth = 1f)
        val layout = textMeasurer.measure("$s", style)
        val tx = (x + pad).coerceIn(gutter, (size.width - layout.size.width).coerceAtLeast(gutter))
        drawText(layout, topLeft = Offset(tx, h - layout.size.height - pad))
        s++
    }
}

/** Tick value formatting: whole numbers stay integers, otherwise one decimal place. */
private fun formatTick(value: Float): String {
    val rounded = value.roundToInt()
    return if (abs(value - rounded) < 0.001f) rounded.toString() else "%.1f".format(value)
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

/** White, semi-transparent minus button in the top-right that minimizes the chart. */
@Composable
private fun MinimizeButton(onClick: () -> Unit) {
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
