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
    private(set) var dataCountInWindow: Int = 0
    /// 最初のデータが届いた瞬間にセットされる。
    /// 成功率の分母（経過時間）はここを基準にする。
    private var firstDataDate: Date?

    // MARK: - Timer

    /// communication (通信率) の集計ウィンドウ秒数。
    private let communicationWindow: TimeInterval = 1.0
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
        dataCountInWindow = 0
        firstDataDate = nil
    }

    // MARK: - Measurement lifecycle

    func startMeasurement(quality: Int) {
        // startDate は最初のデータ受信時にセットする (bumpDataCount 参照)
        firstDataDate = nil
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
    /// 成功率の再計算は約 0.2 秒間隔（100Hz→20件・50Hz→10件）に集約する。
    /// 1件目を受け取った瞬間に firstDataDate を確定させ、成功率の分母（経過時間）の起点とする。
    func bumpDataCount() {
        if firstDataDate == nil {
            firstDataDate = Date()
        }
        dataCount += 1
        dataCountInWindow += 1
        let stride = max(20 / quality, 1)
        if dataCount % stride == 0 {
            updateSuccessRate()
        }
    }

    // MARK: - Private

    private func updateSuccessRate() {
        guard let firstDataDate else { return }
        let timeCount = Date().timeIntervalSince1970 - firstDataDate.timeIntervalSince1970
        let rate: Double
        if timeCount > 0 {
            rate = (Double(dataCount) / (timeCount * 100.0 / Double(quality))) * 100.0
        } else {
            rate = 0
        }
        let clamped = min(rate, 100.0)
        onSuccessRate?(clamped, String(format: "%.1f%%", clamped))
    }

    private func startCommunicationTimer() {
        communicationTimer = Timer.scheduledTimer(withTimeInterval: communicationWindow, repeats: true) { [weak self] _ in
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
        let comm = (Double(dataCountInWindow) / (communicationWindow * 100.0 / Double(quality))) * 100.0
        let clamped = min(comm, 100.0)
        onCommunication?(clamped, String(format: "%.1f%%", clamped))
        dataCountInWindow = 0
    }
}
