//
//  MemeCentral.swift
//  MEME_Academic (iOS)
//
//  スキャン → 接続 → 計測 → 集計。接続直後の手順(DEV_INFO → MODE → 6AXIS_PRMS)は Mac 版と同じ。
//  100Hz の受信ごとに画面を描き直すと重いので、集計は無観測の stats に貯めて 4Hz で shown へ写す。
//

import CoreBluetooth
import Foundation
import Observation
import UIKit

@MainActor
@Observable
final class MemeCentral: NSObject {
    enum State: String {
        case unavailable = "Bluetooth unavailable"
        case idle = "Not connected"
        case scanning = "Scanning"
        case connecting = "Connecting"
        case ready = "Connected"
        case starting = "Starting"
        case measuring = "Measuring"
    }

    struct Found: Identifiable {
        let id: UUID
        let name: String
        var rssi: Int
    }

    private(set) var state: State = .unavailable
    private(set) var found: [Found] = []
    private(set) var deviceName: String?
    private(set) var firmware: String?
    /// ATT MTU(書き込み上限 + 3)
    private(set) var mtu: Int?
    private(set) var shown = RateStats()
    private(set) var shownElapsed = 0.0
    private(set) var shownHz = 0
    private(set) var logURL: URL?
    private(set) var lastEvent = ""
    var mode: Meme.Mode = .full
    var rate: Meme.Rate = .hz100

    @ObservationIgnored private var central: CBCentralManager!
    @ObservationIgnored private var peripherals: [UUID: CBPeripheral] = [:]
    @ObservationIgnored private var peripheral: CBPeripheral?
    @ObservationIgnored private var notifyChar: CBCharacteristic?
    @ObservationIgnored private var writeChar: CBCharacteristic?
    @ObservationIgnored private var stats = RateStats()
    @ObservationIgnored private var log: RateLog?
    @ObservationIgnored private var logHandle: FileHandle?
    @ObservationIgnored private var startedAt = Date()
    @ObservationIgnored private var measuringHz = 100
    @ObservationIgnored private var uiTimer: Timer?
    @ObservationIgnored private var ackTimer: Timer?
    @ObservationIgnored private var appPhase: ProbePhase = .active
    @ObservationIgnored private var locked = false

    var phase: ProbePhase { locked ? .locked : appPhase }

    override init() {
        super.init()
        central = CBCentralManager(delegate: self, queue: nil)
        observeAppState()
    }

    // MARK: - 操作

    func scan() {
        guard state == .idle else { return }
        found.removeAll()
        peripherals.removeAll()
        central.scanForPeripherals(withServices: [Meme.serviceUUID])
        state = .scanning
    }

    func stopScan() {
        guard state == .scanning else { return }
        central.stopScan()
        state = .idle
    }

    func connect(_ id: UUID) {
        guard let p = peripherals[id] else { return }
        central.stopScan()
        peripheral = p
        p.delegate = self
        deviceName = p.name
        state = .connecting
        central.connect(p)
    }

    func disconnect() {
        guard let p = peripheral else { return }
        if state == .measuring { stop() }
        central.cancelPeripheralConnection(p)
    }

    /// モードを設定し、その ACK(0x8F)を待ってから送信開始を送る。
    /// ACK が来なくても 2 秒で開始する(届いたデータで結果は分かるため)。
    func start() {
        guard state == .ready else { return }
        stats = RateStats(phase: phase)
        startedAt = Date()
        measuringHz = rate.hz
        openLog()
        note("start \(mode.label) \(rate.hz)Hz")
        send(Meme.opSetMode, [4: mode.rawValue, 5: rate.rawValue])
        state = .starting
        ackTimer = Timer.scheduledTimer(withTimeInterval: 2, repeats: false) { [weak self] _ in
            MainActor.assumeIsolated {
                self?.note("no ACK for SET_MODE")
                self?.beginReport()
            }
        }
        uiTimer = Timer.scheduledTimer(withTimeInterval: 0.25, repeats: true) { [weak self] _ in
            MainActor.assumeIsolated { self?.publish() }
        }
    }

    func stop() {
        guard state == .measuring || state == .starting else { return }
        send(Meme.opGetData, [2: 0x00])
        state = .ready
        finishMeasurement("stop")
    }

