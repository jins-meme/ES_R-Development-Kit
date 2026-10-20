//
//  MemeProtocolTests.swift
//  MEME_AcademicTests
//

import XCTest
@testable import MEME_Academic

final class MemeProtocolTests: XCTestCase {

    /// 端末が送るのと同じ形の暗号化済みデータパケット
    private func dataPacket(counter: Int, battery: Int = 4, type: UInt8 = Meme.repAcademic2) -> [UInt8] {
        var b = [UInt8](repeating: 0, count: 20)
        b[0] = 20
        b[1] = type
        b[2] = UInt8(counter & 0xFF)
        b[3] = UInt8((counter >> 8) & 0x0F) | UInt8(battery << 4)
        for i in 4..<20 { b[i] = UInt8(i * 7 & 0xFF) }
        MemeCodec.encode(&b)
        return b
    }

    func testEncodeDecodeRoundTrip() {
        let original: [UInt8] = (0..<20).map { UInt8(($0 * 37 + 11) & 0xFF) }
        var b = original
        MemeCodec.encode(&b)
        XCTAssertEqual(Array(b[0..<2]), Array(original[0..<2]), "先頭 2byte は暗号化しない")
        XCTAssertNotEqual(b, original)
        MemeCodec.decode(&b)
        XCTAssertEqual(b, original)
    }

    func testCommandIsEncodedWithHeader() {
        var b = [UInt8](MemeCodec.command(Meme.opSetMode, [4: 2, 5: 1]))
        XCTAssertEqual(b.count, 20)
        XCTAssertEqual(b[0], 20)
        XCTAssertEqual(b[1], 0xA4)
        MemeCodec.decode(&b)
        XCTAssertEqual(b[4], 2)
        XCTAssertEqual(b[5], 1)
    }

    func testSplitSinglePacket() {
        let packets = MemeCodec.split(Data(dataPacket(counter: 0x123, battery: 3)))
        XCTAssertEqual(packets.count, 1)
        XCTAssertTrue(packets[0].isData)
        XCTAssertEqual(packets[0].counter, 0x123)
        XCTAssertEqual(packets[0].battery, 3)
        XCTAssertEqual(packets[0].bytes[4], 28)
    }

    func testSplitConcatenatedPackets() {
        let data = Data(dataPacket(counter: 4095) + dataPacket(counter: 0))
        let packets = MemeCodec.split(data)
        XCTAssertEqual(packets.map(\.counter), [4095, 0])
    }

    func testSplitIgnoresShortTailAndBadHeader() {
        XCTAssertTrue(MemeCodec.split(Data([20, 0x99, 0, 0])).isEmpty)
        var bad = dataPacket(counter: 2)
        bad[0] = 0
        XCTAssertEqual(MemeCodec.split(Data(dataPacket(counter: 1) + bad)).map(\.counter), [1])
    }
}
