//
//  SettingsView.swift
//  MEME_Academic
//
//  Setting シート (既存 SettingViewController の代替)。
//

import SwiftUI
import AppKit

struct SettingsView: View {

    @Environment(\.dismiss) private var dismiss
    @Environment(MEMEViewModel.self) private var viewModel

    @State private var saveFilePath: String = ""
    @State private var xAxis: String = "0"
    @State private var yAxis: String = "0"
    @State private var zAxis: String = "0"
    @State private var showSaveFileDialog: Bool = false
    @State private var extermalOutputSocket: Bool = false
    @State private var localPort: String = ""

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("Setting")
                .font(.title2).bold()

            Grid(alignment: .leading, horizontalSpacing: 12, verticalSpacing: 12) {
                GridRow {
                    Text("Save File Path")
                    HStack(spacing: 8) {
                        TextField("", text: $saveFilePath)
                            .textFieldStyle(.roundedBorder)
                            .frame(minWidth: 320)
                        Button("Browse") { browseFolder() }
                        Button("Open Folder") { openFolder() }
                    }
                }

                GridRow {
                    Text("Acc Offset X / Y / Z")
                    HStack(spacing: 8) {
                        axisField(text: $xAxis, label: "X")
                        axisField(text: $yAxis, label: "Y")
                        axisField(text: $zAxis, label: "Z")
                    }
                }

                GridRow {
                    Text("Save Dialog")
                    Toggle("Show save file dialog after measurement",
                           isOn: $showSaveFileDialog)
                        .toggleStyle(.checkbox)
                }

                GridRow {
                    Text("TCP Output")
                    Toggle("External output via TCP socket",
                           isOn: $extermalOutputSocket)
                        .toggleStyle(.checkbox)
                }

                GridRow {
                    Text("Local Port")
                    HStack(spacing: 12) {
                        TextField("", text: $localPort)
                            .textFieldStyle(.roundedBorder)
                            .frame(width: 100)
                        Text("Local IP: \(Common.getIPAddress())")
                            .foregroundStyle(.secondary)
                    }
                }
            }

            Divider()

            HStack {
                Spacer()
                Button("Cancel") { dismiss() }
                Button("Apply") { apply() }
                    .keyboardShortcut(.defaultAction)
            }
        }
        .padding(24)
        .frame(minWidth: 720, minHeight: 269)
        .onAppear { load() }
    }

    private func axisField(text: Binding<String>, label: String) -> some View {
        HStack(spacing: 4) {
            Text(label).foregroundStyle(.secondary)
            TextField("", text: text)
                .textFieldStyle(.roundedBorder)
                .frame(width: 80)
        }
    }

    // MARK: - Load / Save

    private func load() {
        saveFilePath = UserSetting.getSaveFilePath()
        xAxis = String(format: "%g", UserSetting.getXAxis())
        yAxis = String(format: "%g", UserSetting.getYAxis())
        zAxis = String(format: "%g", UserSetting.getZAxis())
        showSaveFileDialog = UserSetting.getShowSaveFileDialog()
        extermalOutputSocket = UserSetting.getExtermalOutputSocket()
        localPort = UserSetting.getLocalPort()
    }

    private func apply() {
        UserSetting.setSaveFilePath(saveFilePath)
        UserSetting.setXAxis(Double(xAxis) ?? 0)
        UserSetting.setYAxis(Double(yAxis) ?? 0)
        UserSetting.setZAxis(Double(zAxis) ?? 0)
        UserSetting.setShowSaveFileDialog(showSaveFileDialog)
        UserSetting.setExtermalOutputSocket(extermalOutputSocket)
        UserSetting.setLocalPort(localPort)
        viewModel.settingsDidApply()
        dismiss()
    }

    // MARK: - Folder pickers

    private func browseFolder() {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        if panel.runModal() == .OK, let url = panel.url {
            saveFilePath = url.path
        }
    }

    private func openFolder() {
        let url = URL(fileURLWithPath: saveFilePath)
        NSWorkspace.shared.open(url)
    }
}
