//
//  ContentView.swift
//  MEME_Academic (iOS)
//
//  メイン画面。並びは Android2 の MainScreen と同じ: 上に「Connect」のカード(計測中は隠す)と「Measure」のカード
//  (接続中だけ)、残りの高さをグラフ画面(WebView)が使う。
//

import SwiftUI
import UniformTypeIdentifiers

struct ContentView: View {

    @Environment(MEMEViewModel.self) private var viewModel
    @Environment(\.scenePhase) private var scenePhase

    var body: some View {
        @Bindable var vm = viewModel

        VStack(spacing: 8) {
            VStack(spacing: 12) {
                if !viewModel.isMeasuring {
                    ConnectCard()
                }
                if viewModel.isConnected {
                    MeasureCard()
                }
            }
            .padding(.horizontal, 12)
            .animation(.default, value: viewModel.phase)

            // グラフ画面は画面の端まで使う(余白はカードにだけ付ける。Android と同じ)
            WebChartView(bridge: viewModel.web)
                .ignoresSafeArea(edges: .bottom)
        }
        .padding(.top, 8)
        // 色はグラフ画面と揃える(Theme)
        .background(Theme.paper)
        .tint(Theme.accent)
        .overlay(alignment: .bottom) { ToastView(text: viewModel.toast) }
        .onChange(of: scenePhase) { _, phase in
            // アプリが裏に回っている間はグラフ画面へ送らない(戻ったら途切れを知らせる)。
            // inactive(許可のダイアログ・コントロールセンターなど)は画面に見えているので送り続ける
            viewModel.setForeground(phase != .background)
        }
        .sheet(isPresented: $vm.showingSettings) {
            SettingsView().environment(viewModel)
        }
        .sheet(isPresented: $vm.showingDisplayEngine) {
            DisplayEngineView().environment(viewModel)
        }
        .sheet(isPresented: Binding(get: { viewModel.shareItems != nil },
                                    set: { if !$0 { viewModel.shareItems = nil } })) {
            ShareSheet(items: viewModel.shareItems ?? [])
        }
        .fileImporter(isPresented: $vm.showingReplayPicker, allowedContentTypes: CsvFile.openPanelTypes) { result in
            if case .success(let url) = result { viewModel.openReplay(url) }
        }
        .alert("Do you want to enter shelf mode?", isPresented: $vm.showingShelfDialog) {
            Button("Yes") { viewModel.confirmShelfMode() }
            Button("Cancel", role: .cancel) {}
        } message: {
            Text("In shelf mode, all pairing capabilities are disabled and power consumption is reduced. To exit shelf mode, please recharge the device.")
        }
        .alert(viewModel.shelfResult?.title ?? "", isPresented: Binding(get: { viewModel.shelfResult != nil },
                                                                       set: { if !$0 { viewModel.shelfResult = nil } })) {
            Button("OK", role: .cancel) {}
        } message: {
            Text(viewModel.shelfResult?.message ?? "")
        }
        .alert("Bluetooth error", isPresented: $vm.showingBluetoothAlert) {
            Button("OK", role: .cancel) {}
        } message: {
            Text("Bluetooth is off. Please turn it on from the settings.")
        }
        // 落ち続ける取り込んだ zip をやめて標準版へ戻したとき(WebBridge.webViewWebContentProcessDidTerminate)
        .alert("Display Engine", isPresented: Binding(get: { viewModel.engineFallbackNotice != nil },
                                                      set: { if !$0 { viewModel.engineFallbackNotice = nil } })) {
            Button("OK", role: .cancel) {}
        } message: {
            Text(viewModel.engineFallbackNotice ?? "")
        }
    }
}

// MARK: - Connect

private struct ConnectCard: View {

    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        @Bindable var vm = viewModel

