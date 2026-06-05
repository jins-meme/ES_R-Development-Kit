//
//  MEMELibFactory.swift
//  MEME_Academic
//
//  Scheme の Launch Arguments に "-mock" を含むかどうかで、
//  実機実装 (MEMELib_Academic) かモック実装 (MockMEMELib_Academic) を切り替える。
//
//  この Xcode プロジェクトでは Swift 側で `DEBUG` が定義されていないため
//  `#if DEBUG` ガードは使わず、ランチ引数のみで判定する。
//

import Foundation

enum MEMELibFactory {

    /// `-mock` 引数で起動された場合に true。
    static var isMock: Bool {
        return ProcessInfo.processInfo.arguments.contains("-mock")
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
