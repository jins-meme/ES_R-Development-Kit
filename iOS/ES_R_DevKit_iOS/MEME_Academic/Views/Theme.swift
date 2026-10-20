//
//  Theme.swift
//  MEME_Academic (iOS)
//
//  ネイティブ側の色。グラフ画面(webview/common/ui.css のトークン)と同じ値を使い、ライト/ダークで切り替える。
//

import SwiftUI
import UIKit

enum Theme {
    /// 画面の地(--paper)
    static let paper = dynamic(light: 0xeef0f2, dark: 0x101214)
    /// カードの地(--surface)
    static let surface = dynamic(light: 0xffffff, dark: 0x181b1f)
    /// 枠線(--line)
    static let line = dynamic(light: 0xd9dde2, dark: 0x2b3036)
    /// 文字(--ink / --muted)
    static let ink = dynamic(light: 0x1d2126, dark: 0xe4e7eb)
    static let muted = dynamic(light: 0x5b6470, dark: 0x98a1ac)
    /// 強調色(--accent)。ボタンや見出しの色
    static let accent = dynamic(light: 0xc2127f, dark: 0xe0529f)
    /// 強調色の上の文字(--on-accent)
    static let onAccent = dynamic(light: 0xffffff, dark: 0x101214)

    /// 強調色のボタンの文字。押せないときは地の灰色に乗るので、灰色の文字にする
    static func onAccent(enabled: Bool) -> Color { enabled ? onAccent : Color(.tertiaryLabel) }

    private static func dynamic(light: UInt32, dark: UInt32) -> Color {
        Color(UIColor { $0.userInterfaceStyle == .dark ? uiColor(dark) : uiColor(light) })
    }

    private static func uiColor(_ rgb: UInt32) -> UIColor {
        UIColor(red: CGFloat((rgb >> 16) & 0xff) / 255,
                green: CGFloat((rgb >> 8) & 0xff) / 255,
                blue: CGFloat(rgb & 0xff) / 255,
                alpha: 1)
    }
}
