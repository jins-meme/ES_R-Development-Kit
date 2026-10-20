//
//  ContentView.swift
//  MEME_Academic (iOS)
//

import SwiftUI

struct ContentView: View {
    @Bindable var central: MemeCentral

    var body: some View {
        NavigationStack {
            List {
                connectionSection
                measureSection
                if central.shown.received > 0 {
                    resultSection
                    phaseSection
                    sizeSection
                    intervalSection
                }
                if let url = central.logURL {
                    Section("Log") {
                        ShareLink(item: url) { Label(url.lastPathComponent, systemImage: "square.and.arrow.up") }
                    }
                }
            }
            .navigationTitle("ES_R Rate Probe")
        }
    }

    private var busy: Bool {
        central.state == .measuring || central.state == .starting
    }

    private var connectionSection: some View {
        Section("Device") {
            row("State", central.state.rawValue)
            if let name = central.deviceName, central.state != .idle && central.state != .scanning {
                row("Device", name)
                row("Firmware", central.firmware ?? "-")
                row("ATT MTU", central.mtu.map(String.init) ?? "-")
            }
            if !central.lastEvent.isEmpty {
                row("Last event", central.lastEvent)
            }
            switch central.state {
            case .idle:
                Button("Scan") { central.scan() }
            case .scanning:
                Button("Stop scan") { central.stopScan() }
                ForEach(central.found) { d in
                    Button { central.connect(d.id) } label: {
                        HStack {
                            Text(d.name)
                            Spacer()
                            Text("\(d.rssi) dBm").foregroundStyle(.secondary)
                        }
                    }
                }
            case .connecting, .ready, .starting, .measuring:
                Button("Disconnect", role: .destructive) { central.disconnect() }
            case .unavailable:
                EmptyView()
            }
        }
    }

    private var measureSection: some View {
        Section("Measurement") {
            Picker("Mode", selection: $central.mode) {
                ForEach(Meme.Mode.allCases) { Text($0.label).tag($0) }
            }
            .disabled(busy)
            Picker("Rate", selection: $central.rate) {
                ForEach(Meme.Rate.allCases) { Text("\($0.hz) Hz").tag($0) }
            }
            .disabled(busy)
            if central.state == .ready {
                Button("Start") { central.start() }
            } else if busy {
                Button("Stop") { central.stop() }
            }
        }
    }

    private var resultSection: some View {
        let s = central.shown
        let avg = central.shownElapsed > 0 ? Double(s.received) / central.shownElapsed : 0
        return Section("Result") {
            row("Elapsed", String(format: "%.0f s", central.shownElapsed))
            row("Received", "\(s.received)")
            row("Lost", "\(s.lost) (" + String(format: "%.2f%%", s.lossRate * 100) + ")")
            row("Average", String(format: "%.1f Hz", avg))
            row("Last second", "\(central.shownHz) Hz")
            row("Notifications", "\(s.notifications)")
            row("Largest gap", "\(s.largestGap) samples")
            row("Max interval", String(format: "%.0f ms", s.maxIntervalMs))
            row("Duplicates", "\(s.duplicates)")
            row("Disconnects", "\(s.disconnects)")
            row("Battery", s.battery.map { "\($0) / 5" } ?? "-")
        }
    }

    private var phaseSection: some View {
        Section("By app state") {
            ForEach(ProbePhase.allCases) { p in
                let t = central.shown.total(for: p, now: central.shownElapsed)
                if t.seconds > 0 || t.received > 0 {
                    let hz = t.seconds > 0 ? Double(t.received) / t.seconds : 0
                    row(p.rawValue, String(format: "%.0f s  %.1f Hz  lost %d", t.seconds, hz, t.lost))
                }
            }
        }
    }

    private var sizeSection: some View {
        Section("Notification size") {
            ForEach(central.shown.sizes.keys.sorted(), id: \.self) { size in
                row("\(size) bytes", "\(central.shown.sizes[size] ?? 0)")
            }
        }
    }

    private var intervalSection: some View {
        Section("Notification interval") {
            ForEach(central.shown.intervals.indices, id: \.self) { i in
                row(intervalLabel(i), "\(central.shown.intervals[i])")
            }
        }
    }

    private func intervalLabel(_ i: Int) -> String {
        let bounds = RateStats.intervalBoundsMs.map { Int($0) }
        if i == 0 { return "< \(bounds[0]) ms" }
        if i == bounds.count { return "≥ \(bounds[i - 1]) ms" }
        return "\(bounds[i - 1])–\(bounds[i]) ms"
    }

    private func row(_ title: String, _ value: String) -> some View {
        HStack {
            Text(title)
            Spacer()
            Text(value).foregroundStyle(.secondary).monospacedDigit()
        }
    }
}
