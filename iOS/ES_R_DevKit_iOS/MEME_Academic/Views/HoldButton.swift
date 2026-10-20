//
//  HoldButton.swift
//  MEME_Academic (iOS)
//
//  押し続けると確定するボタン(Start / Stop Measurement)。Android2 の HoldButton と同じく、0.7 秒押し続けると
//  左から色が満ちて確定する。途中で離したら取り消し、短いタップには「Hold to …」の案内を出す(誤って計測を止めないため)。
//

import SwiftUI

struct HoldButton: View {
    let text: String
    let hint: String
    let enabled: Bool
    var holdSeconds: Double = 0.7

    /// ボタンの高さ(隣の Free Marking もこれに揃える)
    static let height: CGFloat = 40
    let onConfirmed: () -> Void

    @State private var progress: CGFloat = 0
    /// 押し始めた時刻(離したときに、確定まで押したかを見る)
    @State private var pressedAt: Date?
    @State private var showHint = false
    @State private var hintTask: Task<Void, Never>?

    var body: some View {
        ZStack {
            GeometryReader { geo in
                ZStack(alignment: .leading) {
                    Theme.accent.opacity(enabled ? 1 : 0.3)
                    Color.black.opacity(0.3)
                        .frame(width: geo.size.width * progress)
                }
            }
            Text(showHint ? hint : text)
                .font(.subheadline.weight(.semibold))
                .foregroundStyle(Theme.onAccent)
                .lineLimit(1)
                .minimumScaleFactor(0.7)
                .padding(.horizontal, 8)
        }
        .frame(maxWidth: .infinity)
        .frame(height: Self.height)
        .clipShape(RoundedRectangle(cornerRadius: 10))
        .contentShape(RoundedRectangle(cornerRadius: 10))
        .onLongPressGesture(minimumDuration: holdSeconds, maximumDistance: 40) {
            guard enabled else { return }
            pressedAt = nil
            progress = 0
            onConfirmed()
        } onPressingChanged: { pressing in
            guard enabled else { return }
            if pressing {
                pressedAt = Date()
                withAnimation(.linear(duration: holdSeconds)) { progress = 1 }
                return
            }
            withAnimation(.easeOut(duration: 0.15)) { progress = 0 }
            // 確定する前に離した: 押し続けるよう案内する
            if let at = pressedAt, Date().timeIntervalSince(at) < holdSeconds { flashHint() }
            pressedAt = nil
        }
        .accessibilityAddTraits(.isButton)
        .accessibilityLabel(text)
        .accessibilityAction { if enabled { onConfirmed() } }
    }

    private func flashHint() {
        showHint = true
        hintTask?.cancel()
        hintTask = Task { @MainActor in
            try? await Task.sleep(for: .seconds(1.2))
            guard !Task.isCancelled else { return }
            showHint = false
        }
    }
}
