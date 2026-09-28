//
//  ContentView.swift
//  MEME_Academic
//
//  メイン画面（旧 ViewController.swift + Main.storyboard の SwiftUI 版）。
//

import SwiftUI

struct ContentView: View {

    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        @Bindable var vm = viewModel

        HStack(alignment: .top, spacing: 12) {
            LeftColumnView()
                .environment(viewModel)
                .frame(width: 270)

            // グラフ画面(WebView)。表示幅・拡大縮小・畳む・再生の操作・アーティファクトの入力はこの中にある。
            WebChartView(bridge: viewModel.web)
                .frame(maxWidth: .infinity, maxHeight: .infinity)
                .clipShape(RoundedRectangle(cornerRadius: 6))
                .overlay(RoundedRectangle(cornerRadius: 6).stroke(Color.secondary.opacity(0.3), lineWidth: 1))
        }
        .padding(12)
        .sheet(isPresented: $vm.showingSettings) {
            SettingsView()
                .environment(viewModel)
        }
        .alert("Do you want to enter shelf mode?", isPresented: $vm.showingShelfDialog) {
            Button("Yes") { viewModel.confirmShelfMode() }
            Button("Cancel", role: .cancel) { viewModel.cancelShelfMode() }
        } message: {
            Text("In shelf mode, all pairing capabilities are disabled and power consumption is reduced. To exit shelf mode, please recharge the device.")
        }
    }
}

// MARK: - Left column

private struct LeftColumnView: View {

    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        @Bindable var vm = viewModel

        VStack(alignment: .leading, spacing: 18) {
            HStack {
                Button("Settings") { viewModel.openSettings() }
                    .disabled(viewModel.isInputDisabled)
                Spacer()
            }
            Text(viewModel.appVersionText).font(.caption).foregroundStyle(.secondary)
            Text(viewModel.memeVersionText).font(.caption).foregroundStyle(.secondary)

            Divider()

            Group {
                HStack(spacing: 8) {
                    if viewModel.showScanButton {
                        Button(viewModel.scanButtonLabel) { viewModel.toggleScan() }
                    }
                    if viewModel.showFileReplay {
                        Button("File Replay") { viewModel.chooseReplayFile() }
                            .disabled(viewModel.isScanning)
                    }
                }
                Picker("", selection: $vm.selectedDevice) {
                    if viewModel.foundDevices.isEmpty {
                        Text("(no device)").tag("")
                    } else {
                        ForEach(viewModel.foundDevices, id: \.self) { Text($0).tag($0) }
                    }
                }
                .labelsHidden()
                .disabled(viewModel.isDeviceSelectionDisabled)
                if viewModel.showConnect {
                    ConnectButton()
                        .environment(viewModel)
                }
                Text(viewModel.connectionStateText).foregroundStyle(.secondary)
            }

            Divider()

            Grid(alignment: .leading, horizontalSpacing: 12, verticalSpacing: 14) {
                GridRow {
                    LabeledPicker(title: "Select Mode",
                                  selection: $vm.selectMode,
                                  options: viewModel.selectModeOptions,
                                  disabled: viewModel.isInputDisabled)
                }
                GridRow {
                    LabeledPicker(title: "Trans Speed",
                                  selection: $vm.transSpeed,
                                  options: viewModel.transSpeedOptions,
                                  disabled: viewModel.isInputDisabled)
                }
                GridRow {
                    LabeledPicker(title: "Accel Range",
                                  selection: $vm.accelRange,
                                  options: viewModel.accelRangeOptions,
                                  disabled: viewModel.isInputDisabled)
                }
                GridRow {
                    LabeledPicker(title: "Gyro Range",
                                  selection: $vm.gyroRange,
                                  options: viewModel.gyroRangeOptions,
                                  disabled: viewModel.isInputDisabled)
                }
            }

            HStack(spacing: 8) {
                if viewModel.showMeasurement {
                    Button(viewModel.phase == .measuring ? "Stop Measurement" : "Start Measurement") {
                        viewModel.toggleMeasurement()
                    }
                }
                if viewModel.showReplayControls {
                    Button("Save Artifacts") {
                        viewModel.saveReplayArtifacts()
                    }
                    .help("グラフで付けた Artifact を再生中の CSV へ書き戻す（切断時にも書き戻す）")
                }
            }

            if viewModel.showFreeMarking {
                HStack(spacing: 8) {
                    Button("Free Marking") { viewModel.toggleFreeMarking() }
                }
            }

            Divider()

            StatsDisplayView()
                .environment(viewModel)

            Spacer(minLength: 0)
        }
    }
}

/// Connect / Disconnect ボタン。通常のクリックは接続／切断。
/// 実機に接続済みで非計測のときだけ、5秒の長押しで Shelf mode の確認ダイアログを開く。
/// Shelf mode は隠し操作なので、押している間にゲージなどは出さない。
private struct ConnectButton: View {

    @Environment(MEMEViewModel.self) private var viewModel

    /// 長押しが成立したジェスチャのクリックを1回だけ捨てるフラグ。
    /// macOS では長押しが成立した直後に Button の action も走るため、
    /// これが無いと確認ダイアログを出しながら切断してしまう。
    @State private var suppressClick = false

    var body: some View {
        Button(viewModel.connectButtonLabel) {
            if suppressClick {
                suppressClick = false
            } else {
                viewModel.toggleConnect()
            }
        }
        .disabled(viewModel.isConnecting || viewModel.isEnteringShelf)
        .simultaneousGesture(
            LongPressGesture(minimumDuration: MEMEViewModel.shelfLongPressSeconds)
                .onEnded { _ in
                    guard viewModel.canEnterShelfMode else { return }
                    suppressClick = true
                    viewModel.requestShelfMode()
                }
        )
        // ダイアログを閉じた時点で必ず倒す。ジェスチャが奪われてクリックが
        // 来なかった場合に、次の1タップを取りこぼさないため。
        .onChange(of: viewModel.showingShelfDialog) { _, showing in
            if !showing { suppressClick = false }
        }
    }
}

private struct LabeledPicker: View {
    let title: String
    @Binding var selection: Int
    let options: [String]
    let disabled: Bool

    var body: some View {
        HStack(spacing: 8) {
            Text(title)
                .frame(width: 90, alignment: .leading)
                .foregroundStyle(.secondary)
            Picker("", selection: $selection) {
                ForEach(options.indices, id: \.self) { i in
                    Text(options[i]).tag(i)
                }
            }
            .labelsHidden()
            .frame(minWidth: 120)
            .disabled(disabled)
        }
    }
}

private struct StatsDisplayView: View {
    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text("Success rate:").foregroundStyle(.secondary)
                Text(viewModel.successRateText).monospacedDigit()
            }
            ProgressView(value: min(max(viewModel.successRateValue, 0), 100), total: 100)
                .progressViewStyle(.linear)

            HStack {
                Text("Communication:").foregroundStyle(.secondary)
                Text(viewModel.communicationText).monospacedDigit()
            }
            ProgressView(value: min(max(viewModel.communicationValue, 0), 100), total: 100)
                .progressViewStyle(.linear)

            Text(viewModel.localAddressText).font(.caption).foregroundStyle(.secondary)
            Text(viewModel.localPortText).font(.caption).foregroundStyle(.secondary)
            Text(viewModel.socketStatusText).font(.caption).foregroundStyle(.secondary)
        }
    }
}
