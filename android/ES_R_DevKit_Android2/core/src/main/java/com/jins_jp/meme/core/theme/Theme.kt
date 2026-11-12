package com.jins_jp.meme.core.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val LightScheme = lightColorScheme(
    primary = MemePink,
    onPrimary = androidx.compose.ui.graphics.Color.White,
    primaryContainer = MemePinkContainer,
    onPrimaryContainer = MemePinkDeep,
    secondary = MemePinkDeep,
    onSecondary = androidx.compose.ui.graphics.Color.White,
)

private val DarkScheme = darkColorScheme(
    primary = MemePinkDarkMode,
    onPrimary = androidx.compose.ui.graphics.Color.White,
    secondary = MemePink,
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
