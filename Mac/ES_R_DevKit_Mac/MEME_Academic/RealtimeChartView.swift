//
//  RealtimeChartView.swift
//  MEME_Academic
//
//  Swift Charts を用いた波形描画ビュー。
//  既存 ChartView (NSView/draw) の代替。
//

import SwiftUI
import Charts

struct RealtimeChartView: View {

    let plot: ChartPlot

    private let xMax: Int = 200
    private let xLongScale: Int = 25

    var body: some View {
        Chart {
            ForEach(plot.series) { series in
                ForEach(Array(series.values.enumerated()), id: \.offset) { idx, value in
                    LineMark(
                        x: .value("Index", idx),
                        y: .value("Value", value)
                    )
                    .foregroundStyle(by: .value("Series", series.name))
                    .lineStyle(StrokeStyle(lineWidth: 1.0))
                }
            }
        }
        .chartXScale(domain: 0...Double(xMax))
        .chartYScale(domain: plot.yMin...plot.yMax)
        .chartXAxis {
            AxisMarks(values: xAxisValues) { value in
                AxisGridLine()
                AxisTick()
                AxisValueLabel {
                    if let intVal = value.as(Int.self) {
                        let label = plot.xLabelStart + (intVal / xLongScale)
                        Text("\(label)")
                            .font(.caption2)
                    }
                }
            }
        }
        .chartYAxis {
            AxisMarks(values: .automatic(desiredCount: 7)) { _ in
                AxisGridLine()
                AxisTick()
                AxisValueLabel()
            }
        }
        .chartForegroundStyleScale(seriesColorMapping)
        .chartLegend(.hidden)
        .padding(.vertical, 4)
        .background(Color.white)
    }

    /// x軸目盛の値（xLongScale 単位、xInitial分シフト）
    private var xAxisValues: [Int] {
        var values: [Int] = []
        let count = (xMax + xLongScale) / xLongScale
        for i in 0..<count {
            let v = Int(Float(xLongScale * i) + plot.xInitial)
            if v >= 0 && v <= xMax { values.append(v) }
        }
        return values
    }

    /// シリーズ名 → 色マッピング
    private var seriesColorMapping: KeyValuePairs<String, Color> {
        [
            "EOG L": .yellow,
            "EOG R": .green,
            "ΔH": .red,
            "ΔV": .blue,
            "Gyro X": .red,
            "Gyro Y": .green,
            "Gyro Z": .blue,
            "Acc X": .red,
            "Acc Y": .green,
            "Acc Z": .blue
        ]
    }
}