        Card {
            // 再生中は見出しの行(再生・設定のボタンごと)と端末の選択を出さず、Disconnect とファイル名だけにする(Android と同じ)
            if !viewModel.isReplaying {
                HStack(spacing: 4) {
                    Text("Connect to MEME device")
                        .font(.subheadline.weight(.bold))
                        .foregroundStyle(Theme.accent)
                    Spacer()
                    IconButton("play.fill", label: "Playback (replay logged CSV)") { viewModel.chooseReplayFile() }
                        .disabled(viewModel.phase != .idle)
                    IconButton("display", label: "Display Engine") { viewModel.showingDisplayEngine = true }
                    IconButton("gearshape.fill", label: "Settings") { viewModel.showingSettings = true }
                }

                HStack(spacing: 8) {
                    Button("Scan device") { viewModel.startScan() }
                        .buttonStyle(.borderedProminent)
                        .foregroundStyle(Theme.onAccent(enabled: viewModel.canScan))
                        .disabled(!viewModel.canScan)
                    Menu {
                        Picker("Device", selection: $vm.selectedDevice) {
                            ForEach(viewModel.foundDevices, id: \.self) { Text($0).tag($0) }
                        }
                    } label: {
                        HStack {
                            Text(viewModel.selectedDevice.isEmpty ? " " : viewModel.selectedDevice)
                                .lineLimit(1)
                                .foregroundStyle(Theme.ink)
                            Spacer()
                            Image(systemName: "chevron.down").font(.caption)
                        }
                        .padding(.horizontal, 12)
                        .frame(height: 32)
                        .overlay(Capsule().stroke(Theme.line))
                    }
                    .disabled(viewModel.foundDevices.isEmpty || viewModel.phase != .idle)
                    if viewModel.isScanning {
                        ProgressView()
                    }
                }
            }

            HStack(spacing: 12) {
                ConnectButton()
                Text(viewModel.statusText)
                    .font(.footnote)
                    .foregroundStyle(Theme.muted)
                    .lineLimit(2)
                Spacer(minLength: 0)
            }
        }
    }
}

/// Connect / Disconnect ボタン。タップは接続／切断。実機に接続済みで計測していないときだけ、5 秒の長押しで
/// Shelf mode の確認を開く。隠し操作なので、押している間にゲージなどは出さない。
private struct ConnectButton: View {

    @Environment(MEMEViewModel.self) private var viewModel
    /// 長押しが成立したジェスチャのタップを 1 回だけ捨てる(確認を出しながら切断しないため)
    @State private var suppressTap = false

    var body: some View {
        Button {
            if suppressTap { suppressTap = false } else { viewModel.connectOrDisconnect() }
        } label: {
            Text(viewModel.connectButtonLabel)
        }
        .buttonStyle(.borderedProminent)
        .foregroundStyle(Theme.onAccent(enabled: viewModel.canConnect))
        .disabled(!viewModel.canConnect)
        .simultaneousGesture(
            LongPressGesture(minimumDuration: MEMEViewModel.shelfLongPressSeconds)
                .onEnded { _ in
                    guard viewModel.canEnterShelfMode else { return }
                    suppressTap = true
                    viewModel.requestShelfMode()
                }
        )
        // 確認を閉じたら必ず倒す(ジェスチャが奪われてタップが来なかったとき、次の 1 タップを取りこぼさない)
        .onChange(of: viewModel.showingShelfDialog) { _, showing in
            if !showing { suppressTap = false }
        }
    }
}

// MARK: - Measure

private struct MeasureCard: View {

    @Environment(MEMEViewModel.self) private var viewModel

