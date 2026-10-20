//
//  SettingsView.swift
//  MEME_Academic (iOS)
//
//  設定。項目は Android2 の SettingsDialog に合わせる: 端末の初期化、計測条件(モード・送信頻度・加速度/ジャイロのレンジ)、
//  自動接続・再接続・計測後の共有・gz 圧縮。iOS 版には位置の記録は無く、グラフの時刻の表示(Mac 版の設定)を足してある。
//  計測中・再生中は計測条件を変えられない。
//

import SwiftUI

struct SettingsView: View {

    @Environment(\.dismiss) private var dismiss
    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        @Bindable var vm = viewModel
        let locked = viewModel.isMeasuring || viewModel.isReplaying

        NavigationStack {
            Form {
                Section {
                    Picker("Select mode", selection: $vm.selectMode) {
                        ForEach(viewModel.selectModeOptions.indices, id: \.self) { Text(viewModel.selectModeOptions[$0]).tag($0) }
                    }
                    Picker("Transmission speed", selection: $vm.transSpeed) {
                        ForEach(viewModel.transSpeedOptions.indices, id: \.self) { Text(viewModel.transSpeedOptions[$0]).tag($0) }
                    }
                    Picker("Accelerometer range", selection: $vm.accelRange) {
                        ForEach(viewModel.accelRangeOptions.indices, id: \.self) { Text(viewModel.accelRangeOptions[$0]).tag($0) }
                    }
                    Picker("Gyroscope range", selection: $vm.gyroRange) {
                        ForEach(viewModel.gyroRangeOptions.indices, id: \.self) { Text(viewModel.gyroRangeOptions[$0]).tag($0) }
                    }
                    .disabled(MeasurementMode(pickerIndex: viewModel.selectMode) == .quaternion)
                } header: {
                    Text("Measurement")
                } footer: {
                    if locked {
                        Text("Stop the measurement or replay to change these.")
                    } else if MeasurementMode(pickerIndex: viewModel.selectMode) == .quaternion {
                        Text("Quaternion mode always uses 2000dps.")
                    }
                }
                .disabled(locked)

                Section {
                    Toggle("Auto-connect when discovered", isOn: $vm.autoConnect)
                    Toggle("Reconnect and restart when BLE disconnected", isOn: $vm.reconnectEnabled)
                    Toggle("Open sharing when measurement is complete", isOn: $vm.openSharingOnComplete)
                    Toggle("Compress saved data (.csv.gz)", isOn: $vm.compressSaveFile)
                    Toggle("Show graph time in local time", isOn: $vm.convertToLocalTime)
                } footer: {
                    Text("Data files are saved in the Files app (On My iPhone › ES_R DevKit). The DATE column is always UTC.")
                }

                Section {
                    Button {
                        viewModel.initializeDevice()
                    } label: {
                        HStack {
                            Text("Initialize")
                            if viewModel.isInitializing {
                                Spacer()
                                ProgressView()
                            }
                        }
                    }
                    .disabled(viewModel.phase != .connected || viewModel.isInitializing)
                } header: {
                    Text("Device")
                } footer: {
                    Text("Resets the parameters stored in the connected device.")
                }

                Section {
                    LabeledContent("App", value: viewModel.appVersionText)
                    if let fw = viewModel.firmwareVersion {
                        LabeledContent("Device firmware", value: fw)
                    }
                }
            }
            .navigationTitle("Settings")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) {
                    Button("OK") { dismiss() }
                }
            }
        }
    }
}
