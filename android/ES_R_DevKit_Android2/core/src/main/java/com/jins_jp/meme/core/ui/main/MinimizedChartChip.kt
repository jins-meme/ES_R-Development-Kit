package com.jins_jp.meme.core.ui.main

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.BarChart
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable

/** A minimized chart, rendered as a tappable title chip that restores the chart. */
@Composable
fun MinimizedChartChip(title: String, onClick: () -> Unit) {
    AssistChip(
        onClick = onClick,
        label = { Text(title) },
        leadingIcon = { Icon(Icons.Filled.BarChart, contentDescription = null) },
    )
}
