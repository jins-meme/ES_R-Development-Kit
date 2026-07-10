package com.jins_jp.meme.core.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val LightScheme = lightColorScheme(
    primary = MemeRed,
    onPrimary = androidx.compose.ui.graphics.Color.White,
    primaryContainer = MemeRedContainer,
    onPrimaryContainer = MemeRedDark,
    secondary = MemeRedDark,
    onSecondary = androidx.compose.ui.graphics.Color.White,
)

private val DarkScheme = darkColorScheme(
    primary = MemeRed,
    onPrimary = androidx.compose.ui.graphics.Color.White,
    secondary = MemeRedDark,
)

@Composable
fun ESRTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit,
) {
    MaterialTheme(
        colorScheme = if (darkTheme) DarkScheme else LightScheme,
        content = content,
    )
}
