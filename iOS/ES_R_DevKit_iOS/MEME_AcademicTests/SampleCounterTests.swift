//
//  SampleCounterTests.swift
//  MEME_AcademicTests
//

import XCTest
@testable import MEME_Academic

final class SampleCounterTests: XCTestCase {

    /// 最初のパケットは基準を取るだけ。以降は差分で NUM を進める
    func testFirstPacketIsBaseline() {
        var c = SampleCounter()
        XCTAssertFalse(c.countUp(100))
        XCTAssertTrue(c.countUp(101))
        XCTAssertTrue(c.countUp(102))
        XCTAssertEqual(c.totalCount, 2)
        XCTAssertEqual(c.errorCount, 0)
        XCTAssertEqual(c.successRate, 1)
    }

    /// カウンタが飛んだら取りこぼしとして数え、NUM も飛ばす
    func testGapCountsAsLost() {
        var c = SampleCounter()
        _ = c.countUp(10)
        _ = c.countUp(11)
        _ = c.countUp(15)
        XCTAssertEqual(c.totalCount, 5)
        XCTAssertEqual(c.errorCount, 3)
        XCTAssertEqual(c.successRate, 0.4, accuracy: 1e-9)
    }

    /// 4095 → 0 の周回
    func testWrapAround() {
        var c = SampleCounter()
        _ = c.countUp(4094)
        _ = c.countUp(4095)
        _ = c.countUp(0)
        _ = c.countUp(1)
        XCTAssertEqual(c.totalCount, 3)
        XCTAssertEqual(c.errorCount, 0)
    }

    /// 0.4 秒に 40 件(100Hz)なら 100%
    func testCommRate() {
        var c = SampleCounter()
        _ = c.countUp(0)
        for i in 1...40 { _ = c.countUp(i) }
        XCTAssertEqual(c.commRateTick(hz: 100), 1, accuracy: 1e-9)
        for i in 41...60 { _ = c.countUp(i) }
        // 今回 50%、前回 100% の平均
        XCTAssertEqual(c.commRateTick(hz: 100), 0.75, accuracy: 1e-9)
    }
}
