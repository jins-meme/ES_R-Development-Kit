//
//  ChartView.swift
//  MEME_Academic
//
//  Created by Celleus on 2022/09/13.
//  Copyright © 2022 jins-jp. All rights reserved.
//

import Cocoa

class ChartView: NSView {

    var datas: [ChartData] = []

    var xMaxValue: Float = 10
    var xMinValue: Float = 0
    var yMaxValue: Float = 10
    var yMinValue: Float = 0

    var xInitialPosition: Float = 0
    var xLongScale: Int = 100
    var xShortScale: Int = 20

    var xTextFields: [NSTextField] = []
    var xTextFieldValue: Int = 0

    var yTextFields: [NSTextField] = []
    var yTextFieldValue: Int = 0

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
    }

    required init?(coder: NSCoder) {
        super.init(coder: coder)
    }

    override func draw(_ dirtyRect: NSRect) {
        super.draw(dirtyRect)

        NSColor.white.setFill()
        bounds.fill()

        let marginTop: CGFloat = 20
        let marginBottom: CGFloat = 0
        let marginLeft: CGFloat = 0
        let marginRight: CGFloat = 0

        let chanvasH = frame.size.height - marginTop - marginBottom
        let chanvasW = frame.size.width - marginLeft - marginRight

        let border = NSBezierPath()
        NSColor.black.set()
        border.lineWidth = 1.0
        border.move(to: NSPoint(x: marginLeft, y: marginTop))
        border.line(to: NSPoint(x: marginLeft + chanvasW, y: marginTop))
        border.line(to: NSPoint(x: marginLeft + chanvasW, y: marginTop + chanvasH))
        border.line(to: NSPoint(x: marginLeft, y: marginTop + chanvasH))
        border.line(to: NSPoint(x: marginLeft, y: marginTop))
        border.stroke()

        for textField in xTextFields {
            textField.removeFromSuperview()
        }
        xTextFields.removeAll()

        let xRange = xMaxValue - xMinValue
        guard xRange > 0, xShortScale > 0, xLongScale > 0 else { return }

        let count = Int((xMaxValue + Float(xLongScale)) / Float(xShortScale))
        for i in 0..<count {
            let wScal = chanvasW / CGFloat(xRange)
            let x = marginLeft + wScal * CGFloat(Float(xShortScale * i) + xInitialPosition)
            if marginLeft < x && x < marginLeft + chanvasW {
                var length: CGFloat = 3
                if (xShortScale * i) % xLongScale == 0 {
                    length = 6

                    let textField = NSTextField()
                    textField.frame = CGRect(x: x - 60/2, y: 0, width: 60, height: 20)
                    textField.isEditable = false
                    textField.isBordered = false
                    textField.drawsBackground = false
                    textField.backgroundColor = .clear
                    textField.stringValue = "\(xTextFieldValue + (xShortScale * i / xLongScale))"
                    textField.alignment = .center
                    textField.textColor = NSColor(named: "ChartText")
                    addSubview(textField)
                    xTextFields.append(textField)
                }

                let path1 = NSBezierPath()
                NSColor.black.set()
                path1.lineWidth = 1.0
                path1.move(to: NSPoint(x: x, y: marginTop))
                path1.line(to: NSPoint(x: x, y: marginTop + length))
                path1.stroke()

                let path2 = NSBezierPath()
                NSColor.black.set()
                path2.lineWidth = 1.0
                path2.move(to: NSPoint(x: x, y: marginTop + chanvasH))
                path2.line(to: NSPoint(x: x, y: marginTop + chanvasH - length))
                path2.stroke()
            }
        }

        let yRange = yMaxValue - yMinValue
        guard yRange > 0 else { return }

        for chartData in datas {
            if chartData.datas.count > 1 {
                let path = NSBezierPath()
                chartData.lineColor.set()
                path.lineWidth = 1.0

                let wScal = chanvasW / CGFloat(xRange)
                let hScal = chanvasH / CGFloat(yRange)

                let firstNumber = chartData.datas[0]
                path.move(to: NSPoint(x: marginLeft,
                                      y: marginTop + chanvasH + hScal * CGFloat(firstNumber.floatValue) + hScal * CGFloat(yMinValue)))

                for i in 1..<chartData.datas.count {
                    let number = chartData.datas[i]
                    path.line(to: NSPoint(x: marginLeft + wScal * CGFloat(i),
                                          y: marginTop + chanvasH + hScal * CGFloat(number.floatValue) + hScal * CGFloat(yMinValue)))
                }
                path.stroke()
            }
        }
    }

    func setChartData(_ datas: [NSNumber], lineColor: NSColor) {
        let data = ChartData()
        data.lineColor = lineColor
        data.datas = datas
        self.datas.append(data)
    }
}
