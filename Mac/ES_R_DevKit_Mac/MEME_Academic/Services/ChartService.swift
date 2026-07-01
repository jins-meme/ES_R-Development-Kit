//
//  ChartService.swift
//  MEME_Academic
//
//  リアルタイム／リプレイ チャート用データ計算サービス。
//  受信した AcademicData をフルレート（間引きなし）でバッファリングし、
//  表示ウィンドウ分の ChartPlot (yMin/yMax, series, 時間軸情報) を更新する。
//  間引きをしないのは、ハム（50/60Hz）成分を波形に残すため。
//  描画コストは Canvas 側（RealtimeChartView）が全点を1本の Path で描くことで賄う。
//

import Foundation
import SwiftUI

@MainActor
final class ChartService {

    /// 描画バッファ。フルレート（間引きなし）で直近サンプルのみ保持する。
    private var chartDatas: [AcademicData] = []

    /// chartDatas[0] のストリーム全体での絶対サンプル位置。
    /// リプレイのシーク時に reset(baseIndex:) で設定し、
    /// 時間軸ラベル（秒 = 絶対サンプル位置 / 周波数）を正しく算出するために使う。
    private var baseIndex: Int = 0

    /// 保持する最大サンプル数。最大ウィンドウ（30秒 × 100Hz）を賄えれば十分。
    /// これを超えたら古い方から破棄し baseIndex を進める（メモリ上限の確保）。
    private static let maxBufferSamples: Int = 30 * 100

    // MARK: - Buffer

    func append(_ data: AcademicData) {
        chartDatas.append(data)
        let overflow = chartDatas.count - Self.maxBufferSamples
        if overflow > 0 {
            chartDatas.removeFirst(overflow)
            baseIndex += overflow
        }
    }

    /// バッファをクリアする。baseIndex には次に append されるサンプルの絶対位置を渡す
    /// （リプレイのシーク先など）。通常のリセットは 0。
    func reset(baseIndex: Int = 0) {
        chartDatas.removeAll(keepingCapacity: true)
        self.baseIndex = baseIndex
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
                     sampleRate: Int,
                     xRangeSeconds: Int,
                     artifacts: [Int: String] = [:]) {
        let rate = max(sampleRate, 1)
        let windowSamples = max(1, xRangeSeconds * rate)
        // 直近 windowSamples 件のみ描画対象にする（波形は右詰めで、右端が最新サンプル）。
        let windowStart = max(0, chartDatas.count - windowSamples)
        let window = windowStart == 0 ? chartDatas : Array(chartDatas[windowStart...])
        // 最新サンプル（右端）の絶対位置。時間軸ラベルと右詰め描画の基準。
        let latestSampleIndex = baseIndex + max(chartDatas.count - 1, 0)
        // Artifact は全チャート共通（絶対サンプル位置と可視ウィンドウにのみ依存）。一度だけ抽出する。
        let visibleArtifacts = Self.artifactsInWindow(artifacts,
                                                      latest: latestSampleIndex,
                                                      windowSamples: windowSamples)

        updateChartPlot(&chart1, category: chart1Category,
                        eog: chart1Eog, gyro: chart1Gyro, accel: chart1Accel,
                        window: window, windowSamples: windowSamples,
                        latestSampleIndex: latestSampleIndex, sampleRate: rate,
                        artifacts: visibleArtifacts)

        updateChartPlot(&chart2, category: chart2Category,
                        eog: chart2Eog, gyro: chart2Gyro, accel: chart2Accel,
                        window: window, windowSamples: windowSamples,
                        latestSampleIndex: latestSampleIndex, sampleRate: rate,
                        artifacts: visibleArtifacts)

        updateChartPlot(&chart3, category: chart3Category,
                        eog: chart3Eog, gyro: chart3Gyro, accel: chart3Accel,
                        window: window, windowSamples: windowSamples,
                        latestSampleIndex: latestSampleIndex, sampleRate: rate,
                        artifacts: visibleArtifacts)
    }

    /// 可視ウィンドウ（右端 latest、幅 windowSamples サンプル）に入る Artifact だけ抽出する。
    private static func artifactsInWindow(_ artifacts: [Int: String],
                                          latest: Int,
                                          windowSamples: Int) -> [ChartArtifact] {
        guard !artifacts.isEmpty else { return [] }
        let leftAbs = latest - windowSamples
        var out: [ChartArtifact] = []
        for (index, text) in artifacts where index >= leftAbs && index <= latest {
            out.append(ChartArtifact(sampleIndex: index, text: text))
        }
        return out
    }

    // MARK: - Private helpers

    private func updateChartPlot(_ plot: inout ChartPlot,
                                 category: Int,
                                 eog: EogToggles,
                                 gyro: GyroToggles,
                                 accel: AccelToggles,
                                 window: [AcademicData],
                                 windowSamples: Int,
                                 latestSampleIndex: Int,
                                 sampleRate: Int,
                                 artifacts: [ChartArtifact]) {
        plot.windowSamples = windowSamples
        plot.latestSampleIndex = latestSampleIndex
        plot.sampleRate = sampleRate
        plot.artifacts = artifacts

        switch category {
        case 0: // EOG
            plot.yMin = -1200; plot.yMax = 1200
            plot.series = buildEogSeries(window: window, toggles: eog)
        case 1: // Gyro (Full only)
            plot.yMin = -36000; plot.yMax = 36000
            plot.series = buildGyroSeries(window: window, toggles: gyro)
        case 2: // Accel
            plot.yMin = -36000; plot.yMax = 36000
            plot.series = buildAccelSeries(window: window, toggles: accel)
        default: break
        }
    }

    private func buildEogSeries(window: [AcademicData], toggles: EogToggles) -> [ChartSeries] {
        var L: [Double] = []; var R: [Double] = []; var H: [Double] = []; var V: [Double] = []
        if toggles.left { L.reserveCapacity(window.count) }
        if toggles.right { R.reserveCapacity(window.count) }
        if toggles.deltaH { H.reserveCapacity(window.count) }
        if toggles.deltaV { V.reserveCapacity(window.count) }
        for d in window {
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

    private func buildGyroSeries(window: [AcademicData], toggles: GyroToggles) -> [ChartSeries] {
        var X: [Double] = []; var Y: [Double] = []; var Z: [Double] = []
        if toggles.x { X.reserveCapacity(window.count) }
        if toggles.y { Y.reserveCapacity(window.count) }
        if toggles.z { Z.reserveCapacity(window.count) }
        for d in window {
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

    private func buildAccelSeries(window: [AcademicData], toggles: AccelToggles) -> [ChartSeries] {
        var X: [Double] = []; var Y: [Double] = []; var Z: [Double] = []
        if toggles.x { X.reserveCapacity(window.count) }
        if toggles.y { Y.reserveCapacity(window.count) }
        if toggles.z { Z.reserveCapacity(window.count) }
        for d in window {
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
