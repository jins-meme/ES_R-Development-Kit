//
//  ChartService.swift
//  MEME_Academic
//
//  リアルタイムチャート用データ計算サービス。
//  受信した AcademicData をバッファリングし、
//  ChartPlot (yMin/yMax, series, 軸ラベル) を更新する。
//

import Foundation
import SwiftUI

@MainActor
final class ChartService {

    private var chartDatas: [AcademicData] = []

    /// チャートに追加されるサンプルのレート（1秒あたりの件数）。
    /// Trans Speed に関わらず、Full/Standard 受信側の間引き処理により常に25Hz相当になる。
    nonisolated static let xLongScale: Int = 25

    /// 描画する点数の上限。X軸レンジが広くなっても LineMark の数を一定に保ち、
    /// 描画負荷（Swift Charts の再描画コスト）が増えないようにする。
    private static let maxRenderedPoints: Int = 200

    // MARK: - Buffer

    func append(_ data: AcademicData) {
        chartDatas.append(data)
    }

    func reset() {
        chartDatas.removeAll()
    }

    var count: Int { chartDatas.count }

    // MARK: - Plot update

    func updatePlots(chart1: inout ChartPlot,
                     chart1Category: Int,
                     chart1Eog: EogToggles,
                     chart1Gyro: GyroToggles,
                     chart1Accel: AccelToggles,
                     chart2: inout ChartPlot,
                     chart2Category: Int,
                     chart2Eog: EogToggles,
                     chart2Gyro: GyroToggles,
                     chart2Accel: AccelToggles,
                     chart3: inout ChartPlot,
                     chart3Category: Int,
                     chart3Eog: EogToggles,
                     chart3Gyro: GyroToggles,
                     chart3Accel: AccelToggles,
                     xRangeSeconds: Int) {
        let xLong = Self.xLongScale
        let xMax = xRangeSeconds * xLong
        // 表示ウィンドウが切り替わる直前（半秒分手前）からラベルをずらし始め、
        // 満杯になった瞬間に急にジャンプしないようにするための余白。
        let chartLimit = xMax - xLong / 2

        let sliced = slicedDatas(limit: chartLimit)
        // sliced 内でのローカルな位置ではなく、ストリーム全体での絶対位置を基準に間引く。
        // ウィンドウは1件ずつスライドするため、ローカル位置基準だと同じサンプルが
        // フレームごとに間引かれたり残ったりして描画がチカチカしてしまう。
        let globalOffset = chartDatas.count - sliced.count
        let stride = renderStride(for: sliced.count)
        let (xInit, xLabel) = computeXOffset(datasCount: chartDatas.count, xMax: xMax, xLong: xLong)

        updateChartPlot(&chart1,
                        category: chart1Category,
                        eog: chart1Eog, gyro: chart1Gyro, accel: chart1Accel,
                        sliced: sliced, stride: stride, globalOffset: globalOffset, xInitial: xInit, xLabel: xLabel, xMax: xMax)

        updateChartPlot(&chart2,
                        category: chart2Category,
                        eog: chart2Eog, gyro: chart2Gyro, accel: chart2Accel,
                        sliced: sliced, stride: stride, globalOffset: globalOffset, xInitial: xInit, xLabel: xLabel, xMax: xMax)

        updateChartPlot(&chart3,
                        category: chart3Category,
                        eog: chart3Eog, gyro: chart3Gyro, accel: chart3Accel,
                        sliced: sliced, stride: stride, globalOffset: globalOffset, xInitial: xInit, xLabel: xLabel, xMax: xMax)
    }

    // MARK: - Private helpers

    private func slicedDatas(limit: Int) -> [AcademicData] {
        let datas = chartDatas
        if datas.count >= limit {
            let startIndex = datas.count - limit
            return Array(datas[startIndex..<datas.count])
        } else {
            return datas
        }
    }

    /// sliced の何件おきに1点描画するか。maxRenderedPoints を超えないように間引く。
    private func renderStride(for sampleCount: Int) -> Int {
        guard sampleCount > Self.maxRenderedPoints else { return 1 }
        return Int(ceil(Double(sampleCount) / Double(Self.maxRenderedPoints)))
    }

    private func computeXOffset(datasCount: Int, xMax: Int, xLong: Int) -> (xInitial: Float, xLabel: Int) {
        if Float(datasCount) > Float(xMax - xLong) {
            let pos = datasCount % xLong
            let xInitial = Float(pos) * -1
            let xLabel = (datasCount / xLong) - (xMax / xLong) + 1
            return (xInitial, xLabel)
        }
        return (0, 0)
    }

    private func updateChartPlot(_ plot: inout ChartPlot,
                                 category: Int,
                                 eog: EogToggles,
                                 gyro: GyroToggles,
                                 accel: AccelToggles,
                                 sliced: [AcademicData],
                                 stride: Int,
                                 globalOffset: Int,
                                 xInitial: Float,
                                 xLabel: Int,
                                 xMax: Int) {
        plot.xInitial = xInitial
        plot.xLabelStart = xLabel
        plot.xMax = xMax

        switch category {
        case 0: // EOG
            plot.yMin = -1200; plot.yMax = 1200
            plot.series = buildEogSeries(sliced: sliced, toggles: eog, stride: stride, globalOffset: globalOffset)
        case 1: // Gyro (Full only)
            plot.yMin = -36000; plot.yMax = 36000
            plot.series = buildGyroSeries(sliced: sliced, toggles: gyro, stride: stride, globalOffset: globalOffset)
        case 2: // Accel
            plot.yMin = -36000; plot.yMax = 36000
            plot.series = buildAccelSeries(sliced: sliced, toggles: accel, stride: stride, globalOffset: globalOffset)
        default: break
        }
    }