    /// 直近の計測結果(自動計測が Documents へ書き、Mac 側で読む)。
    func summaryJSON(reason: String) -> String {
        let t = shownElapsed
        var phases: [String: Any] = [:]
        for p in ProbePhase.allCases {
            let total = stats.total(for: p, now: t)
            guard total.seconds > 0 || total.received > 0 else { continue }
            phases[p.rawValue] = [
                "seconds": total.seconds,
                "received": total.received,
                "lost": total.lost,
                "hz": total.seconds > 0 ? Double(total.received) / total.seconds : 0,
            ]
        }
        let bounds = RateStats.intervalBoundsMs.map { Int($0) }
        var intervals: [String: Int] = [:]
        for (i, n) in stats.intervals.enumerated() {
            intervals[i < bounds.count ? "<\(bounds[i])ms" : ">=\(bounds[i - 1])ms"] = n
        }
        let summary: [String: Any] = [
            "reason": reason,
            "device": deviceName ?? "",
            "firmware": firmware ?? "",
            "mtu": mtu ?? 0,
            "mode": mode.label,
            "rate": rate.hz,
            "elapsed": t,
            "received": stats.received,
            "lost": stats.lost,
            "lossRate": stats.lossRate,
            "averageHz": t > 0 ? Double(stats.received) / t : 0,
            "largestGap": stats.largestGap,
            "maxIntervalMs": stats.maxIntervalMs,
            "duplicates": stats.duplicates,
            "disconnects": stats.disconnects,
            "notifications": stats.notifications,
            "sizes": Dictionary(uniqueKeysWithValues: stats.sizes.map { ("\($0.key)", $0.value) }),
            "intervals": intervals,
            "phases": phases,
            "log": logURL?.lastPathComponent ?? "",
        ]
        let data = (try? JSONSerialization.data(withJSONObject: summary, options: [.prettyPrinted, .sortedKeys]))
        return data.flatMap { String(data: $0, encoding: .utf8) } ?? "{}"
    }

    // MARK: - 内部

    private func beginReport() {
        ackTimer?.invalidate()
        ackTimer = nil
        guard state == .starting else { return }
        send(Meme.opGetData, [2: 0x01])
        state = .measuring
    }

    private func finishMeasurement(_ reason: String) {
        ackTimer?.invalidate()
        ackTimer = nil
        uiTimer?.invalidate()
        uiTimer = nil
        let t = elapsed()
        log?.event("\(reason): received=\(stats.received) lost=\(stats.lost)", at: t, phase: phase)
        log?.finish(at: t, phase: phase)
        log = nil
        try? logHandle?.close()
        logHandle = nil
        publish()
        shownElapsed = t
    }

    private func send(_ op: UInt8, _ params: [Int: UInt8] = [:]) {
        guard let p = peripheral, let c = writeChar else { return }
        p.writeValue(MemeCodec.command(op, params), for: c, type: .withResponse)
    }

    private func elapsed() -> Double {
        Date().timeIntervalSince(startedAt)
    }

    private func note(_ text: String) {
        lastEvent = text
        log?.event(text, at: elapsed(), phase: phase)
    }

    private func publish() {
        shown = stats
        shownElapsed = state == .measuring || state == .starting ? elapsed() : shownElapsed
        shownHz = log?.lastFullSecondSamples ?? shownHz
    }

    /// Documents に置く(Info.plist の UIFileSharingEnabled で Finder / ファイル App から取り出せる)。
    /// ファイル名は他のアプリの CSV と同じく UTC。
    private func openLog() {
        let f = DateFormatter()
        f.dateFormat = "yyyyMMdd_HHmmss"
        f.timeZone = TimeZone(identifier: "UTC")
        f.locale = Locale(identifier: "en_US_POSIX")
        let dir = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
        let url = dir.appendingPathComponent("rate_\(f.string(from: startedAt))_\(mode.label)_\(rate.hz)Hz.csv")
        FileManager.default.createFile(atPath: url.path, contents: nil)
        let handle = try? FileHandle(forWritingTo: url)
        logHandle = handle
        logURL = url
        log = RateLog(start: startedAt) { line in
            try? handle?.write(contentsOf: Data(line.utf8))
        }
    }

    private func handle(_ pkt: MemePacket, at t: Double) {
        switch pkt.type {
        case Meme.repDevInfo:
            firmware = "\(pkt.bytes[6]).\(pkt.bytes[5]).\(pkt.bytes[4])"
            send(Meme.opGetMode)
        case Meme.repMode:
            send(Meme.opGet6AxisParams)
        case Meme.rep6AxisParams:
            if state == .connecting { state = .ready }
        case Meme.repResp:
            if state == .starting { beginReport() }
        case _ where pkt.isData:
            guard state == .measuring else { return }
            let missing = stats.noteSample(counter: pkt.counter, battery: pkt.battery, at: t, hz: measuringHz)
            log?.addSample(lost: missing, at: t, phase: phase)
        default:
            break
        }
    }

    // MARK: - アプリの状態

