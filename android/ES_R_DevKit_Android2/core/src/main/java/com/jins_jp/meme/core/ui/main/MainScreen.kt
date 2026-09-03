package com.jins_jp.meme.core.ui.main

import android.content.ClipData
import android.content.Intent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarDuration
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.ble.ConnectionState
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MainScreen(
    viewModel: MainViewModel = viewModel(factory = MainViewModel.Factory),
    charts: @Composable ColumnScope.(MainUiState) -> Unit = { ui -> SensorChartsPane(viewModel, ui) },
) {
    val ui by viewModel.ui.collectAsStateWithLifecycle()
    val snackbarHost = remember { SnackbarHostState() }
    val scope = rememberCoroutineScope()
    val context = LocalContext.current

    // 計測完了時、設定が有効なら本体データCSV・分類CSVを「その他のアプリと共有」で開く。
    LaunchedEffect(ui.shareRequest) {
        val req = ui.shareRequest ?: return@LaunchedEffect
        viewModel.dismissShareRequest()
        val shareIntent = Intent(Intent.ACTION_SEND_MULTIPLE).apply {
            type = "text/csv"
            putParcelableArrayListExtra(Intent.EXTRA_STREAM, ArrayList(req.uris))
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            clipData = ClipData(null, arrayOf("text/csv"), ClipData.Item(req.uris.first())).apply {
                for (u in req.uris.drop(1)) addItem(ClipData.Item(u))
            }
        }
        context.startActivity(Intent.createChooser(shareIntent, null))
    }

    // Toast(Snackbar) は後勝ちで即時表示する。短時間に連続でタップしても、
    // 直前の表示をキャンセルして最新のメッセージにすぐ差し替える（順番待ちで遅延しない）。
    var toastJob by remember { mutableStateOf<Job?>(null) }
    LaunchedEffect(ui.toast) {
        val msg = ui.toast ?: return@LaunchedEffect
        viewModel.dismissToast()
        toastJob?.cancel()
        toastJob = scope.launch {
            snackbarHost.currentSnackbarData?.dismiss()
            snackbarHost.showSnackbar(msg, duration = SnackbarDuration.Short)
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
            charts(ui)
        }
    }

    // チャートタップのラベル入力。OK で入力文字列（空なら "X"）を記録し、
    // Stop Measurement / Stop Replay 時にデータCSVの ARTIFACT 列へ統合される。
    ui.labelDialog?.let { prompt ->
        var text by remember(prompt) { mutableStateOf("") }
        AlertDialog(
            onDismissRequest = { viewModel.dismissLabelDialog() },
            title = { Text(stringResource(R.string.label_dialog_title, prompt.num)) },
            text = {
                OutlinedTextField(
                    value = text,
                    onValueChange = { text = it },
                    singleLine = true,
                    label = { Text(stringResource(R.string.label_dialog_hint)) },
                )
            },
            confirmButton = {
                TextButton(onClick = { viewModel.confirmLabel(text) }) {
                    Text(stringResource(R.string.button_dialog_ok))
                }
            },
            dismissButton = {
                TextButton(onClick = { viewModel.dismissLabelDialog() }) {
                    Text(stringResource(R.string.button_dialog_cancel))
                }
            },
        )
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

    // Disconnect 長押しで開く Shelf mode の確認。Yes で CONFIG → SHELF を送り、
    // 端末は自ら切断する（復帰は充電のみ）。
    if (ui.showShelfDialog) {
        AlertDialog(
            onDismissRequest = { viewModel.dismissShelfDialog() },
            title = { Text(stringResource(R.string.shelf_dialog_title)) },
            text = { Text(stringResource(R.string.shelf_dialog_text)) },
            confirmButton = {
                TextButton(onClick = { viewModel.confirmShelfMode() }) {
                    Text(stringResource(R.string.button_dialog_yes))
                }
            },
            dismissButton = {
                TextButton(onClick = { viewModel.dismissShelfDialog() }) {
                    Text(stringResource(R.string.button_dialog_cancel))
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
