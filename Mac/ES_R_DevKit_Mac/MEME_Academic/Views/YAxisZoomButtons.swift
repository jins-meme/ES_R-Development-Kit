//
//  YAxisZoomButtons.swift
//  MEME_Academic
//
//  チャート縦軸の拡大／縮小ボタン。
//  アイコンは画像アセットではなく Path で生成し、ボタンの色・解像度に自動追従させる。
//

import SwiftUI

/// 縦軸スケール操作を表すアイコン。
/// `expanding == true`  : 中央から上下へ開く矢印（拡大＝ max-min を半分にする）
/// `expanding == false` : 上下から中央へ閉じる矢印（縮小＝ max-min を2倍にする）
struct YAxisZoomIcon: Shape {

    let expanding: Bool

    func path(in rect: CGRect) -> Path {
        var path = Path()
        // 上下対称に描くため、上半分を作って下半分は 180 度回転で複製する。
        path.addPath(halfArrow(in: rect))
        path.addPath(halfArrow(in: rect),
                     transform: CGAffineTransform(translationX: rect.midX, y: rect.midY)
                        .rotated(by: .pi)
                        .translatedBy(x: -rect.midX, y: -rect.midY))
        return path
    }

    /// 上半分の矢印（矢頭＋軸）。正規化座標（0...1）で組み立ててから rect へ写す。
    private func halfArrow(in rect: CGRect) -> Path {
        // 拡大は外向き（矢頭が上端）、縮小は内向き（矢頭が中央寄り）。
        let tipY: CGFloat = expanding ? 0.0 : 0.46
        let headBaseY: CGFloat = expanding ? 0.28 : 0.18
        let tailY: CGFloat = expanding ? 0.46 : 0.0
        let headHalfWidth: CGFloat = 0.34
        let shaftHalfWidth: CGFloat = 0.11

        var p = Path()
        p.move(to: CGPoint(x: 0.5, y: tipY))
        p.addLine(to: CGPoint(x: 0.5 + headHalfWidth, y: headBaseY))
        p.addLine(to: CGPoint(x: 0.5 + shaftHalfWidth, y: headBaseY))
        p.addLine(to: CGPoint(x: 0.5 + shaftHalfWidth, y: tailY))
        p.addLine(to: CGPoint(x: 0.5 - shaftHalfWidth, y: tailY))
        p.addLine(to: CGPoint(x: 0.5 - shaftHalfWidth, y: headBaseY))
        p.addLine(to: CGPoint(x: 0.5 - headHalfWidth, y: headBaseY))
        p.closeSubpath()

        return p.applying(CGAffineTransform(scaleX: rect.width, y: rect.height)
            .concatenating(CGAffineTransform(translationX: rect.minX, y: rect.minY)))
    }
}

/// チャート1枚分の縦軸拡大・縮小ボタン（拡大／縮小の2つ）。
struct YAxisZoomButtons: View {

    @Binding var plot: ChartPlot

    /// アイコンの表示サイズ。ボタン自体は不可視テキストで高さを決めているため、
    /// テキスト1行分（約16pt）を超えない範囲でアイコンだけを大きくしている。
    private static let iconWidth: CGFloat = 14
    private static let iconHeight: CGFloat = 15

    var body: some View {
        HStack(spacing: 4) {
            Button {
                plot.zoomInY()
            } label: {
                iconLabel(expanding: true)
            }
            .disabled(!plot.canZoomInY)
            .help("縦軸を拡大する（max-min を半分にする）")

            Button {
                plot.zoomOutY()
            } label: {
                iconLabel(expanding: false)
            }
            .disabled(!plot.canZoomOutY)
            .help("縦軸を縮小する（max-min を2倍にする）")
        }
    }

    /// アイコンだけだとボタンが標準コントロールより低くなるため、
    /// 不可視のテキストで高さを稼ぎ、隣の Picker と同じ高さに揃える。
    private func iconLabel(expanding: Bool) -> some View {
        ZStack {
            Text(verbatim: "A").hidden()
            YAxisZoomIcon(expanding: expanding)
                .frame(width: Self.iconWidth, height: Self.iconHeight)
        }
    }
}
