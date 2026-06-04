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

    /// 既存ロジックと同じ「直近 chartLimit 件」を取り出すための上限。
    /// xMax(200) - xLongScale(25)/2 = 187
    private let chartLimit: Int = 200 - 25 / 2

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
                     chart3Accel: AccelToggles) {
        let sliced = slicedDatas()
        let (xInit, xLabel) = computeXOffset(datasCount: chartDatas.count)

        updateChartPlot(&chart1,
                        category: chart1Category,
                        eog: chart1Eog, gyro: chart1Gyro, accel: chart1Accel,
                        sliced: sliced, xInitial: xInit, xLabel: xLabel)

        updateChartPlot(&chart2,
                        category: chart2Category,
                        eog: chart2Eog, gyro: chart2Gyro, accel: chart2Accel,
                        sliced: sliced, xInitial: xInit, xLabel: xLabel)

        updateChartPlot(&chart3,
                        category: chart3Category,
                        eog: chart3Eog, gyro: chart3Gyro, accel: chart3Accel,
                        sliced: sliced, xInitial: xInit, xLabel: xLabel)
    }

    // MARK: - Private helpers

    private func slicedDatas() -> [AcademicData] {
        let datas = chartDatas
        let limit = chartLimit
        if datas.count >= limit {
            let startIndex = datas.count - limit
            return Array(datas[startIndex..<datas.count])
        } else {
            return datas
        }
    }

    private func computeXOffset(datasCount: Int) -> (xInitial: Float, xLabel: Int) {
        let xMax: Float = 200
        let xLong: Int = 25
        if Float(datasCount) > xMax - Float(xLong) {
            let pos = datasCount % xLong
            let xInitial = Float(pos) * -1
            let xLabel = (datasCount / xLong) - (Int(xMax) / xLong) + 1
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
                                 xInitial: Float,
                                 xLabel: Int) {
        plot.xInitial = xInitial
        plot.xLabelStart = xLabel

        switch category {
        case 0: // EOG
            plot.yMin = -1200; plot.yMax = 1200
            plot.series = buildEogSeries(sliced: sliced, toggles: eog)
        case 1: // Gyro (Full only)
            plot.yMin = -36000; plot.yMax = 36000
            plot.series = buildGyroSeries(sliced: sliced, toggles: gyro)
        case 2: // Accel
            plot.yMin = -36000; plot.yMax = 36000
            plot.series = buildAccelSeries(sliced: sliced, toggles: accel)
        default: break
        }
    }

    private func buildEogSeries(sliced: [AcademicData], toggles: EogToggles) -> [ChartSeries] {
        var L: [Double] = []
        var R: [Double] = []
        var H: [Double] = []
        var V: [Double] = []
        for d in sliced {
            if let s = d as? AcademicStandardData {
                if toggles.left { L.append(Double(s.eogL1)) }
                if toggles.right { R.append(Double(s.eogR1)) }
                if toggles.deltaH { H.append(Double(s.eogH1)) }
                if toggles.deltaV { V.append(Double(s.eogV1)) }
            } else if let f = d as? AcademicFullData {
                if toggles.left { L.append(Double(f.eogL)) }
                if toggles.right { R.append(Double(f.eogR)) }
                if toggles.deltaH { H.append(Double(f.eogH)) }
                if toggles.deltaV { V.append(Double(f.eogV)) }
            }
        }
        var out: [ChartSeries] = []
        if toggles.left { out.append(ChartSeries(name: "EOG L", color: .yellow, values: L)) }
        if toggles.right { out.append(ChartSeries(name: "EOG R", color: .green, values: R)) }
        if toggles.deltaH { out.append(ChartSeries(name: "ΔH", color: .red, values: H)) }
        if toggles.deltaV { out.append(ChartSeries(name: "ΔV", color: .blue, values: V)) }
        return out
    }

    private func buildGyroSeries(sliced: [AcademicData], toggles: GyroToggles) -> [ChartSeries] {
        var X: [Double] = []; var Y: [Double] = []; var Z: [Double] = []
        for d in sliced {
            guard let f = d as? AcademicFullData else { continue }
            if toggles.x { X.append(Double(f.gyroX)) }
            if toggles.y { Y.append(Double(f.gyroY)) }
            if toggles.z { Z.append(Double(f.gyroZ)) }
        }
        var out: [ChartSeries] = []
        if toggles.x { out.append(ChartSeries(name: "Gyro X", color: .red, values: X)) }
        if toggles.y { out.append(ChartSeries(name: "Gyro Y", color: .green, values: Y)) }
        if toggles.z { out.append(ChartSeries(name: "Gyro Z", color: .blue, values: Z)) }
        return out
    }

    private func buildAccelSeries(sliced: [AcademicData], toggles: AccelToggles) -> [ChartSeries] {
        var X: [Double] = []; var Y: [Double] = []; var Z: [Double] = []
        for d in sliced {
            if let s = d as? AcademicStandardData {
                if toggles.x { X.append(Double(s.accX) + UserSetting.getXAxis()) }
                if toggles.y { Y.append(Double(s.accY) + UserSetting.getYAxis()) }
                if toggles.z { Z.append(Double(s.accZ) + UserSetting.getZAxis()) }
            } else if let f = d as? AcademicFullData {
                if toggles.x { X.append(Double(f.accX) + UserSetting.getXAxis()) }
                if toggles.y { Y.append(Double(f.accY) + UserSetting.getYAxis()) }
                if toggles.z { Z.append(Double(f.accZ) + UserSetting.getZAxis()) }
            }
        }
        var out: [ChartSeries] = []
        if toggles.x { out.append(ChartSeries(name: "Acc X", color: .red, values: X)) }
        if toggles.y { out.append(ChartSeries(name: "Acc Y", color: .green, values: Y)) }
        if toggles.z { out.append(ChartSeries(name: "Acc Z", color: .blue, values: Z)) }
        return out
    }
}
