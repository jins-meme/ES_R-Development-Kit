//
//  AcademicData.swift
//  MEME_Academic
//
//  Created by Celleus on 2022/09/12.
//  Copyright © 2022 jins-jp. All rights reserved.
//

import Foundation

class AcademicData: NSObject {
    var cnt: UInt32 = 0
    var battLv: UInt16 = 0
    /// このサンプルの記録時刻（UTC基準の絶対時刻）。
    /// 計測中は受信時刻、ファイル再生中は再生元CSVの DATE 列（UTC）から復元する。
    /// CSV／ソケットへの出力と、チャートX軸のタイムスタンプ表示に使う。
    /// 時刻が分からない場合（DATE列が壊れているCSVなど）は nil。
    var date: Date?
}
