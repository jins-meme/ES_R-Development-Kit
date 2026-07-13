package com.jins_jp.meme.core.ui.main

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Battery0Bar
import androidx.compose.material.icons.filled.Battery2Bar
import androidx.compose.material.icons.filled.Battery3Bar
import androidx.compose.material.icons.filled.Battery5Bar
import androidx.compose.material.icons.filled.Battery6Bar
import androidx.compose.material.icons.automirrored.filled.BatteryUnknown
import androidx.compose.material.icons.filled.BatteryFull
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.ble.ConnectionState

@OptIn(ExperimentalMaterial3Api::class)
@Composable
internal fun MeasureCard(ui: MainUiState, vm: MainViewModel) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(
                    stringResource(R.string.text_label_measure),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.primary,
                    modifier = Modifier.weight(1f),
                )
                if (ui.isMeasuring) {
                    Text(
                        "Recording(${ui.recordingRows})",
                        color = MaterialTheme.colorScheme.primary,
                    )
                    Box(Modifier.width(8.dp))
                }
                BatteryIcon(ui.batteryLevel)
            }

            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                HoldButton(
                    modifier = Modifier.weight(1f),
                    text = if (ui.isMeasuring) {
                        if (ui.mockEnabled) stringResource(R.string.button_replay_stop)
                        else stringResource(R.string.button_measurement_stop)
                    } else {
                        if (ui.mockEnabled) stringResource(R.string.button_replay_start)
                        else stringResource(R.string.button_measurement_start)
                    },
                    hintText = if (ui.isMeasuring) stringResource(R.string.hint_hold_to_stop)
                    else stringResource(R.string.hint_hold_to_start),
                    enabled = ui.connection == ConnectionState.ServicesReady && !ui.isStarting,
                    onConfirmed = { vm.toggleMeasurement() },
                )
                Button(
                    modifier = Modifier.weight(1f),
                    onClick = { vm.marking() },
                    enabled = ui.isMeasuring,
                ) { Text(stringResource(R.string.button_free_marking)) }
            }

            if (ui.mockEnabled && ui.isMeasuring) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    Button(
                        modifier = Modifier.weight(1f),
                        onClick = { vm.seekPlayback(-5.0) },
                    ) { Text(stringResource(R.string.button_replay_rewind)) }
                    Button(
                        modifier = Modifier.weight(1f),
                        onClick = {
                            if (ui.isPlaybackPaused) vm.resumePlayback() else vm.pausePlayback()
                        },
                    ) {
                        Text(
                            if (ui.isPlaybackPaused) stringResource(R.string.button_replay_resume)
                            else stringResource(R.string.button_replay_pause)
                        )
                    }
                    Button(
                        modifier = Modifier.weight(1f),
                        onClick = { vm.seekPlayback(5.0) },
                    ) { Text(stringResource(R.string.button_replay_forward)) }
                }
                // 再生位置 / 総時間。シークの結果（<< で戻った等）がひと目で分かるようにする。
                Text(
                    "%.1f / %.1f s".format(ui.replayPositionSec, ui.replayDurationSec),
                    modifier = Modifier.fillMaxWidth(),
                    style = MaterialTheme.typography.bodySmall,
                    textAlign = TextAlign.Center,
                )
            }

            if (ui.isStarting) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(20.dp),
                        strokeWidth = 2.dp,
                    )
                    Text(stringResource(R.string.text_label_starting))
                }
            }

            if (ui.isMeasuring) {
                LinearProgressIndicator(
                    progress = { ui.successRate.toFloat() },
                    modifier = Modifier.fillMaxWidth(),
                )
                Row(modifier = Modifier.fillMaxWidth()) {
                    Text(
                        "SUCCESS RATE: %.2f%%".format(ui.successRate * 100.0),
                        modifier = Modifier.weight(1f),
                        style = MaterialTheme.typography.bodySmall,
                    )
                    Text(
                        "COMM RATE: %.2f%%".format(ui.commRate * 100.0),
                        modifier = Modifier.weight(1f),
                        style = MaterialTheme.typography.bodySmall,
                    )
                }
            }
        }
    }
}

@Composable
private fun BatteryIcon(level: Int) {
    val (icon, desc) = when (level) {
        0 -> Icons.Filled.Battery0Bar to "0"
        1 -> Icons.Filled.Battery2Bar to "1"
        2 -> Icons.Filled.Battery3Bar to "2"
        3 -> Icons.Filled.Battery5Bar to "3"
        4 -> Icons.Filled.Battery6Bar to "4"
        5 -> Icons.Filled.BatteryFull to "5"
        else -> Icons.AutoMirrored.Filled.BatteryUnknown to "?"
    }
    AssistChip(onClick = {}, label = { Text(desc) }, leadingIcon = {
        Icon(icon, contentDescription = null)
    })
}
