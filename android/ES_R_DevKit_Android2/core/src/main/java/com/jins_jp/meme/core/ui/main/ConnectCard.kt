package com.jins_jp.meme.core.ui.main

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.ble.ConnectionState

@OptIn(ExperimentalMaterial3Api::class)
@Composable
internal fun ConnectCard(ui: MainUiState, vm: MainViewModel) {
    var showSettings by remember { mutableStateOf(false) }
    // Play button: pick a logged CSV, then replay it through the mock engine.
    val playbackCsvPicker = rememberLauncherForActivityResult(
        ActivityResultContracts.OpenDocument()
    ) { uri -> vm.startPlayback(uri) }
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(
                    stringResource(R.string.text_label_connect_meme),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.primary,
                    modifier = Modifier.weight(1f),
                )
                // 接続中(モック=再生中/実機いずれも)は再生ボタンを無効化する。
                val playbackEnabled = ui.connection == ConnectionState.Disconnected
                IconButton(
                    onClick = { playbackCsvPicker.launch(arrayOf("*/*")) },
                    enabled = playbackEnabled,
                ) {
                    Icon(
                        Icons.Filled.PlayArrow,
                        contentDescription = stringResource(R.string.button_playback),
                        tint = if (playbackEnabled) {
                            MaterialTheme.colorScheme.primary
                        } else {
                            MaterialTheme.colorScheme.onSurface.copy(alpha = 0.38f)
                        },
                    )
                }
                IconButton(onClick = { showSettings = true }) {
                    Icon(
                        Icons.Filled.Settings,
                        contentDescription = stringResource(R.string.text_label_settings),
                        tint = MaterialTheme.colorScheme.primary,
                    )
                }
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Button(
                    onClick = { vm.startScan() },
                    enabled = !ui.scanning && ui.connection == ConnectionState.Disconnected,
                ) { Text(stringResource(R.string.button_scan_device)) }
                Box(Modifier.width(8.dp))
                Box(Modifier.weight(1f)) {
                    DeviceDropdown(
                        devices = ui.devices,
                        selectedIndex = ui.selectedDeviceIndex,
                        enabled = ui.devices.isNotEmpty() && ui.connection == ConnectionState.Disconnected,
                        onSelected = vm::selectDevice,
                    )
                }
                if (ui.scanning) {
                    Box(Modifier.width(8.dp))
                    CircularProgressIndicator(modifier = Modifier.size(28.dp), strokeWidth = 3.dp)
                }
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Button(
                    onClick = { vm.connectOrDisconnect() },
                    enabled = ui.devices.isNotEmpty() &&
                            (ui.connection == ConnectionState.Disconnected ||
                                    ui.connection == ConnectionState.ServicesReady ||
                                    ui.connection == ConnectionState.Connected),
                ) {
                    Text(
                        if (ui.connection == ConnectionState.Disconnected)
                            stringResource(R.string.button_connect)
                        else stringResource(R.string.button_disconnect)
                    )
                }
                Box(Modifier.width(12.dp))
                Text(stringResource(R.string.text_label_status))
                Box(Modifier.width(4.dp))
                Text(
                    text = statusLabel(ui),
                    style = MaterialTheme.typography.bodyMedium,
                )
            }
        }
    }

    if (showSettings) {
        SettingsDialog(ui = ui, vm = vm, onDismiss = { showSettings = false })
    }
}

@Composable
private fun statusLabel(ui: MainUiState): String {
    if (ui.isReconnecting) return stringResource(R.string.text_state_reconnecting)
    return when (ui.connection) {
        ConnectionState.Disconnected -> stringResource(R.string.text_state_disconnect)
        ConnectionState.Connecting -> "Connecting…"
        ConnectionState.Connected -> "Connected"
        ConnectionState.ServicesReady ->
            if (ui.firmwareVersion != null) "Connected(ver.${ui.firmwareVersion})" else "Connected"
        ConnectionState.Disconnecting -> "Disconnecting…"
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun DeviceDropdown(
    devices: List<String>,
    selectedIndex: Int,
    enabled: Boolean,
    onSelected: (Int) -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }
    val current = devices.getOrNull(selectedIndex) ?: ""
    val borderAlpha = if (enabled) 1f else 0.38f
    val textAlpha = if (enabled) 1f else 0.38f
    val shape = RoundedCornerShape(50)
    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { if (enabled) expanded = it }) {
        Row(
            modifier = Modifier
                .menuAnchor(MenuAnchorType.PrimaryNotEditable, enabled)
                .fillMaxWidth()
                .height(40.dp)
                .border(
                    width = 1.dp,
                    color = MaterialTheme.colorScheme.outline.copy(alpha = borderAlpha),
                    shape = shape,
                )
                .padding(horizontal = 16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Text(
                text = current,
                modifier = Modifier.weight(1f),
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = textAlpha),
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
                style = MaterialTheme.typography.bodyMedium,
            )
            ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded)
        }
        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            devices.forEachIndexed { i, address ->
                DropdownMenuItem(
                    text = { Text(address) },
                    onClick = { onSelected(i); expanded = false },
                )
            }
        }
    }
}
