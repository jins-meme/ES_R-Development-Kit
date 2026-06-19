package com.jins_jp.meme.academic.ui.main

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.tween
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.awaitEachGesture
import androidx.compose.foundation.gestures.awaitFirstDown
import androidx.compose.foundation.gestures.waitForUpOrCancellation
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Battery0Bar
import androidx.compose.material.icons.filled.Battery2Bar
import androidx.compose.material.icons.filled.Battery3Bar
import androidx.compose.material.icons.filled.Battery5Bar
import androidx.compose.material.icons.filled.Battery6Bar
import androidx.compose.material.icons.automirrored.filled.BatteryUnknown
import androidx.compose.material.icons.filled.BarChart
import androidx.compose.material.icons.filled.BatteryFull
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Surface
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.IntRect
import androidx.compose.ui.unit.IntSize
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Popup
import androidx.compose.ui.window.PopupPositionProvider
import androidx.compose.ui.window.PopupProperties
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.jins_jp.meme.academic.BuildConfig
import com.jins_jp.meme.academic.R
import com.jins_jp.meme.academic.ble.ConnectionState
import com.jins_jp.meme.academic.data.AccRange
import com.jins_jp.meme.academic.data.GyroRange
import com.jins_jp.meme.academic.data.MemeMode
import com.jins_jp.meme.academic.data.MemeQuality
import com.jins_jp.meme.academic.ui.graph.LineSeries
import com.jins_jp.meme.academic.ui.graph.LiveLineChart
import com.jins_jp.meme.academic.ui.theme.AccBlue
import com.jins_jp.meme.academic.ui.theme.AccGreen
import com.jins_jp.meme.academic.ui.theme.AccRed
import com.jins_jp.meme.academic.ui.theme.EogBlue
import com.jins_jp.meme.academic.ui.theme.EogRed
import com.jins_jp.meme.academic.ui.theme.GyroBlue
import com.jins_jp.meme.academic.ui.theme.GyroGreen
import com.jins_jp.meme.academic.ui.theme.GyroRed
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

