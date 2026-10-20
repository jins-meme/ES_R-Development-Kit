//
//  MEME_AcademicApp.swift
//  MEME_Academic (iOS)
//
//  SwiftUI アプリのエントリポイント。ファイル App などから .csv / .csv.gz を「開く」と、再生する。
//

import SwiftUI

@main
struct MEME_AcademicApp: App {

    @State private var viewModel = MEMEViewModel()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environment(viewModel)
                .tint(Theme.accent)
                .onOpenURL { url in
                    guard url.isFileURL, CsvFile.isSupported(fileName: url.lastPathComponent) else { return }
                    viewModel.openReplay(url)
                }
                #if DEBUG
                .onAppear { DebugAutoTest.startIfRequested(viewModel) }
                #endif
        }
    }
}
