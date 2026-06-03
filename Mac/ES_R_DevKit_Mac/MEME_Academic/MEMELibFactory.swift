//
//  MEMELibFactory.swift
//  MEME_Academic
//
//  Scheme の Launch Arguments に "-mock" を含むかどうかで、
//  実機実装 (MEMELib_Academic) かモック実装 (MockMEMELib_Academic) を切り替える。
//

import Foundation

enum MEMELibFactory {

    /// `-mock` 引数で起動された場合に true。
    /// ViewController 側で「CoreBluetooth 経由ではなくモックを直接駆動する」分岐に用いる。
    static var isMock: Bool {
        #if DEBUG
        return ProcessInfo.processInfo.arguments.contains("-mock")
        #else
        return false
        #endif
    }

    @MainActor
    static func make() -> any MEMELibInterface {
        #if DEBUG
        if isMock {
            NSLog("[MEMELibFactory] -mock detected; using MockMEMELib_Academic")
            return MockMEMELib_Academic()
        }
        #endif
        return MEMELib_Academic()
    }
}
