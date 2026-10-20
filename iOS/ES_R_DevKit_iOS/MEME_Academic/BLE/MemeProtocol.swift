//
//  MemeProtocol.swift
//  MEME_Academic (iOS)
//
//  ES_R の BLE プロトコル。UUID・コマンド・暗号は Mac 版 MEMELib_Academic.swift / DecEnc.swift と同じ値。
//

import CoreBluetooth
import Foundation

enum Meme {
    static var serviceUUID: CBUUID { CBUUID(string: "D6F25BD1-5B54-4360-96D8-7AA62E04C7EF") }
    static var notifyUUID: CBUUID { CBUUID(string: "D6F25BD4-5B54-4360-96D8-7AA62E04C7EF") }
    static var writeUUID: CBUUID { CBUUID(string: "D6F25BD2-5B54-4360-96D8-7AA62E04C7EF") }

    static let packetLength = 20

    // 端末へ送るコマンド(ADN_*)
    static let opGetData: UInt8 = 0xA0
    static let opGetDevInfo: UInt8 = 0xA1
    static let opGetMode: UInt8 = 0xA3
    static let opSetMode: UInt8 = 0xA4
    static let opGet6AxisParams: UInt8 = 0xA9

    // 端末から届く応答・データ(AUP_REPORT_*)
    static let repDevInfo: UInt8 = 0x81
    static let repMode: UInt8 = 0x83
    static let rep6AxisParams: UInt8 = 0x89
    static let repResp: UInt8 = 0x8F
    static let repAcademic1: UInt8 = 0x98   // Standard
    static let repAcademic2: UInt8 = 0x99   // Full
    static let repAcademic3: UInt8 = 0x9A   // Quaternion

    /// ADN_SET_MODE の mode バイト(Mac 版 MEMEMode_*)
    enum Mode: UInt8, CaseIterable, Identifiable {
        case standard = 1, full = 2, quaternion = 3
        var id: UInt8 { rawValue }
        var label: String {
            switch self {
            case .standard: "Standard"
            case .full: "Full"
            case .quaternion: "Quaternion"
            }
        }
    }

    /// ADN_SET_MODE の quality バイト(Mac 版 MEMEQuality_*)
    enum Rate: UInt8, CaseIterable, Identifiable {
        case hz100 = 1, hz50 = 2
        var id: UInt8 { rawValue }
        var hz: Int { self == .hz100 ? 100 : 50 }
    }
}

/// 復号済みの 20byte パケット 1 個。
struct MemePacket {
    let bytes: [UInt8]

    var type: UInt8 { bytes[1] }
    var isData: Bool { (Meme.repAcademic1...Meme.repAcademic3).contains(type) }
    /// 12bit の送信カウンタ(0..4095)
    var counter: Int { Int(bytes[2]) | (Int(bytes[3] & 0x0F) << 8) }
    /// 電池残量(0..5)
    var battery: Int { Int(bytes[3] >> 4) }
}

enum MemeCodec {
    private static let key: [UInt8] = [
        0x39, 0xCC, 0x6D, 0xAB, 0x9E, 0x07, 0x1A, 0xDE, 0x67,
        0x49, 0x71, 0x9A, 0x5B, 0x69, 0x0F, 0x17, 0xC9, 0xB1
    ]

    /// 先頭 2byte(長さ・種別)を除く 18byte を変換する。
    static func encode(_ b: inout [UInt8]) {
        for i in 0..<key.count { b[2 + i] = (b[2 + i] ^ key[i]) &+ UInt8(i) }
    }

    static func decode(_ b: inout [UInt8]) {
        for i in 0..<key.count { b[2 + i] = (b[2 + i] &- UInt8(i)) ^ key[i] }
    }

    /// 端末へ書き込む 20byte のコマンド。params は [バイト位置: 値]。
    static func command(_ op: UInt8, _ params: [Int: UInt8] = [:]) -> Data {
        var b = [UInt8](repeating: 0, count: Meme.packetLength)
        b[0] = UInt8(Meme.packetLength)
        b[1] = op
        for (i, v) in params { b[i] = v }
        encode(&b)
        return Data(b)
    }

    /// 通知 1 回分を 20byte ずつに分けて復号する。
    /// iOS は MTU を自動で広げるので、1 通知に複数パケットが連結されて届く可能性がある
    /// (Mac 版は先頭 20byte しか見ていない)。各 20byte は個別に暗号化されている前提で、
    /// 先頭が長さ 20 のものだけを拾う。
    static func split(_ data: Data) -> [MemePacket] {
        let all = [UInt8](data)
        var out: [MemePacket] = []
        var offset = 0
        while offset + Meme.packetLength <= all.count {
            var b = Array(all[offset..<offset + Meme.packetLength])
            if b[0] != UInt8(Meme.packetLength) { break }
            decode(&b)
            out.append(MemePacket(bytes: b))
            offset += Meme.packetLength
        }
        return out
    }
}
