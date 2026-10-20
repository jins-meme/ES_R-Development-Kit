//
//  RateStatsTests.swift
//  MEME_AcademicTests
//

import XCTest
@testable import MEME_Academic

final class RateStatsTests: XCTestCase {

    func testSequentialHasNoLoss() {
        var s = RateStats()
        for i in 0..<300 {
            s.noteSample(counter: i % 4096, battery: 5, at: Double(i) / 100, hz: 100)
        }
        XCTAssertEqual(s.received, 300)
        XCTAssertEqual(s.lost, 0)
    }

    func testGapCountsMissingSamples() {
        var s = RateStats()
        s.noteSample(counter: 10, battery: 5, at: 0, hz: 100)
        XCTAssertEqual(s.noteSample(counter: 14, battery: 5, at: 0.04, hz: 100), 3)
        XCTAssertEqual(s.lost, 3)
        XCTAssertEqual(s.largestGap, 3)
        XCTAssertEqual(s.lossRate, 3.0 / 5.0, accuracy: 1e-9)
    }

    func testWrapAroundIsNotLoss() {
        var s = RateStats()
        s.noteSample(counter: 4094, battery: 5, at: 0, hz: 100)
        s.noteSample(counter: 4095, battery: 5, at: 0.01, hz: 100)
        s.noteSample(counter: 0, battery: 5, at: 0.02, hz: 100)
        XCTAssertEqual(s.lost, 0)
    }

    func testDuplicateIsNotCounted() {
        var s = RateStats()
        s.noteSample(counter: 5, battery: 5, at: 0, hz: 100)
        s.noteSample(counter: 5, battery: 5, at: 0.01, hz: 100)
        XCTAssertEqual(s.received, 1)
        XCTAssertEqual(s.duplicates, 1)
        XCTAssertEqual(s.lost, 0)
    }

    /// 12bit カウンタが一周以上する長い途切れは経過時間で補う
    func testLongStallUsesElapsedTime() {
        var s = RateStats()
        s.noteSample(counter: 100, battery: 5, at: 0, hz: 100)
        // 50 秒(5000 サンプル)止まった後: カウンタ差は (5100 - 100) % 4096 = 904
        s.noteSample(counter: 5100 % 4096, battery: 5, at: 50, hz: 100)
        XCTAssertEqual(s.lost, 4999)
    }

    func testPhaseTotals() {
        var s = RateStats(phase: .active)
        s.noteSample(counter: 0, battery: 5, at: 0, hz: 100)
        s.noteSample(counter: 1, battery: 5, at: 0.01, hz: 100)
        s.setPhase(.background, at: 10)
        s.noteSample(counter: 5, battery: 5, at: 10.05, hz: 100)
        XCTAssertEqual(s.total(for: .active, now: 15).seconds, 10, accuracy: 1e-9)
        XCTAssertEqual(s.total(for: .active, now: 15).received, 2)
        XCTAssertEqual(s.total(for: .background, now: 15).seconds, 5, accuracy: 1e-9)
        XCTAssertEqual(s.total(for: .background, now: 15).lost, 3)
    }

    func testNotificationHistogram() {
        var s = RateStats()
        XCTAssertNil(s.noteNotification(size: 20, at: 0))
        XCTAssertEqual(s.noteNotification(size: 20, at: 0.001)!, 1, accuracy: 1e-6)   // < 2ms
        XCTAssertEqual(s.noteNotification(size: 40, at: 0.021)!, 20, accuracy: 1e-6)   // 20–30ms
        s.noteNotification(size: 20, at: 0.321)                                          // ≥ 100ms
        XCTAssertEqual(s.sizes, [20: 3, 40: 1])
        XCTAssertEqual(s.intervals[0], 1)
        XCTAssertEqual(s.intervals[5], 1)
        XCTAssertEqual(s.intervals.last, 1)
        XCTAssertEqual(s.maxIntervalMs, 300, accuracy: 1e-6)
    }

    func testLogFillsEmptySeconds() {
        var lines: [String] = []
        let log = RateLog(start: Date(timeIntervalSince1970: 0)) { lines.append($0) }
        log.addSample(lost: 0, at: 0.1, phase: .active)
        log.addSample(lost: 2, at: 0.5, phase: .active)
        log.event("phase active -> background", at: 2.2, phase: .background)
        log.addSample(lost: 0, at: 2.3, phase: .background)
        log.finish(at: 2.5, phase: .background)
        XCTAssertEqual(lines, [
            RateLog.header,
            "0,1970-01-01T00:00:00Z,active,2,2,0,0.0,\n",
            "1,1970-01-01T00:00:01Z,active,0,0,0,0.0,\n",   // 受信の無い秒は直前の状態のまま
            "2,1970-01-01T00:00:02Z,background,1,0,0,0.0,phase active -> background\n",
        ])
        XCTAssertEqual(log.lastFullSecondSamples, 1)
    }
}
