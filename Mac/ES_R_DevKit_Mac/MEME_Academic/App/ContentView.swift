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

            VStack(spacing: 8) {
                ChartPanelView(index: 1,
                               title: vm.chart1Title,
                               categoryBinding: $vm.chart1Category,
                               eog: $vm.chart1EogToggles,
                               gyro: $vm.chart1GyroToggles,
                               accel: $vm.chart1AccelToggles,
                               plot: vm.chart1Plot)
                ChartPanelView(index: 2,
                               title: vm.chart2Title,
                               categoryBinding: $vm.chart2Category,
                               eog: $vm.chart2EogToggles,
                               gyro: $vm.chart2GyroToggles,
                               accel: $vm.chart2AccelToggles,
                               plot: vm.chart2Plot)
                ChartPanelView(index: 3,
                               title: vm.chart3Title,
                               categoryBinding: $vm.chart3Category,
                               eog: $vm.chart3EogToggles,
                               gyro: $vm.chart3GyroToggles,
                               accel: $vm.chart3AccelToggles,
                               plot: vm.chart3Plot)
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
        .padding(12)
        .sheet(isPresented: $vm.showingSettings) {
            SettingsView()
                .environment(viewModel)
        }
        .alert("Artifact", isPresented: $vm.showingArtifactDialog) {
            TextField("Artifact", text: $vm.artifactInput, prompt: Text("X"))
            Button("Cancel", role: .cancel) { viewModel.cancelArtifact() }
            Button("OK") { viewModel.confirmArtifact() }
        } message: {
            Text("Artifact will be added on 'Stop Replay' timing")
        }
        .sheet(isPresented: $vm.showingCutDialog) {
            CutFileDialogView()
                .environment(viewModel)
        }
    }
}

// MARK: - Replay range cut dialog

/// ドラッグ選択した区間をCSVへ切り出す際のファイル名入力ダイアログ。
/// `.alert` はボタン押下で必ず閉じてしまうため、エラー表示を保持できるシートで実装する。
private struct CutFileDialogView: View {

    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        @Bindable var vm = viewModel

        VStack(alignment: .leading, spacing: 12) {
            Text("Save selected range as CSV").font(.headline)
            TextField("File name", text: $vm.cutFileNameInput)
                .textFieldStyle(.roundedBorder)
                .frame(width: 300)
                .onSubmit { viewModel.confirmCutFile() }
            if !viewModel.cutErrorMessage.isEmpty {
                Text(viewModel.cutErrorMessage)
                    .font(.callout)
                    .foregroundStyle(.red)
            }
            HStack {
                Spacer()
                Button("Cancel", role: .cancel) { viewModel.cancelCutFile() }
                Button("OK") { viewModel.confirmCutFile() }
                    .keyboardShortcut(.defaultAction)
            }
        }
        .padding(20)
        .onChange(of: vm.cutFileNameInput) {
            // 入力し直したら前回のエラー表示を消す。
            viewModel.cutErrorMessage = ""
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
                    Button(viewModel.connectButtonLabel) {
                        viewModel.toggleConnect()
                    }
                    .disabled(viewModel.isConnecting)
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
                    Button(viewModel.replayButtonLabel) {
                        viewModel.toggleReplay()
                    }
                }
                if viewModel.showReplayPause {
                    Button(viewModel.replayPauseButtonLabel) {
                        viewModel.toggleReplayPause()
                    }
                }
                if viewModel.showXRangeControls {
                    Button("＋") { viewModel.zoomInXRange() }
                        .disabled(!viewModel.canZoomInXRange)
                        .help("より狭いX軸レンジに拡大する")
                    Button("－") { viewModel.zoomOutXRange() }
                        .disabled(!viewModel.canZoomOutXRange)
                        .help("より広いX軸レンジに縮小する")
                    Text("\(viewModel.xRangeSeconds)s").foregroundStyle(.secondary).monospacedDigit()
                }
            }

            if viewModel.showFreeMarking {
                HStack(spacing: 8) {
                    Button("Free Marking") { viewModel.toggleFreeMarking() }
                }
            }

            if viewModel.showReplayScrubber {
                ReplayScrubberView()
                    .environment(viewModel)
            }

            Divider()

            StatsDisplayView()
                .environment(viewModel)

            Spacer(minLength: 0)
        }
    }
}

