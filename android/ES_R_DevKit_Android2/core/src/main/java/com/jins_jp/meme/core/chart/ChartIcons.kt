package com.jins_jp.meme.core.chart

import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.graphics.vector.path
import androidx.compose.ui.unit.dp

/**
 * 縦軸ズームのアイコン。Material の虫めがね（ZoomIn/ZoomOut）は「拡大鏡＝全体を拡大」に
 * 見えてしまうので、「縦軸のレンジを狭める/広げる」ことが一目で分かる自前の図形を組む。
 * どちらも中央の横棒（＝軸）に対して上下の矢印が外向き（拡大）／内向き（縮小）。
 *
 * 24x24 のビューポートで、[androidx.compose.material3.Icon] の tint がそのまま乗るよう
 * 塗り色は不透明の黒（tint 未指定時のみ意味を持つ）にしてある。
 */

/** 縦軸拡大（レンジを半分にする）: 中央の軸から上下へ開く矢印。 */
val ChartZoomInIcon: ImageVector by lazy { buildZoomIcon(expand = true) }

/** 縦軸縮小（レンジを 2 倍にする）: 上下から中央の軸へ閉じる矢印。 */
val ChartZoomOutIcon: ImageVector by lazy { buildZoomIcon(expand = false) }

private fun buildZoomIcon(expand: Boolean): ImageVector =
    ImageVector.Builder(
        name = if (expand) "ChartZoomIn" else "ChartZoomOut",
        defaultWidth = 24.dp,
        defaultHeight = 24.dp,
        viewportWidth = 24f,
        viewportHeight = 24f,
    ).apply {
        val fill = SolidColor(Color.Black)
        // 中央の軸（横棒）。上下の矢印はこの棒を基準に開く/閉じる。
        path(fill = fill) {
            moveTo(3f, 11f); lineTo(21f, 11f); lineTo(21f, 13f); lineTo(3f, 13f); close()
        }
        if (expand) {
            // 上向き矢印（軸の上→外側へ）＋軸へ向かう軸棒。
            path(fill = fill) {
                moveTo(12f, 2f); lineTo(17f, 8f); lineTo(7f, 8f); close()
            }
            path(fill = fill) {
                moveTo(11f, 7f); lineTo(13f, 7f); lineTo(13f, 10f); lineTo(11f, 10f); close()
            }
            // 下向き矢印（軸の下→外側へ）。
            path(fill = fill) {
                moveTo(12f, 22f); lineTo(7f, 16f); lineTo(17f, 16f); close()
            }
            path(fill = fill) {
                moveTo(11f, 14f); lineTo(13f, 14f); lineTo(13f, 17f); lineTo(11f, 17f); close()
            }
        } else {
            // 上から軸へ向かう矢印（下向き）。
            path(fill = fill) {
                moveTo(12f, 10f); lineTo(7f, 4f); lineTo(17f, 4f); close()
            }
            path(fill = fill) {
                moveTo(11f, 2f); lineTo(13f, 2f); lineTo(13f, 5f); lineTo(11f, 5f); close()
            }
            // 下から軸へ向かう矢印（上向き）。
            path(fill = fill) {
                moveTo(12f, 14f); lineTo(17f, 20f); lineTo(7f, 20f); close()
            }
            path(fill = fill) {
                moveTo(11f, 19f); lineTo(13f, 19f); lineTo(13f, 22f); lineTo(11f, 22f); close()
            }
        }
    }.build()
