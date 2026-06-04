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
                .frame(width: 360)

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
    }
}

// MARK: - Left column

private struct LeftColumnView: View {

    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        @Bindable var vm = viewModel

        VStack(alignment: .leading, spacing: 10) {
            HStack {
                Button("Settings") { viewModel.openSettings() }
                    .disabled(viewModel.isInputDisabled)
                Spacer()
            }
            Text(viewModel.appVersionText).font(.caption).foregroundStyle(.secondary)
            Text(viewModel.memeVersionText).font(.caption).foregroundStyle(.secondary)

            Divider()

            Group {
                if viewModel.showStartScan {
                    Button("Start Scan") { viewModel.startScan() }
                }
                Picker("", selection: $vm.selectedDevice) {
                    if viewModel.foundDevices.isEmpty {
                        Text("(no device)").tag("")
                    } else {
                        ForEach(viewModel.foundDevices, id: \.self) { Text($0).tag($0) }
                    }
                }
                .labelsHidden()
                .disabled(viewModel.isInputDisabled)
                if viewModel.showConnect {
                    Button(viewModel.phase == .connected ? "Disconnect" : "Connect") {
                        viewModel.toggleConnect()
                    }
                }
                Text(viewModel.connectionStateText).foregroundStyle(.secondary)
            }

            Divider()

            Grid(alignment: .leading, horizontalSpacing: 10, verticalSpacing: 8) {
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
                if viewModel.showFreeMarking {
                    Button("Free Marking") { viewModel.toggleFreeMarking() }
                }
            }

            Divider()

            DataDisplayView()
                .environment(viewModel)

            Divider()

            StatsDisplayView()
                .environment(viewModel)
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

private struct DataDisplayView: View {
    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        Grid(alignment: .leading, horizontalSpacing: 12, verticalSpacing: 4) {
            GridRow {
                DataCell(label: "Cnt", value: "\(viewModel.displayCnt)")
                DataCell(label: "Battery", value: "\(viewModel.displayBattLv)")
            }
            GridRow {
                DataCell(label: "Acc X", value: "\(viewModel.displayAccX)")
                DataCell(label: "Gyro X", value: "\(viewModel.displayGyroX)")
            }
            GridRow {
                DataCell(label: "Acc Y", value: "\(viewModel.displayAccY)")
                DataCell(label: "Gyro Y", value: "\(viewModel.displayGyroY)")
            }
            GridRow {
                DataCell(label: "Acc Z", value: "\(viewModel.displayAccZ)")
                DataCell(label: "Gyro Z", value: "\(viewModel.displayGyroZ)")
            }
            GridRow {
                DataCell(label: "EOG L", value: "\(viewModel.displayEogL)")
                DataCell(label: "EOG R", value: "\(viewModel.displayEogR)")
            }
            GridRow {
                DataCell(label: "EOG H", value: "\(viewModel.displayEogH)")
                DataCell(label: "EOG V", value: "\(viewModel.displayEogV)")
            }
        }
        .font(.system(.body, design: .monospaced))
    }
}

private struct DataCell: View {
    let label: String
    let value: String

    var body: some View {
        HStack(spacing: 4) {
            Text(label).foregroundStyle(.secondary).frame(width: 60, alignment: .leading)
            Text(value).frame(width: 80, alignment: .trailing).monospacedDigit()
        }
    }
}

private struct StatsDisplayView: View {
    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
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

            HStack(alignment: .top, spacing: 8) {
                ChannelToggles(category: categoryBinding,
                               eog: $eog, gyro: $gyro, accel: $accel,
                               disabled: viewModel.isInputDisabled)
                    .frame(width: 160)
                RealtimeChartView(plot: plot)
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
            VStack(alignment: .leading, spacing: 2) {
                Toggle("Left", isOn: $eog.left).foregroundStyle(.yellow)
                Toggle("Right", isOn: $eog.right).foregroundStyle(.green)
                Toggle("ΔH", isOn: $eog.deltaH).foregroundStyle(.red)
                Toggle("ΔV", isOn: $eog.deltaV).foregroundStyle(.blue)
            }
            .toggleStyle(.checkbox)
            .disabled(disabled)
        case 1:
            VStack(alignment: .leading, spacing: 2) {
                Toggle("X Axis", isOn: $gyro.x).foregroundStyle(.red)
                Toggle("Y Axis", isOn: $gyro.y).foregroundStyle(.green)
                Toggle("Z Axis", isOn: $gyro.z).foregroundStyle(.blue)
            }
            .toggleStyle(.checkbox)
            .disabled(disabled)
        case 2:
            VStack(alignment: .leading, spacing: 2) {
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