    var body: some View {
        Card {
            HStack(spacing: 8) {
                Text("Measure")
                    .font(.subheadline.weight(.bold))
                    .foregroundStyle(Theme.accent)
                Spacer()
                if viewModel.isMeasuring {
                    Text("Recording(\(viewModel.recordingRows))")
                        .font(.footnote)
                        .foregroundStyle(Theme.accent)
                        .monospacedDigit()
                }
                BatteryChip(level: viewModel.batteryLevel)
            }

            HStack(spacing: 8) {
                HoldButton(text: viewModel.isMeasuring ? "Stop Measurement" : "Start Measurement",
                           hint: viewModel.isMeasuring ? "Hold to Stop" : "Hold to Start",
                           enabled: !viewModel.isStarting) {
                    viewModel.toggleMeasurement()
                }
                Button {
                    viewModel.freeMarking()
                } label: {
                    Text("Free Marking")
                        .fontWeight(.semibold)
                        .frame(maxWidth: .infinity, minHeight: HoldButton.height - 14)
                }
                .buttonStyle(.borderedProminent)
                .buttonBorderShape(.roundedRectangle(radius: 10))
                .foregroundStyle(Theme.onAccent(enabled: viewModel.isMeasuring))
                .disabled(!viewModel.isMeasuring)
            }

            if viewModel.isStarting {
                HStack(spacing: 8) {
                    ProgressView()
                    Text("Starting…")
                }
            }

            if viewModel.isMeasuring {
                ProgressView(value: viewModel.successRate)
                HStack {
                    Text(String(format: "SUCCESS RATE: %.2f%%", viewModel.successRate * 100))
                        .frame(maxWidth: .infinity, alignment: .leading)
                    Text(String(format: "COMM RATE: %.2f%%", viewModel.commRate * 100))
                        .frame(maxWidth: .infinity, alignment: .leading)
                }
                .font(.caption2)
                .foregroundStyle(Theme.muted)
                .monospacedDigit()
            }
        }
    }
}

/// 電池残量(0〜5。0 は充電中。分からなければ ?)
private struct BatteryChip: View {
    let level: Int

    private var symbol: String {
        switch level {
        case 0: return "battery.0percent"
        case 1, 2: return "battery.25percent"
        case 3: return "battery.50percent"
        case 4: return "battery.75percent"
        case 5: return "battery.100percent"
        default: return "questionmark"
        }
    }

    var body: some View {
        HStack(spacing: 4) {
            Image(systemName: symbol)
            Text(level >= 0 ? "\(level)" : "?").monospacedDigit()
        }
        .font(.footnote)
        .foregroundStyle(Theme.ink)
        .padding(.horizontal, 8)
        .padding(.vertical, 3)
        .overlay(RoundedRectangle(cornerRadius: 7).stroke(Theme.line))
    }
}

// MARK: - Parts

private struct Card<Content: View>: View {
    @ViewBuilder let content: Content

    var body: some View {
        VStack(alignment: .leading, spacing: 8) { content }
            .font(.subheadline)
            .padding(10)
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(Theme.surface, in: RoundedRectangle(cornerRadius: 10))
            .overlay(RoundedRectangle(cornerRadius: 10).stroke(Theme.line))
    }
}

private struct IconButton: View {
    let symbol: String
    let label: String
    let action: () -> Void

    init(_ symbol: String, label: String, action: @escaping () -> Void) {
        self.symbol = symbol
        self.label = label
        self.action = action
    }

    var body: some View {
        Button(action: action) {
            Image(systemName: symbol)
                .font(.body)
                .frame(width: 36, height: 32)
        }
        .accessibilityLabel(label)
    }
}

private struct ToastView: View {
    let text: String?

    var body: some View {
        if let text {
            Text(text)
                .font(.subheadline)
                .foregroundStyle(.white)
                .padding(.horizontal, 16)
                .padding(.vertical, 10)
                .background(Color.black.opacity(0.8), in: Capsule())
                .padding(.bottom, 24)
                .transition(.opacity)
        }
    }
}

/// 共有シート(計測を止めたあと、データ CSV と判定器の表の CSV を渡す)
struct ShareSheet: UIViewControllerRepresentable {
    let items: [URL]

    func makeUIViewController(context: Context) -> UIActivityViewController {
        UIActivityViewController(activityItems: items, applicationActivities: nil)
    }

    func updateUIViewController(_ controller: UIActivityViewController, context: Context) {}
}
