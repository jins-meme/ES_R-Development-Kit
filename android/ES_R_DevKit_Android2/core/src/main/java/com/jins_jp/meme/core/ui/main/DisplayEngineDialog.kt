package com.jins_jp.meme.core.ui.main

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.DialogProperties
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.web.WebContentStore

/**
 * Display Engine ダイアログ（設定ボタンの横のアイコンから開く）。Mac の DisplayEngineView・Windows の DisplayEngineForm に当たる。
 * グラフ画面（WebView）の中身 zip の一覧を出し、使う 1 つを選ぶ・取り込む・消す。置き場と検査は [WebContentStore]。
 * 同梱の標準版（Standard）は規定で、消せない。計測中・再生中は切り替えさせない（Mac・Windows と同じ）。
 */
@Composable
internal fun DisplayEngineDialog(ui: MainUiState, vm: MainViewModel, onDismiss: () -> Unit) {
    val busy = ui.isMeasuring || ui.isReplaying
    val zipPicker = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) { uri -> vm.addGraphZip(uri) }
    var removing by remember { mutableStateOf<WebContentStore.Entry?>(null) }
    LaunchedEffect(Unit) { vm.openDisplayEngine() }

    AlertDialog(
        onDismissRequest = onDismiss,
        modifier = Modifier.fillMaxWidth(0.95f),
        properties = DialogProperties(usePlatformDefaultWidth = false),
        title = { Text(stringResource(R.string.text_label_display_engine)) },
        text = {
            Column(
                modifier = Modifier.verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                Text(
                    "The zip that draws the graph. You can keep several, but only one is active.",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Column(
                    Modifier
                        .fillMaxWidth()
                        .border(1.dp, MaterialTheme.colorScheme.outlineVariant, RoundedCornerShape(8.dp)),
                ) {
                    ui.graphEngines.forEachIndexed { k, entry ->
                        if (k > 0) HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                        EngineRow(
                            entry = entry,
                            active = entry.id == ui.graphActiveId,
                            enabled = !busy,
                            onActivate = { vm.activateGraph(entry.id) },
                            onDelete = { removing = entry },
                        )
                    }
                }
                // 取り込み・削除に失敗した理由(赤)か、取り込んだ結果の知らせ(同じファイルだった・置き換えた)
                ui.graphMessage?.let {
                    Text(
                        it,
                        style = MaterialTheme.typography.bodySmall,
                        color = if (ui.graphMessageIsError) MaterialTheme.colorScheme.error
                        else MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
                if (busy) {
                    Text(
                        "Stop the measurement or replay to change the display engine.",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
        },
        dismissButton = {
            OutlinedButton(
                onClick = { zipPicker.launch(arrayOf("application/zip", "application/x-zip-compressed", "application/octet-stream")) },
                enabled = !busy,
            ) { Text("Add zip…", maxLines = 1) }
        },
        confirmButton = { TextButton(onClick = onDismiss) { Text("Done") } },
    )

    removing?.let { entry ->
        AlertDialog(
            onDismissRequest = { removing = null },
            title = { Text("Delete ${entry.manifest.displayName}?") },
            text = { Text("The zip is removed from this app. To use it again, add the zip file again.") },
            confirmButton = {
                TextButton(onClick = { vm.removeGraph(entry.id); removing = null }) {
                    Text("Delete", color = MaterialTheme.colorScheme.error)
                }
            },
            dismissButton = { TextButton(onClick = { removing = null }) { Text("Cancel") } },
        )
    }
}

@Composable
private fun EngineRow(
    entry: WebContentStore.Entry,
    active: Boolean,
    enabled: Boolean,
    onActivate: () -> Unit,
    onDelete: () -> Unit,
) {
    Row(
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier
            .fillMaxWidth()
            .clickable(enabled = enabled && !active, onClick = onActivate)
            .padding(end = 8.dp),
    ) {
        RadioButton(selected = active, onClick = onActivate, enabled = enabled)
        Column(Modifier.weight(1f).padding(vertical = 8.dp)) {
            // 版に日時が入ると長いので、名前は 2 行まで折り返す(電話の幅では 1 行に収まらない)
            Text(entry.manifest.displayName, style = MaterialTheme.typography.bodyLarge,
                maxLines = 2, overflow = TextOverflow.Ellipsis)
            Text(
                if (entry.isBuiltIn) "Built-in (default)" else entry.manifest.name,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 1, overflow = TextOverflow.Ellipsis,
            )
        }
        if (active) {
            Text("Active", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(horizontal = 4.dp))
        }
        if (!entry.isBuiltIn) {
            TextButton(onClick = onDelete, enabled = enabled) { Text("Delete") }
        }
    }
}
