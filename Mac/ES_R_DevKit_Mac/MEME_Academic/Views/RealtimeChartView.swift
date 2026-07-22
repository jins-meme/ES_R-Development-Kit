//
//  RealtimeChartView.swift
//  MEME_Academic
//
//  Canvas を用いた波形描画ビュー。
//  ハム（50/60Hz）成分を残すため間引かずに全サンプルを1本の Path で描く。
//  Swift Charts のマーク差分計算コストを避け、高頻度・多点の再生描画でも軽量。
//  時間軸ラベルは「絶対サンプル位置 / サンプリング周波数」から秒で算出する。
//

import SwiftUI

struct RealtimeChartView: View {

    @Environment(\.colorScheme) private var colorScheme

    let plot: ChartPlot
    /// チャートがタップされたときに、対象データ行（絶対サンプル位置）を通知する。
    var onTapRow: ((Int) -> Void)? = nil
    /// ドラッグによる範囲選択が終了したときに、開始行・終了行（絶対サンプル位置、開始≦終了）を通知する。
    var onRangeSelected: ((Int, Int) -> Void)? = nil
    /// ドラッグによる範囲選択を受け付けるか（ファイル再生中のみ true にする）。
    var rangeSelectionEnabled: Bool = false

    /// ドラッグ中の選択範囲（開始X・現在X）。選択矩形の描画に使う。
    @State private var dragStartX: CGFloat? = nil
    @State private var dragCurrentX: CGFloat? = nil

    // プロット領域の余白（描画とタップ座標→行の変換で共有する）。
    private static let leftInset: CGFloat = 46
    private static let rightInset: CGFloat = 8
    private static let topInset: CGFloat = 6
    private static let bottomInset: CGFloat = 16

    var body: some View {
        GeometryReader { geo in
            Canvas(opaque: false, rendersAsynchronously: false) { context, size in
                draw(context: context, size: size)
            }
            .contentShape(Rectangle())
            .gesture(
                SpatialTapGesture()
                    .onEnded { value in
                        onTapRow?(rowForTap(x: value.location.x, width: geo.size.width))
                    }
            )
            .gesture(rangeSelectionGesture(width: geo.size.width))
        }
        .padding(.vertical, 4)
        .background(chartBackground)
    }

    /// 範囲選択ドラッグ。minimumDistance を設けることでタップ（Artifact 付与）と共存させる。
    /// 左端＝切り出し開始、右端＝終了。左右どちら向きのドラッグでも min/max で正規化する。
    private func rangeSelectionGesture(width: CGFloat) -> some Gesture {
        DragGesture(minimumDistance: 4)
            .onChanged { value in
                guard rangeSelectionEnabled else { return }
                dragStartX = value.startLocation.x
                dragCurrentX = value.location.x
            }
            .onEnded { value in
                defer {
                    dragStartX = nil
                    dragCurrentX = nil
                }
                guard rangeSelectionEnabled, let onRangeSelected else { return }
                let leftX = min(value.startLocation.x, value.location.x)
                let rightX = max(value.startLocation.x, value.location.x)
                onRangeSelected(rowForTap(x: leftX, width: width),
                                rowForTap(x: rightX, width: width))
            }
    }

    /// タップX座標を、右詰め描画に合わせて絶対サンプル行へ変換する。
    private func rowForTap(x: CGFloat, width: CGFloat) -> Int {
        let plotMinX = Self.leftInset
        let plotWidth = max(1, width - Self.leftInset - Self.rightInset)
        let clampedX = min(max(x, plotMinX), plotMinX + plotWidth)
        // 右端＝最新（fromRight=0）、左端＝最古（fromRight=1）。
        let fromRight = (plotMinX + plotWidth - clampedX) / plotWidth
        let windowSamples = max(plot.windowSamples, 1)
        let offset = Int((fromRight * CGFloat(windowSamples)).rounded())
        return max(0, plot.latestSampleIndex - offset)
    }

    // MARK: - Colors

