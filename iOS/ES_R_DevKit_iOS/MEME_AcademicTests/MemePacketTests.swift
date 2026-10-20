//
//  MemePacketTests.swift
//  MEME_AcademicTests
//

import XCTest
@testable import MEME_Academic

final class MemePacketTests: XCTestCase {

    /// 暗号化して復号すると元に戻る(先頭 2byte は触らない)
    func testEncodeDecodeRoundTrip() {
        let plain: [UInt8] = (0..<20).map { UInt8($0 * 13 & 0xFF) }
        var b = plain
        MemePacket.encode(&b)
        XCTAssertEqual(Array(b[0..<2]), Array(plain[0..<2]))
        XCTAssertNotEqual(b, plain)
        MemePacket.decode(&b)
        XCTAssertEqual(b, plain)
    }

    func testCommandIsEncrypted() {
        var b = [UInt8](MemePacket.command(0xA0, [2: 0x01]))
        XCTAssertEqual(b.count, 20)
        XCTAssertEqual(b[0], 20)
        XCTAssertEqual(b[1], 0xA0)
        MemePacket.decode(&b)
        XCTAssertEqual(b[2], 0x01)
        XCTAssertEqual(Array(b[3...]), [UInt8](repeating: 0, count: 17))
    }

    /// 1 回の通知に 2 パケット連結されていても両方読む。長さ 20 でないところで止める
    func testSplitReadsConcatenatedPackets() {
        let a = MemePacket.command(0x99, [2: 1])
        let b = MemePacket.command(0x99, [2: 2])
        let packets = MemePacket.split(a + b + Data([0x00, 0x01]))
        XCTAssertEqual(packets.count, 2)
        XCTAssertEqual(packets.map { $0[2] }, [1, 2])
        XCTAssertEqual(MemePacket.split(Data([0x13] + [UInt8](repeating: 0, count: 19))).count, 0)
    }

    func testParseFull() throws {
        var b = [UInt8](repeating: 0, count: 20)
        b[0] = 20; b[1] = 0x99
        b[2] = 0x34; b[3] = 0x52                       // カウンタ 0x234、電池 5
        b[4] = 0xFF; b[5] = 0xFF                       // accX = -1
        b[16] = 0x10; b[17] = 0x00                     // eogL = 16
        b[18] = 0x04; b[19] = 0x00                     // eogR = 4
        let d = try XCTUnwrap(MemePacket.parse(b) as? AcademicFullData)
        XCTAssertEqual(d.cnt, 0x234)
        XCTAssertEqual(d.battLv, 5)
        XCTAssertEqual(d.accX, -1)
        XCTAssertEqual(d.eogH, 12)
        XCTAssertEqual(d.eogV, -10)
        XCTAssertEqual(MeasurementMode.full.values(of: d)?.count, MeasurementMode.full.columns.count)
    }

    func testParseStandardAndQuaternion() throws {
        var b = [UInt8](repeating: 0, count: 20)
        b[0] = 20; b[1] = 0x98
        b[10] = 0x0A                                   // eogL1 = 10
        b[12] = 0x02                                   // eogR1 = 2
        let s = try XCTUnwrap(MemePacket.parse(b) as? AcademicStandardData)
        XCTAssertEqual(s.eogH1, 8)
        XCTAssertEqual(s.eogV1, -6)

        b = [UInt8](repeating: 0, count: 20)
        b[0] = 20; b[1] = 0x9A
        b[4] = 0xFE; b[5] = 0xFF; b[6] = 0xFF; b[7] = 0xFF   // W = -2
        let q = try XCTUnwrap(MemePacket.parse(b) as? AcademicQuaternionData)
        XCTAssertEqual(q.quaternionW, -2)

        b[1] = 0x81
        XCTAssertNil(MemePacket.parse(b))
    }
}
