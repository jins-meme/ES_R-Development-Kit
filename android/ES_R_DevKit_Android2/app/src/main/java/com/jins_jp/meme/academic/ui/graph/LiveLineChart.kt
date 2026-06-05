package com.jins_jp.meme.academic.ui.graph

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp

data class LineSeries(val color: Color, val values: FloatArray)

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
    modifier: Modifier = Modifier,
    height: Dp = 180.dp,
) {
    val gridColor = MaterialTheme.colorScheme.outlineVariant
    val titleColor = MaterialTheme.colorScheme.onSurfaceVariant
    Box(
        modifier = modifier
            .fillMaxWidth()
            .padding(vertical = 4.dp),
    ) {
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .height(height)
                .background(MaterialTheme.colorScheme.surface),
        ) {
            Canvas(modifier = Modifier.matchParentSize()) {
                val w = size.width
                val h = size.height
                val span = (yMax - yMin).coerceAtLeast(1f)
                // grid: 4 horizontal lines
                for (i in 1..3) {
                    val y = h * i / 4f
                    drawLine(gridColor, Offset(0f, y), Offset(w, y), strokeWidth = 1f)
                }
                series.forEach { s ->
                    if (s.values.size < 2) return@forEach
                    val step = w / (s.values.size - 1).toFloat()
                    val path = Path()
                    s.values.forEachIndexed { i, v ->
                        val x = i * step
                        val ny = ((v - yMin) / span).coerceIn(0f, 1f)
                        val y = h - ny * h
                        if (i == 0) path.moveTo(x, y) else path.lineTo(x, y)
                    }
                    drawPath(path, color = s.color, style = Stroke(width = 2f))
                }
            }
            Text(
                text = title,
                color = titleColor,
                modifier = Modifier
                    .align(Alignment.TopStart)
                    .padding(start = 8.dp, top = 4.dp),
            )
        }
    }
}