    private var chartBackground: Color {
        colorScheme == .dark ? Color(white: 0.12) : .white
    }
    private var gridColor: Color {
        colorScheme == .dark ? Color(white: 0.30) : Color(white: 0.85)
    }
    private var borderColor: Color {
        colorScheme == .dark ? Color(white: 0.40) : Color(white: 0.70)
    }
    private var labelColor: Color {
        colorScheme == .dark ? Color(white: 0.70) : Color(white: 0.35)
    }
    /// Artifact の縦線・文字色。波形色（赤/緑/青/黄）と被らないアクセント色。
    private var artifactColor: Color { .orange }
    /// 範囲選択矩形の色。波形色・Artifact 色と被らない色。
    private var selectionColor: Color { .purple }

    // MARK: - Drawing

    private func draw(context: GraphicsContext, size: CGSize) {
        let plotRect = CGRect(x: Self.leftInset,
                              y: Self.topInset,
                              width: max(1, size.width - Self.leftInset - Self.rightInset),
                              height: max(1, size.height - Self.topInset - Self.bottomInset))

        let ySpan = max(plot.yMax - plot.yMin, 0.0001)
        func yFor(_ v: Double) -> CGFloat {
            plotRect.maxY - CGFloat((v - plot.yMin) / ySpan) * plotRect.height
        }

        let windowSamples = max(plot.windowSamples, 1)

        drawYAxis(context: context, plotRect: plotRect, ySpan: ySpan, yFor: yFor)
        drawXAxis(context: context, plotRect: plotRect, windowSamples: windowSamples)
        drawSeries(context: context, plotRect: plotRect, windowSamples: windowSamples, yFor: yFor)
        drawArtifacts(context: context, plotRect: plotRect, windowSamples: windowSamples)
        drawSelection(context: context, plotRect: plotRect)

        context.stroke(Path(plotRect), with: .color(borderColor), lineWidth: 0.5)
    }

    /// ドラッグ中の選択範囲を半透明の矩形で描く（範囲選択が有効なときのみ）。
    private func drawSelection(context: GraphicsContext, plotRect: CGRect) {
        guard rangeSelectionEnabled, let startX = dragStartX, let currentX = dragCurrentX else { return }
        let minX = max(plotRect.minX, min(startX, currentX))
        let maxX = min(plotRect.maxX, max(startX, currentX))
        guard maxX > minX else { return }
        let rect = CGRect(x: minX, y: plotRect.minY, width: maxX - minX, height: plotRect.height)
        context.fill(Path(rect), with: .color(selectionColor.opacity(0.15)))
        context.stroke(Path(rect), with: .color(selectionColor.opacity(0.6)), lineWidth: 1)
    }

    /// Artifact を縦線＋文字で描く。文字は Y軸上限あたり（プロット上端）に配置する。
    /// X位置は波形と同じ右詰めロジック（右端＝最新 latestSampleIndex）で求める。
    private func drawArtifacts(context: GraphicsContext,
                               plotRect: CGRect,
                               windowSamples: Int) {
        guard !plot.artifacts.isEmpty else { return }
        let latest = plot.latestSampleIndex
        for artifact in plot.artifacts {
            let x = plotRect.maxX - CGFloat(latest - artifact.sampleIndex) / CGFloat(windowSamples) * plotRect.width
            guard x >= plotRect.minX, x <= plotRect.maxX else { continue }

            var line = Path()
            line.move(to: CGPoint(x: x, y: plotRect.minY))
            line.addLine(to: CGPoint(x: x, y: plotRect.maxY))
            context.stroke(line, with: .color(artifactColor.opacity(0.5)), lineWidth: 0.5)

            let text = Text(artifact.text)
                .font(.system(size: 9, weight: .semibold))
                .foregroundColor(artifactColor)
            context.draw(text, at: CGPoint(x: x, y: plotRect.minY + 1), anchor: .top)
        }
    }

    /// 横グリッド＋Y軸ラベル（等間隔）。
    private func drawYAxis(context: GraphicsContext,
                           plotRect: CGRect,
                           ySpan: Double,
                           yFor: (Double) -> CGFloat) {
        // 8分割にすると ±2k/4k/8k/16k/32k・±1200 のいずれでもラベルがきりのよい値になる。
        let divisions = 8
        for i in 0...divisions {
            let v = plot.yMin + ySpan * Double(i) / Double(divisions)
            let y = yFor(v)
            var line = Path()
            line.move(to: CGPoint(x: plotRect.minX, y: y))
            line.addLine(to: CGPoint(x: plotRect.maxX, y: y))
            context.stroke(line, with: .color(gridColor), lineWidth: 0.5)

            let text = Text(yLabel(v)).font(.system(size: 9)).foregroundColor(labelColor)
            context.draw(text, at: CGPoint(x: plotRect.minX - 4, y: y), anchor: .trailing)
        }
    }

