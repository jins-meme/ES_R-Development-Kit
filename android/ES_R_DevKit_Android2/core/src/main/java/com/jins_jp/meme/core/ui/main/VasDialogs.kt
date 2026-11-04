package com.jins_jp.meme.core.ui.main

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Slider
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.DialogProperties
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.data.VAS_VALUE_DEFAULT
import com.jins_jp.meme.core.data.VAS_VALUE_MAX
import com.jins_jp.meme.core.data.VAS_VALUE_MIN
import com.jins_jp.meme.core.data.VasItem
import com.jins_jp.meme.core.data.VasSettings
import com.jins_jp.meme.core.data.parseVasInterval
import java.text.DateFormat
import java.util.Date
import kotlin.math.roundToInt

// 項目のメモの長さの上限（画面に出すだけなので長すぎるものは切る）
private const val VAS_MEMO_MAX_LENGTH = 100

/**
 * 設定「Record VAS」のモーダル。間隔(分、全項目共通)と項目（メモはオプション）を編集する。
 * 間隔は正の整数 1〜1440 だけを受け、それ以外は OK を押せない。項目は Add item / 削除ボタンで増減する（最低 1 つ）。
 * OK で [onConfirm] に編集後の設定を渡す（enabled は [initial] のまま。チェックボックスから開いたときは呼び出し側が ON にする）。
 */
@Composable
internal fun VasSettingsDialog(
    initial: VasSettings,
    onConfirm: (VasSettings) -> Unit,
    onDismiss: () -> Unit,
) {
    var intervalText by remember { mutableStateOf(initial.intervalMinutes.toString()) }
    val memos = remember {
        mutableStateListOf<String>().apply { addAll(initial.items.map { it.memo }.ifEmpty { listOf("") }) }
    }
    val interval = parseVasInterval(intervalText)

    AlertDialog(
        onDismissRequest = onDismiss,
        modifier = Modifier.fillMaxWidth(0.95f),
        properties = DialogProperties(usePlatformDefaultWidth = false),
        title = { Text(stringResource(R.string.text_label_vas_settings)) },
        text = {
            Column(
                modifier = Modifier.verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                OutlinedTextField(
                    value = intervalText,
                    // 数字以外は打てないようにする（貼り付けも含む）。範囲外の数は残して赤くし、OK を押せなくする
                    onValueChange = { v -> if (v.length <= 4 && v.all { it in '0'..'9' }) intervalText = v },
                    label = { Text(stringResource(R.string.vas_interval_label)) },
                    singleLine = true,
                    isError = interval == null,
                    supportingText = if (interval == null) {
                        { Text(stringResource(R.string.vas_interval_error)) }
                    } else null,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.NumberPassword),
                    modifier = Modifier.fillMaxWidth(),
                )
                Text(
                    stringResource(R.string.vas_interval_note),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                HorizontalDivider()
                memos.forEachIndexed { i, memo ->
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        OutlinedTextField(
                            value = memo,
                            onValueChange = { memos[i] = it.replace(Regex("[\\r\\n]"), " ").take(VAS_MEMO_MAX_LENGTH) },
                            label = { Text(stringResource(R.string.vas_item_title, i + 1) + " – " + stringResource(R.string.vas_memo_label)) },
                            singleLine = true,
                            modifier = Modifier.weight(1f),
                        )
                        IconButton(
                            onClick = { memos.removeAt(i) },
                            enabled = memos.size > 1,
                        ) {
                            Icon(Icons.Filled.Delete, contentDescription = stringResource(R.string.vas_delete_item))
                        }
                    }
                }
                OutlinedButton(onClick = { memos.add("") }) {
                    Icon(Icons.Filled.Add, contentDescription = null)
                    Text(stringResource(R.string.button_vas_add_item))
                }
                Text(
                    stringResource(R.string.vas_memo_note),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = {
                    val minutes = interval ?: return@TextButton
                    onConfirm(initial.copy(intervalMinutes = minutes, items = memos.map { VasItem(it.trim()) }))
                },
                enabled = interval != null && memos.isNotEmpty(),
            ) { Text(stringResource(R.string.button_dialog_ok)) }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text(stringResource(R.string.button_dialog_cancel)) }
        },
    )
}

/**
 * 計測中の VAS の入力ポップアップ。項目ごとにスライダー（0〜100 の整数、初期値 50）で入力し、OK で閉じて記録する。
 * 外側のタップや戻るでは閉じない（記録の取りこぼしを防ぐ）。
 */
@Composable
internal fun VasPromptDialog(prompt: VasPrompt, onSubmit: (List<Int>) -> Unit) {
    val values = remember(prompt.id) {
        mutableStateListOf<Int>().apply { repeat(prompt.memos.size) { add(VAS_VALUE_DEFAULT) } }
    }
    val timeText = remember(prompt.id) { DateFormat.getTimeInstance(DateFormat.MEDIUM).format(Date(prompt.promptedAt)) }
    AlertDialog(
        onDismissRequest = {},
        modifier = Modifier.fillMaxWidth(0.95f),
        properties = DialogProperties(
            usePlatformDefaultWidth = false,
            dismissOnBackPress = false,
            dismissOnClickOutside = false,
        ),
        title = {
            Column {
                Text(stringResource(R.string.vas_prompt_title))
                Text(
                    stringResource(R.string.vas_prompt_time, timeText),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        },
        text = {
            Column(
                modifier = Modifier.verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                prompt.memos.forEachIndexed { i, memo ->
                    Column {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Text(
                                stringResource(R.string.vas_item_title, i + 1),
                                fontWeight = FontWeight.Bold,
                            )
                            if (memo.isNotEmpty()) {
                                Text(
                                    "  $memo",
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                    modifier = Modifier.weight(1f),
                                )
                            } else {
                                Spacer(Modifier.weight(1f))
                            }
                            Text(
                                values[i].toString(),
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold,
                                color = MaterialTheme.colorScheme.primary,
                            )
                        }
                        Slider(
                            value = values[i].toFloat(),
                            onValueChange = { values[i] = it.roundToInt().coerceIn(VAS_VALUE_MIN, VAS_VALUE_MAX) },
                            valueRange = VAS_VALUE_MIN.toFloat()..VAS_VALUE_MAX.toFloat(),
                            // 0〜100 の整数で止まる（間の止まり位置が 99 個）
                            steps = VAS_VALUE_MAX - VAS_VALUE_MIN - 1,
                        )
                        Row(modifier = Modifier.fillMaxWidth()) {
                            Text(
                                VAS_VALUE_MIN.toString(),
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.weight(1f),
                            )
                            Text(
                                VAS_VALUE_MAX.toString(),
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                            )
                        }
                    }
                }
            }
        },
        confirmButton = {
            TextButton(onClick = { onSubmit(values.toList()) }) {
                Text(stringResource(R.string.button_dialog_ok))
            }
        },
    )
}
