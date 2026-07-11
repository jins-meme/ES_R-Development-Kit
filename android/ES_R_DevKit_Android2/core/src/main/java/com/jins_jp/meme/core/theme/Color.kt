package com.jins_jp.meme.core.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Immutable
import androidx.compose.ui.graphics.Color

val MemeRed = Color(0xFFD00D2B)
val MemeRedDark = Color(0xFFBE0C27)
val MemeRedContainer = Color(0xFFFFE2E5)

val EogBlue = Color(0xFF1565C0)
val EogRed = Color(0xFFD32F2F)
val AccBlue = Color(0xFF1976D2)
val AccGreen = Color(0xFF388E3C)
val AccRed = Color(0xFFD32F2F)
val GyroBlue = Color(0xFF1976D2)
val GyroGreen = Color(0xFF388E3C)
val GyroRed = Color(0xFFD32F2F)

// ダークモード用の明るい系列色（暗い背景でも見やすいよう Material 300〜400 相当）。
val EogBlueBright = Color(0xFF64B5F6)
val EogRedBright = Color(0xFFFF8A80)
val AccBlueBright = Color(0xFF64B5F6)
val AccGreenBright = Color(0xFF81C784)
val AccRedBright = Color(0xFFE57373)
val GyroBlueBright = Color(0xFF64B5F6)
val GyroGreenBright = Color(0xFF81C784)
val GyroRedBright = Color(0xFFE57373)

/** チャート系列色。ライト/ダークで切り替えるため [chartSeriesColors] 経由で取得する。 */
@Immutable
data class ChartSeriesColors(
    val eogBlue: Color,
    val eogRed: Color,
    val accBlue: Color,
    val accGreen: Color,
    val accRed: Color,
    val gyroBlue: Color,
    val gyroGreen: Color,
    val gyroRed: Color,
)

/** 現在のテーマに応じた系列色。ダークでは暗い背景に埋もれない明るい色(*Bright)を返す。 */
@Composable
fun chartSeriesColors(dark: Boolean = isSystemInDarkTheme()): ChartSeriesColors =
    if (dark) {
        ChartSeriesColors(
            eogBlue = EogBlueBright, eogRed = EogRedBright,
            accBlue = AccBlueBright, accGreen = AccGreenBright, accRed = AccRedBright,
            gyroBlue = GyroBlueBright, gyroGreen = GyroGreenBright, gyroRed = GyroRedBright,
        )
    } else {
        ChartSeriesColors(
            eogBlue = EogBlue, eogRed = EogRed,
            accBlue = AccBlue, accGreen = AccGreen, accRed = AccRed,
            gyroBlue = GyroBlue, gyroGreen = GyroGreen, gyroRed = GyroRed,
        )
    }