    /// 縦グリッド＋X軸（時間）ラベル。右端が最新サンプル（latestSampleIndex）。
    /// ラベルは「絶対サンプル位置 / 周波数」を hh:mm:ss（描画開始直後は負値）で表示する。
    private func drawXAxis(context: GraphicsContext,
                           plotRect: CGRect,
                           windowSamples: Int) {
        let rate = max(plot.sampleRate, 1)
        let latest = plot.latestSampleIndex
        let leftAbs = latest - windowSamples

        let totalSeconds = Double(windowSamples) / Double(rate)
        let stepSeconds = niceTimeStep(totalSeconds)
        let stepSamples = max(1, Int((stepSeconds * Double(rate)).rounded()))

        // leftAbs 以上で最小の stepSamples の倍数（負値にも対応する floor 除算）。
        var tick = Int(floor(Double(leftAbs) / Double(stepSamples))) * stepSamples
        if tick < leftAbs { tick += stepSamples }
        while tick <= latest {
            let x = plotRect.maxX - CGFloat(latest - tick) / CGFloat(windowSamples) * plotRect.width
            var line = Path()
            line.move(to: CGPoint(x: x, y: plotRect.minY))
            line.addLine(to: CGPoint(x: x, y: plotRect.maxY))
            context.stroke(line, with: .color(gridColor), lineWidth: 0.5)

            let seconds = Double(tick) / Double(rate)
            let text = Text(timeLabel(seconds)).font(.system(size: 9)).foregroundColor(labelColor)
            context.draw(text, at: CGPoint(x: x, y: plotRect.maxY + 3), anchor: .top)

            tick += stepSamples
        }
    }

    /// 各シリーズを1本の Path として描画（間引きなし・全点）。
    /// 最新サンプルを右端に固定し、古いサンプルほど左へ配置する（右詰め）。
    private func drawSeries(context: GraphicsContext,
                            plotRect: CGRect,
                            windowSamples: Int,
                            yFor: (Double) -> CGFloat) {
        var clipped = context
        clipped.clip(to: Path(plotRect))
        for series in plot.series where series.values.count > 1 {
            let count = series.values.count
            var path = Path()
            for (i, v) in series.values.enumerated() {
                // 末尾（最新）を右端に、i が小さい（古い）ほど左へ。
                let x = plotRect.maxX - CGFloat(count - 1 - i) / CGFloat(windowSamples) * plotRect.width
                let point = CGPoint(x: x, y: yFor(v))
                if i == 0 { path.move(to: point) } else { path.addLine(to: point) }
            }
            clipped.stroke(path, with: .color(series.color), lineWidth: 1.0)
        }
    }

    // MARK: - Label helpers

    /// ラベル本数が過剰にならない「きりのよい」秒刻みを選ぶ。
    private func niceTimeStep(_ totalSeconds: Double) -> Double {
        let candidates: [Double] = [1, 2, 5, 10, 15, 30, 60]
        let maxLabels = 8.0
        for c in candidates where totalSeconds / c <= maxLabels { return c }
        return candidates.last ?? 60
    }

    /// Y軸ラベル。1000以上は "k" 表記に丸めて、狭い左余白（leftInset）に収める。
    private func yLabel(_ v: Double) -> String {
        guard abs(v) >= 1000 else { return String(format: "%.0f", v) }
        let k = v / 1000
        return k == k.rounded() ? String(format: "%.0fk", k) : String(format: "%.1fk", k)
    }

    /// 秒を hh:mm:ss 形式にする。描画開始直後など負値なら先頭に "-" を付ける。
    private func timeLabel(_ seconds: Double) -> String {
        let total = Int(seconds.rounded())
        let sign = total < 0 ? "-" : ""
        let a = abs(total)
        return String(format: "%@%02d:%02d:%02d", sign, a / 3600, (a % 3600) / 60, a % 60)
    }
}
