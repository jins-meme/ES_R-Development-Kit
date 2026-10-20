//
//  MEMELibFactory.swift
//  MEME_Academic (iOS)
//
//  起動引数に "-mock" があればモック実装 (MockMEMELib_Academic)、無ければ実機実装 (MEMELib_Academic) を使う(Mac 版と同じ)。
//  Xcode では「MEME_Academic Mock」スキームで起動すると -mock が付く。
//

import Foundation

enum MEMELibFactory {

    /// `-mock` 引数で起動された場合に true
    static var isMock: Bool {
        ProcessInfo.processInfo.arguments.contains("-mock")
    }

    @MainActor
    static func make() -> any MEMELibInterface {
        if isMock {
            NSLog("[MEMELibFactory] -mock detected; using MockMEMELib_Academic")
            return MockMEMELib_Academic()
        }
        return MEMELib_Academic()
    }
}