    private func buildEogSeries(sliced: [AcademicData], toggles: EogToggles, stride: Int, globalOffset: Int) -> [ChartSeries] {
        var Lx: [Int] = []; var L: [Double] = []
        var Rx: [Int] = []; var R: [Double] = []
        var Hx: [Int] = []; var H: [Double] = []
        var Vx: [Int] = []; var V: [Double] = []
        for (i, d) in sliced.enumerated() where (globalOffset + i) % stride == 0 {
            if let s = d as? AcademicStandardData {
                if toggles.left { Lx.append(i); L.append(Double(s.eogL1)) }
                if toggles.right { Rx.append(i); R.append(Double(s.eogR1)) }
                if toggles.deltaH { Hx.append(i); H.append(Double(s.eogH1)) }
                if toggles.deltaV { Vx.append(i); V.append(Double(s.eogV1)) }
            } else if let f = d as? AcademicFullData {
                if toggles.left { Lx.append(i); L.append(Double(f.eogL)) }
                if toggles.right { Rx.append(i); R.append(Double(f.eogR)) }
                if toggles.deltaH { Hx.append(i); H.append(Double(f.eogH)) }
                if toggles.deltaV { Vx.append(i); V.append(Double(f.eogV)) }
            }
        }
        var out: [ChartSeries] = []
        if toggles.left { out.append(ChartSeries(name: "EOG L", color: .yellow, xs: Lx, values: L)) }
        if toggles.right { out.append(ChartSeries(name: "EOG R", color: .green, xs: Rx, values: R)) }
        if toggles.deltaH { out.append(ChartSeries(name: "ΔH", color: .red, xs: Hx, values: H)) }
        if toggles.deltaV { out.append(ChartSeries(name: "ΔV", color: .blue, xs: Vx, values: V)) }
        return out
    }

    private func buildGyroSeries(sliced: [AcademicData], toggles: GyroToggles, stride: Int, globalOffset: Int) -> [ChartSeries] {
        var Xx: [Int] = []; var X: [Double] = []
        var Yx: [Int] = []; var Y: [Double] = []
        var Zx: [Int] = []; var Z: [Double] = []
        for (i, d) in sliced.enumerated() where (globalOffset + i) % stride == 0 {
            guard let f = d as? AcademicFullData else { continue }
            if toggles.x { Xx.append(i); X.append(Double(f.gyroX)) }
            if toggles.y { Yx.append(i); Y.append(Double(f.gyroY)) }
            if toggles.z { Zx.append(i); Z.append(Double(f.gyroZ)) }
        }
        var out: [ChartSeries] = []
        if toggles.x { out.append(ChartSeries(name: "Gyro X", color: .red, xs: Xx, values: X)) }
        if toggles.y { out.append(ChartSeries(name: "Gyro Y", color: .green, xs: Yx, values: Y)) }
        if toggles.z { out.append(ChartSeries(name: "Gyro Z", color: .blue, xs: Zx, values: Z)) }
        return out
    }

    private func buildAccelSeries(sliced: [AcademicData], toggles: AccelToggles, stride: Int, globalOffset: Int) -> [ChartSeries] {
        var Xx: [Int] = []; var X: [Double] = []
        var Yx: [Int] = []; var Y: [Double] = []
        var Zx: [Int] = []; var Z: [Double] = []
        for (i, d) in sliced.enumerated() where (globalOffset + i) % stride == 0 {
            if let s = d as? AcademicStandardData {
                if toggles.x { Xx.append(i); X.append(Double(s.accX) + UserSetting.getXAxis()) }
                if toggles.y { Yx.append(i); Y.append(Double(s.accY) + UserSetting.getYAxis()) }
                if toggles.z { Zx.append(i); Z.append(Double(s.accZ) + UserSetting.getZAxis()) }
            } else if let f = d as? AcademicFullData {
                if toggles.x { Xx.append(i); X.append(Double(f.accX) + UserSetting.getXAxis()) }
                if toggles.y { Yx.append(i); Y.append(Double(f.accY) + UserSetting.getYAxis()) }
                if toggles.z { Zx.append(i); Z.append(Double(f.accZ) + UserSetting.getZAxis()) }
            }
        }
        var out: [ChartSeries] = []
        if toggles.x { out.append(ChartSeries(name: "Acc X", color: .red, xs: Xx, values: X)) }
        if toggles.y { out.append(ChartSeries(name: "Acc Y", color: .green, xs: Yx, values: Y)) }
        if toggles.z { out.append(ChartSeries(name: "Acc Z", color: .blue, xs: Zx, values: Z)) }
        return out
    }
}
