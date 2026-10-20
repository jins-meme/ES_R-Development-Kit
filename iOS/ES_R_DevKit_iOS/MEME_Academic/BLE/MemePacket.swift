//
//  MemePacket.swift
//  MEME_Academic (iOS)
//
//  ES_R の 20byte パケット: 暗号化/復号(Mac 版 DecEnc.swift と同じ鍵と変換)と、データパケットの解析
//  (Mac 版 MEMELib_Academic.dataAnalysis と同じ並び)。
//

import Foundation

enum MemePacket {

    private static let key: [UInt8] = [
        0x39, 0xCC, 0x6D, 0xAB, 0x9E, 0x07, 0x1A, 0xDE, 0x67,
        0x49, 0x71, 0x9A, 0x5B, 0x69, 0x0F, 0x17, 0xC9, 0xB1
    ]

    /// 先頭 2byte(長さ・種別)を除く 18byte を変換する
    static func encode(_ b: inout [UInt8]) {
        for i in 0..<key.count { b[2 + i] = (b[2 + i] ^ key[i]) &+ UInt8(i) }
    }

    static func decode(_ b: inout [UInt8]) {
        for i in 0..<key.count { b[2 + i] = (b[2 + i] &- UInt8(i)) ^ key[i] }
    }

    /// 端末へ書く 20byte のコマンド(暗号化済み)。params は [バイト位置: 値]
    static func command(_ op: UInt8, _ params: [Int: UInt8] = [:]) -> Data {
        var b = [UInt8](repeating: 0, count: PACKET_LENGTH)
        b[0] = UInt8(PACKET_LENGTH)
        b[1] = op
        for (i, v) in params { b[i] = v }
        encode(&b)
        return Data(b)
    }

    /// 通知 1 回分を 20byte ずつに分けて復号する。先頭が長さ 20 でないところで止める
    static func split(_ data: Data) -> [[UInt8]] {
        let all = [UInt8](data)
        var out: [[UInt8]] = []
        var offset = 0
        while offset + PACKET_LENGTH <= all.count, all[offset] == UInt8(PACKET_LENGTH) {
            var b = Array(all[offset..<offset + PACKET_LENGTH])
            decode(&b)
            out.append(b)
            offset += PACKET_LENGTH
        }
        return out
    }

    private static func i16(_ b: [UInt8], _ i: Int) -> Int16 {
        Int16(bitPattern: UInt16(b[i + 1]) << 8 | UInt16(b[i]))
    }

    private static func i32(_ b: [UInt8], _ i: Int) -> Int64 {
        Int64(Int32(bitPattern: UInt32(b[i + 3]) << 24 | UInt32(b[i + 2]) << 16 | UInt32(b[i + 1]) << 8 | UInt32(b[i])))
    }

    /// 復号済みのデータパケット(0x98 Standard / 0x99 Full / 0x9A Quaternion)を解析する。データでなければ nil
    static func parse(_ b: [UInt8]) -> AcademicData? {
        guard b.count >= PACKET_LENGTH else { return nil }
        let data: AcademicData
        switch b[1] {
        case 0x98:
            let d = AcademicStandardData()
            d.accX = i16(b, 4); d.accY = i16(b, 6); d.accZ = i16(b, 8)
            d.eogL1 = i16(b, 10); d.eogR1 = i16(b, 12); d.eogL2 = i16(b, 14); d.eogR2 = i16(b, 16)
            d.eogH1 = d.eogL1 &- d.eogR1
            d.eogH2 = d.eogL2 &- d.eogR2
            d.eogV1 = Int16(truncatingIfNeeded: 0 - (Int32(d.eogL1) + Int32(d.eogR1)) / 2)
            d.eogV2 = Int16(truncatingIfNeeded: 0 - (Int32(d.eogL2) + Int32(d.eogR2)) / 2)
            data = d
        case 0x99:
            let d = AcademicFullData()
            d.accX = i16(b, 4); d.accY = i16(b, 6); d.accZ = i16(b, 8)
            d.gyroX = i16(b, 10); d.gyroY = i16(b, 12); d.gyroZ = i16(b, 14)
            d.eogL = i16(b, 16); d.eogR = i16(b, 18)
            d.eogH = d.eogL &- d.eogR
            d.eogV = Int16(truncatingIfNeeded: 0 - (Int32(d.eogL) + Int32(d.eogR)) / 2)
            data = d
        case 0x9A:
            let d = AcademicQuaternionData()
            d.quaternionW = i32(b, 4); d.quaternionX = i32(b, 8); d.quaternionY = i32(b, 12); d.quaternionZ = i32(b, 16)
            data = d
        default:
            return nil
        }
        // 12bit のカウンタと電池残量(0〜5)
        data.cnt = UInt32(b[2]) | (UInt32(b[3]) << 8) & 0x0F00
        data.battLv = UInt16(b[3] >> 4)
        return data
    }
}
