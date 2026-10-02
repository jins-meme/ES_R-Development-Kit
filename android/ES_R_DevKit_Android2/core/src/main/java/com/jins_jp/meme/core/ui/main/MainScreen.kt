package com.jins_jp.meme.core.ui.main

import android.content.ClipData
import android.content.Intent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarDuration
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
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
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.data.CSV_GZ_MIME
import com.jins_jp.meme.core.data.CSV_MIME
import com.jins_jp.meme.core.web.WebChartsPane
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MainScreen(
    viewModel: MainViewModel = viewModel(factory = MainViewModel.Factory),
    charts: @Composable ColumnScope.(MainUiState) -> Unit = { _ -> WebChartsPane(viewModel.web, Modifier.weight(1f)) },
) {
    val ui by viewModel.ui.collectAsStateWithLifecycle()
    val snackbarHost = remember { SnackbarHostState() }
    val scope = rememberCoroutineScope()
    val context = LocalContext.current

    // アプリが裏に回っている間はグラフ画面への push をやめる(戻ったら途切れを知らせる)
    val lifecycleOwner = LocalLifecycleOwner.current
    DisposableEffect(lifecycleOwner) {
        val obs = LifecycleEventObserver { _, e ->
            if (e == Lifecycle.Event.ON_START) viewModel.setForeground(true)
            if (e == Lifecycle.Event.ON_STOP) viewModel.setForeground(false)
        }
        lifecycleOwner.lifecycle.addObserver(obs)
        onDispose { lifecycleOwner.lifecycle.removeObserver(obs) }
    }

    // 計測完了時、設定が有効なら本体データCSVを「その他のアプリと共有」で開く。
    LaunchedEffect(ui.shareRequest) {
        val req = ui.shareRequest ?: return@LaunchedEffect
        viewModel.dismissShareRequest()
        // 本体データは設定により .csv.gz か .csv なので、受け手を絞りすぎないよう intent の type は "*/*"。
        val shareMimes = arrayOf(CSV_GZ_MIME, CSV_MIME)
        val shareIntent = Intent(Intent.ACTION_SEND_MULTIPLE).apply {
            type = "*/*"
            putParcelableArrayListExtra(Intent.EXTRA_STREAM, ArrayList(req.uris))
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            clipData = ClipData(null, shareMimes, ClipData.Item(req.uris.first())).apply {
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
        // グラフ画面(WebView)は残りの高さを取り、中で縦にスクロールする(外側はスクロールさせない)。
        // 余白はカードにだけ付け、グラフ画面は画面の端まで使う(ページ側も余白を詰めてある)
        Column(
            modifier = Modifier
                .padding(inner)
                .fillMaxSize()
                .padding(top = 8.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Column(
                modifier = Modifier.padding(horizontal = 12.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                if (!ui.isMeasuring) {
                    ConnectCard(ui, viewModel)
                }
                if (ui.connection == ConnectionState.ServicesReady) {
                    MeasureCard(ui, viewModel)
                }
            }
            charts(ui)
        }
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
