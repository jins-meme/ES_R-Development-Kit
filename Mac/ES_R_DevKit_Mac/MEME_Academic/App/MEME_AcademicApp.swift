//
//  MEME_AcademicApp.swift
//  MEME_Academic
//
//  SwiftUI アプリのエントリポイント。
//

import SwiftUI
import Combine
import AppKit

/// Finder の「このアプリで開く」やドロップで渡されたファイルURLを、
/// AppDelegate → SwiftUI ビュー間で受け渡すための共有オブジェクト。
/// @Published は購読開始時に現在値を配信するため、起動と同時にファイルを
/// 開かれてビュー生成前に URL が届いた場合も取りこぼさない。
@MainActor
final class FileOpenCoordinator: ObservableObject {
    static let shared = FileOpenCoordinator()
    @Published var pendingURL: URL?
    private init() {}
}

/// Finder からの「開く」イベント（application(_:open:)）を受け取る AppDelegate。
@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationDidFinishLaunching(_ notification: Notification) {
        // 判定器の通知(グラフ画面の notify)の許可は起動時に求める(本人の方針、2026-10-03)。拒否されても計測には関係ない
        DetectorNotifications.setUp()
    }

    func application(_ application: NSApplication, open urls: [URL]) {
        // 複数選択されても先頭の1件のみ File Replay として扱う。
        if let url = urls.first {
            FileOpenCoordinator.shared.pendingURL = url
        }
    }
}

@main
struct MEME_AcademicApp: App {

    @NSApplicationDelegateAdaptor(AppDelegate.self) private var appDelegate
    @State private var viewModel = MEMEViewModel()

    var body: some Scene {
        Window("JINS MEME Academic", id: "main") {
            ContentView()
                .environment(viewModel)
                .frame(minWidth: 1200, minHeight: 720)
                .onReceive(FileOpenCoordinator.shared.$pendingURL.compactMap { $0 }) { url in
                    FileOpenCoordinator.shared.pendingURL = nil
                    viewModel.openReplayFile(url: url)
                }
                #if DEBUG
                .onAppear { DebugAutoTest.startIfRequested(viewModel) }
                #endif
        }
        .windowResizability(.contentSize)
        .commands {
            // アプリのメニューの Settings… と、その下の Display Engine…(左の列のボタンと同じシートを開く)
            CommandGroup(replacing: .appSettings) {
                Button("Settings…") { viewModel.openSettings() }
                    .keyboardShortcut(",", modifiers: .command)
                    .disabled(viewModel.isInputDisabled)
                Button("Display Engine…") { viewModel.openDisplayEngine() }
                    .disabled(viewModel.isInputDisabled)
            }
        }
    }
}
