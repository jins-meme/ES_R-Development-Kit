//
//  ContentView.swift
//  MEME_Academic
//
//  メイン画面。
//  既存 Storyboard と同じ構成（スキャン/接続 → 設定 → データ表示）を踏襲する。
//

import SwiftUI

struct ContentView: View {

    @State private var viewModel = MEMEViewModel()

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            scanSection
            Divider()
            settingsSection
            Divider()
            dataSection
            Spacer(minLength: 0)
        }
        .padding(20)
        .frame(minWidth: 560, minHeight: 620)
    }

    // MARK: - Scan / Connect

    @ViewBuilder
    private var scanSection: some View {
        HStack(spacing: 12) {
            Button("Start Scan") { viewModel.startScan() }
                .opacity(showStartScan ? 1 : 0)
                .disabled(!showStartScan)

            Picker("", selection: $viewModel.selectedDevice) {
                if viewModel.foundDevices.isEmpty {
                    Text("").tag("")
                }
                ForEach(viewModel.foundDevices, id: \.self) { name in
                    Text(name).tag(name)
                }
            }
            .labelsHidden()
            .frame(minWidth: 220)

            Button(viewModel.phase == .connected ? "Disconnect" : "Connect") {
                viewModel.toggleConnect()
            }
            .opacity(showConnect ? 1 : 0)
            .disabled(!showConnect)
        }

        Text(viewModel.connectionStateText)
            .foregroundStyle(.secondary)
    }

    // MARK: - Settings

    @ViewBuilder
    private var settingsSection: some View {
        Grid(alignment: .leading, horizontalSpacing: 16, verticalSpacing: 10) {
            GridRow {
                labeledPicker("Select Mode",
                              selection: $viewModel.selectMode,
                              options: viewModel.selectModeOptions)
                labeledPicker("Trans Speed",
                              selection: $viewModel.transSpeed,
                              options: viewModel.transSpeedOptions)
            }
            GridRow {
                labeledPicker("Accel Range",
                              selection: $viewModel.accelRange,
                              options: viewModel.accelRangeOptions)
                labeledPicker("Gyro Range",
                              selection: $viewModel.gyroRange,
                              options: viewModel.gyroRangeOptions)
            }
        }

        Button(viewModel.phase == .measuring ? "Stop Measurement" : "Start Measurement") {
            viewModel.toggleMeasurement()
        }
        .opacity(showMeasurement ? 1 : 0)
        .disabled(!showMeasurement)
        .padding(.top, 4)
    }

    private func labeledPicker(_ title: String,
                               selection: Binding<Int>,
                               options: [String]) -> some View {
        HStack(spacing: 8) {
            Text(title)
                .frame(width: 100, alignment: .leading)
            Picker("", selection: selection) {
                ForEach(options.indices, id: \.self) { i in
                    Text(options[i]).tag(i)
                }
            }
            .labelsHidden()
            .frame(minWidth: 130)
        }
    }

    // MARK: - Data

    @ViewBuilder
    private var dataSection: some View {
        let d = viewModel.latestData
        Grid(alignment: .leading, horizontalSpacing: 20, verticalSpacing: 6) {
            GridRow {
                dataCell("Cnt", value: d.cnt)
                dataCell("Battery Lv", value: d.battLv)
            }
            Divider().gridCellColumns(4)
            GridRow {
                dataCell("Acc X", value: d.accX)
                dataCell("Gyro X", value: d.gyroX)
            }
            GridRow {
                dataCell("Acc Y", value: d.accY)
                dataCell("Gyro Y", value: d.gyroY)
            }
            GridRow {
                dataCell("Acc Z", value: d.accZ)
                dataCell("Gyro Z", value: d.gyroZ)
            }
            Divider().gridCellColumns(4)
            GridRow {
                dataCell("EOG L", value: d.eogL)
                dataCell("EOG R", value: d.eogR)
            }
            GridRow {
                dataCell("EOG H", value: d.eogH)
                dataCell("EOG V", value: d.eogV)
            }
        }
    }

    @ViewBuilder
    private func dataCell<V: BinaryInteger>(_ label: String, value: V) -> some View {
        Text(label).foregroundStyle(.secondary)
        Text(verbatim: "\(value)")
            .monospacedDigit()
            .frame(minWidth: 80, alignment: .trailing)
    }

    // MARK: - Phase derived flags

    private var showStartScan: Bool {
        viewModel.phase == .idle || viewModel.phase == .deviceFound
    }
    private var showConnect: Bool {
        viewModel.phase == .deviceFound || viewModel.phase == .connected
    }
    private var showMeasurement: Bool {
        viewModel.phase == .connected || viewModel.phase == .measuring
    }
}

#Preview {
    ContentView()
}
