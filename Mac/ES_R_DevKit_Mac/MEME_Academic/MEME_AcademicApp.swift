//
//  MEME_AcademicApp.swift
//  MEME_Academic
//
//  SwiftUI アプリのエントリポイント。
//

import SwiftUI

@main
struct MEME_AcademicApp: App {

    @State private var viewModel = MEMEViewModel()

    var body: some Scene {
        Window("JINS MEME Academic", id: "main") {
            ContentView()
                .environment(viewModel)
                .frame(minWidth: 1200, minHeight: 720)
        }
        .windowResizability(.contentSize)
    }
}
