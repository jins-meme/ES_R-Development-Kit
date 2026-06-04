//
//  CommunicationStatsTracker.swift
//  MEME_Academic
//
//  パケット count 差分計算・成功率／通信率の更新を担当するトラッカー。
//

import Foundation

@MainActor
final class CommunicationStatsTracker {

    // MARK: - Per-packet counters

    private(set) var prevCount: Int = -1
    private(set) var prevTime: Int = 0
    private(set) var totalCount: Int = 0
    private(set) var errorCount: Int = 0
    private(set) var quality: Int = 1
    private(set) var dataCount: Int = 0
    private(set) var dataCount200ms: Int = 0
    private var startDate = Date()

    // MARK: - Timer

    private var communicationTimer: Timer?

    // MARK: - Callbacks

    /// successRate (%) / formatted text を ViewModel に通知する。
    var onSuccessRate: ((Double, String) -> Void)?
    /// communication (%) / formatted text を ViewModel に通知する。
    var onCommunication: ((Double, String) -> Void)?

    // MARK: - Reset

    func reset() {
        prevCount = -1
        prevTime = 0
        totalCount = 0
        errorCount = 0
        quality = 1
        dataCount = 0
        dataCount200ms = 0
        startDate = Date()
    }

    // MARK: - Measurement lifecycle

    func startMeasurement(quality: Int) {
        startDate = Date()
        self.quality = quality
        startCommunicationTimer()
    }

    func stopMeasurement() {
        stopCommunicationTimer()
    }

    // MARK: - Per-packet update

    /// データ受信時に呼び出し。totalCount/errorCount/prevCount/prevTime を更新する。
    /// 既存ロジックと完全に同じ。
    func registerPacket(count: Int) {
        var deff = 0
        if prevCount < 0 {
            deff = 0
            prevTime = Int(Date().timeIntervalSince1970)
        } else {
            if prevCount < count {
                deff = count - prevCount
            } else if prevCount > count {
                deff = 0x1000 - prevCount + count
            }
        }
        prevCount = count
        prevTime += deff * 10 * quality

        if deff == 0 {
            totalCount += deff + 1
            errorCount += deff
        } else {
            totalCount += deff
            if deff - 1 > 0 {
                errorCount += deff - 1
            }
        }
    }

    /// 受信完了ごとに呼ぶ。displayCnt 更新ではなく dataCount のみ更新。
    func bumpDataCount() {
        dataCount += 1
        dataCount200ms += 1
        updateSuccessRate()
    }

    // MARK: - Private

    private func updateSuccessRate() {
        let timeCount = Date().timeIntervalSince1970 - startDate.timeIntervalSince1970
        let rate: Double
        if timeCount > 0 {
            rate = (Double(dataCount) / (timeCount * 100.0 / Double(quality))) * 100.0
        } else {
            rate = 0
        }
        onSuccessRate?(rate, String(format: "%.2f%%", rate))
    }

    private func startCommunicationTimer() {
        communicationTimer = Timer.scheduledTimer(withTimeInterval: 0.2, repeats: true) { [weak self] _ in
            Task { @MainActor in
                self?.tickCommunication()
            }
        }
    }

    private func stopCommunicationTimer() {
        communicationTimer?.invalidate()
        communicationTimer = nil
    }

    private func tickCommunication() {
        let comm = (Double(dataCount200ms) / (0.2 * 100.0 / Double(quality))) * 100.0
        onCommunication?(comm, String(format: "%.2f%%", comm))
        dataCount200ms = 0
    }
}