private struct ReplayScrubberView: View {
    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        @Bindable var vm = viewModel

        VStack(alignment: .leading, spacing: 6) {
            Slider(value: $vm.replayProgress, in: 0...100) { editing in
                viewModel.replaySliderEditingChanged(editing)
            }
            HStack(spacing: 8) {
                Button("<<") { viewModel.replayJumpBackward() }
                Button(">>") { viewModel.replayJumpForward() }
                Button(viewModel.replaySpeedLabel) { viewModel.cycleReplaySpeed() }
                    .help("再生速度を切り替える（x1→x2→x4→x8→x16→x32→x1）")
            }
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

// MARK: - Chart panel

private struct ChartPanelView: View {

    @Environment(MEMEViewModel.self) private var viewModel

    let index: Int
    let title: String
    @Binding var categoryBinding: Int
    @Binding var eog: EogToggles
    @Binding var gyro: GyroToggles
    @Binding var accel: AccelToggles
    let plot: ChartPlot

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                Text(title).font(.headline)
                Spacer()
                Picker("", selection: $categoryBinding) {
                    ForEach(viewModel.chartCategoryOptions.indices, id: \.self) { i in
                        Text(viewModel.chartCategoryOptions[i]).tag(i)
                    }
                }
                .labelsHidden()
                .frame(width: 180)
                .disabled(viewModel.isInputDisabled)
                Button("Apply") { viewModel.applyChartSelection() }
                    .disabled(viewModel.isInputDisabled)
            }

            HStack(alignment: .center, spacing: 8) {
                ChannelToggles(category: categoryBinding,
                               eog: $eog, gyro: $gyro, accel: $accel,
                               disabled: viewModel.isInputDisabled)
                    .frame(width: 160)
                RealtimeChartView(plot: plot,
                                  onTapRow: { row in
                    viewModel.chartTapped(row: row)
                },
                                  onRangeSelected: { start, end in
                    viewModel.chartRangeSelected(startRow: start, endRow: end)
                },
                                  rangeSelectionEnabled: viewModel.isReplayRangeSelectable)
                .frame(maxWidth: .infinity, maxHeight: .infinity)
            }
        }
        .padding(8)
        .background(
            RoundedRectangle(cornerRadius: 6).fill(Color(NSColor.windowBackgroundColor))
        )
        .overlay(
            RoundedRectangle(cornerRadius: 6).stroke(Color.secondary.opacity(0.3), lineWidth: 1)
        )
    }
}

private struct ChannelToggles: View {
    let category: Int
    @Binding var eog: EogToggles
    @Binding var gyro: GyroToggles
    @Binding var accel: AccelToggles
    let disabled: Bool

    var body: some View {
        switch category {
        case 0:
            VStack(alignment: .leading, spacing: 10) {
                Toggle("Left", isOn: $eog.left).foregroundStyle(.yellow)
                Toggle("Right", isOn: $eog.right).foregroundStyle(.green)
                Toggle("ΔH", isOn: $eog.deltaH).foregroundStyle(.red)
                Toggle("ΔV", isOn: $eog.deltaV).foregroundStyle(.blue)
            }
            .toggleStyle(.checkbox)
            .disabled(disabled)
        case 1:
            VStack(alignment: .leading, spacing: 10) {
                Toggle("X Axis", isOn: $gyro.x).foregroundStyle(.red)
                Toggle("Y Axis", isOn: $gyro.y).foregroundStyle(.green)
                Toggle("Z Axis", isOn: $gyro.z).foregroundStyle(.blue)
            }
            .toggleStyle(.checkbox)
            .disabled(disabled)
        case 2:
            VStack(alignment: .leading, spacing: 10) {
                Toggle("X Axis", isOn: $accel.x).foregroundStyle(.red)
                Toggle("Y Axis", isOn: $accel.y).foregroundStyle(.green)
                Toggle("Z Axis", isOn: $accel.z).foregroundStyle(.blue)
            }
            .toggleStyle(.checkbox)
            .disabled(disabled)
        default:
            EmptyView()
        }
    }
}