// 6 seconds at the 25 Hz plot rate (100 Hz ÷ 4 = 50 Hz ÷ 2 = 25 Hz).
private const val GRAPH_LEN = 150

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MainScreen(viewModel: MainViewModel = viewModel(factory = MainViewModel.Factory)) {
    val ui by viewModel.ui.collectAsStateWithLifecycle()
    val snackbarHost = remember { SnackbarHostState() }
    val scope = rememberCoroutineScope()

    LaunchedEffect(ui.toast) {
        ui.toast?.let {
            scope.launch { snackbarHost.showSnackbar(it) }
            viewModel.dismissToast()
        }
    }

    // Live graph buffers
    val eogVh = remember { GraphBuffer(GRAPH_LEN) }
    val eogVv = remember { GraphBuffer(GRAPH_LEN) }
    val accX = remember { GraphBuffer(GRAPH_LEN) }
    val accY = remember { GraphBuffer(GRAPH_LEN) }
    val accZ = remember { GraphBuffer(GRAPH_LEN) }
    val gyroX = remember { GraphBuffer(GRAPH_LEN) }
    val gyroY = remember { GraphBuffer(GRAPH_LEN) }
    val gyroZ = remember { GraphBuffer(GRAPH_LEN) }
    var bumper by remember { mutableStateOf(0) }
    var minimizedCharts by remember { mutableStateOf(setOf<String>()) }

    LaunchedEffect(Unit) {
        viewModel.graph.collect { ev ->
            when (ev) {
                GraphEvent.Reset -> {
                    eogVh.clear(); eogVv.clear()
                    accX.clear(); accY.clear(); accZ.clear()
                    gyroX.clear(); gyroY.clear(); gyroZ.clear()
                }
                is GraphEvent.Eog -> {
                    eogVh.add(ev.vh); eogVv.add(ev.vv)
                }
                is GraphEvent.Acc -> {
                    accX.add(ev.x1); accY.add(ev.y); accZ.add(ev.z)
                }
                is GraphEvent.Gyro -> {
                    gyroX.add(ev.x1); gyroY.add(ev.y); gyroZ.add(ev.z)
                }
            }
            bumper++
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHost) },
        modifier = Modifier.fillMaxSize(),
    ) { inner ->
        Column(
            modifier = Modifier
                .padding(inner)
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 12.dp, vertical = 8.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            if (!ui.isMeasuring) {
                ConnectCard(ui, viewModel)
            }
            if (ui.connection == ConnectionState.ServicesReady) {
                MeasureCard(ui, viewModel)
            }
            if (ui.isMeasuring) {
                bumper // ensure recomposition keys
                val charts = listOf(
                    ChartSpec(
                        key = "eog",
                        title = stringResource(R.string.eog_graph_title),
                        series = listOf(
                            LineSeries(EogBlue, eogVv.snapshotY(), "Vv"),
                            LineSeries(EogRed, eogVh.snapshotY(), "Vh"),
                        ),
                        yMin = -400f, yMax = 400f,
                    ),
                    ChartSpec(
                        key = "acc",
                        title = stringResource(R.string.acc_graph_title),
                        series = listOf(
                            LineSeries(AccBlue, accX.snapshotY(), "X"),
                            LineSeries(AccGreen, accY.snapshotY(), "Y"),
                            LineSeries(AccRed, accZ.snapshotY(), "Z"),
                        ),
                        yMin = -35000f, yMax = 35000f,
                    ),
                    ChartSpec(
                        key = "gyro",
                        title = stringResource(R.string.gyro_graph_title),
                        series = listOf(
                            LineSeries(GyroBlue, gyroX.snapshotY(), "X"),
                            LineSeries(GyroGreen, gyroY.snapshotY(), "Y"),
                            LineSeries(GyroRed, gyroZ.snapshotY(), "Z"),
                        ),
                        yMin = -35000f, yMax = 35000f,
                    ),
                )
                charts.forEach { spec ->
                    if (spec.key !in minimizedCharts) {
                        LiveLineChart(
                            title = spec.title,
                            series = spec.series,
                            yMin = spec.yMin,
                            yMax = spec.yMax,
                            onMinimize = { minimizedCharts = minimizedCharts + spec.key },
                        )
                    }
                }
                if (minimizedCharts.isNotEmpty()) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .horizontalScroll(rememberScrollState()),
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                    ) {
                        charts.filter { it.key in minimizedCharts }.forEach { spec ->
                            MinimizedChartChip(
                                title = spec.title,
                                onClick = { minimizedCharts = minimizedCharts - spec.key },
                            )
                        }
                    }
                }
            }
        }
    }

    if (ui.mockError != null) {
        AlertDialog(
            onDismissRequest = { viewModel.dismissMockError() },
            title = { Text(stringResource(R.string.mock_error_title)) },
            text = { Text(ui.mockError ?: "") },
            confirmButton = {
                TextButton(onClick = { viewModel.dismissMockError() }) {
                    Text(stringResource(R.string.button_dialog_ok))
                }
            },
        )
    }

    if (ui.bluetoothError) {
        AlertDialog(
            onDismissRequest = { viewModel.dismissBluetoothError() },
            title = { Text(stringResource(R.string.bluetooth_error_title)) },
            text = { Text(stringResource(R.string.bluetooth_error_text)) },
            confirmButton = {
                TextButton(onClick = { viewModel.dismissBluetoothError() }) {
                    Text(stringResource(R.string.button_dialog_ok))
                }
            },
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ConnectCard(ui: MainUiState, vm: MainViewModel) {
    var showSettings by remember { mutableStateOf(false) }
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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SettingsDialog(ui: MainUiState, vm: MainViewModel, onDismiss: () -> Unit) {
    val canEditSettings = !ui.isMeasuring
    val canInitialize = ui.connection == ConnectionState.ServicesReady &&
            !ui.isMeasuring && !ui.isInitializing

    // Enabling mock mode opens a file picker; the chosen CSV drives the mock engine.
    val mockCsvPicker = rememberLauncherForActivityResult(
        ActivityResultContracts.OpenDocument()
    ) { uri -> vm.onMockCsvSelected(uri) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(stringResource(R.string.text_label_settings)) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Button(
                        onClick = { vm.initialize() },
                        enabled = canInitialize,
                    ) { Text(stringResource(R.string.button_initialize)) }
                    if (ui.isInitializing) {
                        Box(Modifier.width(8.dp))
                        CircularProgressIndicator(
                            modifier = Modifier.size(20.dp),
                            strokeWidth = 2.dp,
                        )
                    }
                }

                EnumDropdown(
                    label = stringResource(R.string.text_label_set_mode),
                    options = MemeMode.entries.map { it.display },
                    selectedIndex = ui.settings.mode.ordinal,
                    enabled = canEditSettings,
                ) { i -> vm.updateSettings { it.copy(mode = MemeMode.fromIndex(i)) } }

                EnumDropdown(
                    label = stringResource(R.string.text_label_set_quality),
                    options = MemeQuality.entries.map { it.display },
                    selectedIndex = ui.settings.quality.ordinal,
                    enabled = canEditSettings,
                ) { i -> vm.updateSettings { it.copy(quality = MemeQuality.fromIndex(i)) } }

                EnumDropdown(
                    label = stringResource(R.string.text_label_set_acceleration),
                    options = AccRange.entries.map { it.display },
                    selectedIndex = ui.settings.accRange.ordinal,
                    enabled = canEditSettings,
                ) { i -> vm.updateSettings { it.copy(accRange = AccRange.fromIndex(i)) } }

                EnumDropdown(
                    label = stringResource(R.string.text_label_set_gyroacope),
                    options = GyroRange.entries.map { it.display },
                    selectedIndex = ui.settings.gyroRange.ordinal,
                    enabled = canEditSettings,
                ) { i -> vm.updateSettings { it.copy(gyroRange = GyroRange.fromIndex(i)) } }

                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { vm.setReconnectEnabled(!ui.reconnectEnabled) },
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Checkbox(
                        checked = ui.reconnectEnabled,
                        onCheckedChange = { vm.setReconnectEnabled(it) },
                    )
                    Text(
                        stringResource(R.string.text_label_reconnect),
                        modifier = Modifier.weight(1f),
                    )
                }

                if (BuildConfig.DEBUG) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically,
                    ) {
                        Text(
                            stringResource(R.string.text_label_mock_mode),
                            modifier = Modifier.weight(1f),
                        )
                        Switch(
                            checked = ui.mockEnabled,
                            onCheckedChange = { checked ->
                                if (checked) mockCsvPicker.launch(arrayOf("*/*"))
                                else vm.setMockEnabled(false)
                            },
                        )
                    }
                }
            }
        },
        confirmButton = {
            TextButton(onClick = onDismiss) {
                Text(stringResource(R.string.button_dialog_ok))
            }
        },
    )
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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun MeasureCard(ui: MainUiState, vm: MainViewModel) {
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
                    text = if (ui.isMeasuring) stringResource(R.string.button_measurement_stop)
                    else stringResource(R.string.button_measurement_start),
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
private fun HoldButton(
    text: String,
    hintText: String,
    enabled: Boolean,
    onConfirmed: () -> Unit,
    modifier: Modifier = Modifier,
    holdMs: Int = 700,
) {
    val progress = remember { Animatable(0f) }
    var holding by remember { mutableStateOf(false) }
    var hintTrigger by remember { mutableStateOf(0) }
    var showHint by remember { mutableStateOf(false) }

    LaunchedEffect(holding, enabled) {
        if (holding && enabled) {
            progress.snapTo(0f)
            try {
                progress.animateTo(
                    targetValue = 1f,
                    animationSpec = tween(durationMillis = holdMs, easing = LinearEasing),
                )
                onConfirmed()
            } catch (_: CancellationException) {
                // released early; the snap-back is handled in the else branch.
            }
        } else {
            progress.animateTo(0f, tween(durationMillis = 150))
        }
    }

    LaunchedEffect(hintTrigger) {
        if (hintTrigger > 0) {
            showHint = true
            delay(2000)
            showHint = false
        }
    }

    val overlayColor = Color.Black.copy(alpha = 0.25f)
    val positionProvider = remember {
        object : PopupPositionProvider {
            override fun calculatePosition(
                anchorBounds: IntRect,
                windowSize: IntSize,
                layoutDirection: LayoutDirection,
                popupContentSize: IntSize,
            ): IntOffset {
                val x = anchorBounds.left +
                        (anchorBounds.width - popupContentSize.width) / 2
                val y = anchorBounds.top - popupContentSize.height - 16
                return IntOffset(x, y)
            }
        }
    }

    Box(modifier = modifier) {
        Button(
            onClick = {},
            enabled = enabled,
            contentPadding = PaddingValues(0.dp),
            modifier = Modifier
                .fillMaxWidth()
                .height(40.dp)
                .pointerInput(enabled) {
                    if (!enabled) return@pointerInput
                    awaitEachGesture {
                        awaitFirstDown(requireUnconsumed = false)
                        val downTime = System.currentTimeMillis()
                        holding = true
                        try {
                            waitForUpOrCancellation()
                        } finally {
                            val shortTap = System.currentTimeMillis() - downTime < holdMs
                            // Reset even when pointerInput is cancelled mid-press
                            // (e.g. `enabled` flips false while isStarting is true)
                            // so that a stale `holding = true` doesn't immediately
                            // re-arm the timer once `enabled` returns to true.
                            holding = false
                            if (shortTap) hintTrigger++
                        }
                    }
                },
        ) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .drawBehind {
                        if (progress.value > 0f) {
                            drawRect(
                                color = overlayColor,
                                topLeft = Offset.Zero,
                                size = Size(size.width * progress.value, size.height),
                            )
                        }
                    },
                contentAlignment = Alignment.Center,
            ) {
                Text(text, modifier = Modifier.padding(horizontal = 16.dp))
            }
        }
        if (showHint) {
            Popup(
                popupPositionProvider = positionProvider,
                properties = PopupProperties(focusable = false),
            ) {
                Surface(
                    color = Color(0xFF323232),
                    shape = RoundedCornerShape(6.dp),
                    shadowElevation = 4.dp,
                ) {
                    Text(
                        text = hintText,
                        color = Color.White,
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp),
                    )
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun EnumDropdown(
    label: String,
    options: List<String>,
    selectedIndex: Int,
    enabled: Boolean,
    onSelected: (Int) -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }
    val current = options.getOrNull(selectedIndex) ?: ""
    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { if (enabled) expanded = it }) {
        OutlinedTextField(
            value = current,
            onValueChange = {},
            readOnly = true,
            enabled = enabled,
            label = { Text(label) },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
            modifier = Modifier
                .menuAnchor(MenuAnchorType.PrimaryNotEditable, enabled)
                .fillMaxWidth(),
        )
        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            options.forEachIndexed { i, label ->
                DropdownMenuItem(text = { Text(label) }, onClick = {
                    onSelected(i); expanded = false
                })
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

/** A minimized chart, rendered as a tappable title chip that restores the chart. */
private data class ChartSpec(
    val key: String,
    val title: String,
    val series: List<LineSeries>,
    val yMin: Float,
    val yMax: Float,
)

@Composable
private fun MinimizedChartChip(title: String, onClick: () -> Unit) {
    AssistChip(
        onClick = onClick,
        label = { Text(title) },
        leadingIcon = { Icon(Icons.Filled.BarChart, contentDescription = null) },
    )
}
