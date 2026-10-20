//
//  MEME_AcademicApp.swift
//  MEME_Academic (iOS)
//
//  iOS で ES_R をどのレートまで受けられるかを測る検証用アプリ。
//

import SwiftUI

@main
struct MEME_AcademicApp: App {
    @State private var central: MemeCentral
    @State private var autoTest: AutoTest?

    init() {
        let central = MemeCentral()
        _central = State(initialValue: central)
        if let plan = AutoTest.plan() {
            let test = AutoTest(central: central, plan: plan)
            test.run()
            _autoTest = State(initialValue: test)
        }
    }

    var body: some Scene {
        WindowGroup {
            ContentView(central: central)
        }
    }
}