    private func observeAppState() {
        let nc = NotificationCenter.default
        let pairs: [(Notification.Name, @MainActor @Sendable (MemeCentral) -> Void)] = [
            (UIApplication.didBecomeActiveNotification, { $0.appPhase = .active }),
            (UIApplication.willResignActiveNotification, { $0.appPhase = .inactive }),
            (UIApplication.didEnterBackgroundNotification, { $0.appPhase = .background }),
            (UIApplication.willEnterForegroundNotification, { $0.appPhase = .inactive }),
            // パスコードありの端末でだけ届く(ロックから約 10 秒後)。画面オフの目安。
            (UIApplication.protectedDataWillBecomeUnavailableNotification, { $0.locked = true }),
            (UIApplication.protectedDataDidBecomeAvailableNotification, { $0.locked = false }),
        ]
        for (name, apply) in pairs {
            nc.addObserver(forName: name, object: nil, queue: .main) { [weak self] _ in
                MainActor.assumeIsolated {
                    guard let self else { return }
                    let before = self.phase
                    apply(self)
                    self.phaseChanged(from: before)
                }
            }
        }
    }

    private func phaseChanged(from before: ProbePhase) {
        guard phase != before, state == .measuring || state == .starting else { return }
        let t = elapsed()
        stats.setPhase(phase, at: t)
        note("phase \(before.rawValue) -> \(phase.rawValue)")
    }
}

// MARK: - CBCentralManagerDelegate

extension MemeCentral: @preconcurrency CBCentralManagerDelegate {
    func centralManagerDidUpdateState(_ central: CBCentralManager) {
        if central.state == .poweredOn {
            if state == .unavailable { state = .idle }
        } else {
            if state == .measuring || state == .starting { finishMeasurement("bluetooth off") }
            state = .unavailable
        }
    }

    func centralManager(_ central: CBCentralManager, didDiscover peripheral: CBPeripheral,
                        advertisementData: [String: Any], rssi RSSI: NSNumber) {
        let name = peripheral.name ?? advertisementData[CBAdvertisementDataLocalNameKey] as? String ?? "(no name)"
        peripherals[peripheral.identifier] = peripheral
        if let i = found.firstIndex(where: { $0.id == peripheral.identifier }) {
            found[i].rssi = RSSI.intValue
        } else {
            found.append(Found(id: peripheral.identifier, name: name, rssi: RSSI.intValue))
        }
    }

    func centralManager(_ central: CBCentralManager, didConnect peripheral: CBPeripheral) {
        mtu = peripheral.maximumWriteValueLength(for: .withoutResponse) + 3
        peripheral.discoverServices([Meme.serviceUUID])
    }

    func centralManager(_ central: CBCentralManager, didFailToConnect peripheral: CBPeripheral, error: Error?) {
        lastEvent = "connect failed: \(error?.localizedDescription ?? "unknown")"
        self.peripheral = nil
        state = .idle
    }

    func centralManager(_ central: CBCentralManager, didDisconnectPeripheral peripheral: CBPeripheral,
                        error: Error?) {
        if state == .measuring || state == .starting {
            stats.noteDisconnect()
            finishMeasurement("disconnected \(error?.localizedDescription ?? "")")
        }
        lastEvent = "disconnected \(error?.localizedDescription ?? "")"
        self.peripheral = nil
        notifyChar = nil
        writeChar = nil
        state = .idle
    }
}

// MARK: - CBPeripheralDelegate

extension MemeCentral: @preconcurrency CBPeripheralDelegate {
    func peripheral(_ peripheral: CBPeripheral, didDiscoverServices error: Error?) {
        guard let service = peripheral.services?.first(where: { $0.uuid == Meme.serviceUUID }) else { return }
        peripheral.discoverCharacteristics([Meme.notifyUUID, Meme.writeUUID], for: service)
    }

    func peripheral(_ peripheral: CBPeripheral, didDiscoverCharacteristicsFor service: CBService, error: Error?) {
        for c in service.characteristics ?? [] {
            if c.uuid == Meme.notifyUUID { notifyChar = c }
            if c.uuid == Meme.writeUUID { writeChar = c }
        }
        if let c = notifyChar { peripheral.setNotifyValue(true, for: c) }
    }

    func peripheral(_ peripheral: CBPeripheral, didUpdateNotificationStateFor characteristic: CBCharacteristic,
                    error: Error?) {
        guard error == nil, characteristic.isNotifying else {
            lastEvent = "notify failed: \(error?.localizedDescription ?? "")"
            return
        }
        send(Meme.opGetDevInfo)
    }

    func peripheral(_ peripheral: CBPeripheral, didUpdateValueFor characteristic: CBCharacteristic, error: Error?) {
        guard error == nil, let data = characteristic.value else { return }
        let packets = MemeCodec.split(data)
        let t = elapsed()
        if state == .measuring, packets.contains(where: \.isData) {
            let ms = stats.noteNotification(size: data.count, at: t)
            log?.addNotification(intervalMs: ms, at: t, phase: phase)
        }
        for pkt in packets { handle(pkt, at: t) }
    }
}
