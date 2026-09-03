package com.jins_jp.meme.core.ui.main

import android.content.Intent
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.jins_jp.meme.core.R
import com.jins_jp.meme.core.ble.ConnectionState
import com.jins_jp.meme.core.data.AccRange
import com.jins_jp.meme.core.data.GyroRange
import com.jins_jp.meme.core.data.MemeMode
import com.jins_jp.meme.core.data.MemeQuality
import com.google.android.gms.oss.licenses.OssLicensesMenuActivity

@OptIn(ExperimentalMaterial3Api::class)
@Composable
internal fun SettingsDialog(ui: MainUiState, vm: MainViewModel, onDismiss: () -> Unit) {
    val canEditSettings = !ui.isMeasuring
    val canInitialize = ui.connection == ConnectionState.ServicesReady &&
            !ui.isMeasuring && !ui.isInitializing

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
                        .clickable { vm.setAutoConnect(!ui.autoConnect) },
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Checkbox(
                        checked = ui.autoConnect,
                        onCheckedChange = vm::setAutoConnect,
                    )
                    Text(
                        stringResource(R.string.text_label_auto_connect),
                        modifier = Modifier.weight(1f),
                    )
                }

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

                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { vm.setOpenSharingOnComplete(!ui.openSharingOnComplete) },
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Checkbox(
                        checked = ui.openSharingOnComplete,
                        onCheckedChange = { vm.setOpenSharingOnComplete(it) },
                    )
                    Text(
                        stringResource(R.string.text_label_open_sharing_on_complete),
                        modifier = Modifier.weight(1f),
                    )
                }

                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { vm.setLocationLogging(!ui.locationLogging) },
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Checkbox(
                        checked = ui.locationLogging,
                        onCheckedChange = { vm.setLocationLogging(it) },
                    )
                    Text(
                        stringResource(R.string.text_label_location_logging),
                        modifier = Modifier.weight(1f),
                    )
                }
            }
        },
        confirmButton = {
            // BuildConfig は core から参照できないため、PackageManager から表示名/バージョンを取得する。
            val context = LocalContext.current
            Column(modifier = Modifier.fillMaxWidth()) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    val appLabel = remember {
                        context.applicationInfo.loadLabel(context.packageManager).toString()
                    }
                    val versionText = remember {
                        val info = context.packageManager.getPackageInfo(context.packageName, 0)
                        "${info.versionName}.${info.longVersionCode}"
                    }
                    Text(
                        "$appLabel version $versionText",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.weight(1f),
                    )
                    TextButton(onClick = onDismiss) {
                        Text(stringResource(R.string.button_dialog_ok))
                    }
                }
                // バージョン情報の下に OSS Licenses ラベル。タップで
                // oss-licenses-plugin が生成したライセンス一覧を表示する。
                val ossTitle = stringResource(R.string.text_label_oss_licenses)
                Text(
                    ossTitle,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier
                        .clickable {
                            OssLicensesMenuActivity.setActivityTitle(ossTitle)
                            context.startActivity(
                                Intent(context, OssLicensesMenuActivity::class.java),
                            )
                        }
                        .padding(vertical = 8.dp),
                )
            }
        },
    )
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
