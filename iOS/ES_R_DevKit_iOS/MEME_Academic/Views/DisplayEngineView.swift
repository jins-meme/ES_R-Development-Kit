//
//  DisplayEngineView.swift
//  MEME_Academic (iOS)
//
//  Display Engine。グラフ画面(WebView)の中身 zip の一覧を出し、使う 1 つを選ぶ・取り込む・消す(Mac 版の DisplayEngineView と同じ働き)。
//  置き場と検査は WebContentStore。同梱の標準版(Standard)は規定で、消せない。計測中・再生中は切り替えない。
//

import SwiftUI
import UniformTypeIdentifiers

struct DisplayEngineView: View {

    @Environment(\.dismiss) private var dismiss
    @Environment(MEMEViewModel.self) private var viewModel
    /// 計測中・再生中は切り替えない(グラフ画面を読み込み直すと表示中のものが消えるため)
    private var busy: Bool { viewModel.isMeasuring || viewModel.isReplaying }

    @State private var entries: [WebContentEntry] = []
    @State private var activeId = WebContentStore.builtInId
    @State private var message = ""
    @State private var messageIsError = false
    @State private var picking = false

    var body: some View {
        NavigationStack {
            List {
                Section {
                    ForEach(entries) { entry in
                        row(entry)
                    }
                    .onDelete { offsets in
                        for i in offsets where !entries[i].isBuiltIn { remove(entries[i]) }
                    }
                    .deleteDisabled(busy)
                } footer: {
                    Text("The zip that draws the graph. You can keep several, but only one is active. Swipe to delete an added zip.")
                }

                if !message.isEmpty {
                    Section {
                        Text(message).foregroundStyle(messageIsError ? Color.red : Color.secondary)
                    }
                }
                if busy {
                    Section {
                        Text("Stop the measurement or replay to change the display engine.").foregroundStyle(.secondary)
                    }
                }
            }
            .navigationTitle("Display Engine")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .topBarLeading) {
                    Button("Add zip…") { picking = true }.disabled(busy)
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Done") { dismiss() }
                }
            }
            .fileImporter(isPresented: $picking, allowedContentTypes: [.zip]) { result in
                if case .success(let url) = result { add(url) }
            }
            .onAppear { appear() }
        }
    }

    private func row(_ entry: WebContentEntry) -> some View {
        let isActive = entry.id == activeId
        return Button {
            activate(entry)
        } label: {
            HStack(spacing: 12) {
                Image(systemName: isActive ? "largecircle.fill.circle" : "circle")
                    .foregroundStyle(isActive ? Theme.accent : Color.secondary)
                    .imageScale(.large)
                VStack(alignment: .leading, spacing: 2) {
                    Text(entry.manifest.displayName).foregroundStyle(Color.primary)
                    Text(entry.isBuiltIn ? "Built-in (default)" : entry.manifest.name)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
                Spacer()
                if isActive { Text("Active").font(.caption).foregroundStyle(.secondary) }
            }
        }
        .disabled(busy)
        .deleteDisabled(entry.isBuiltIn || busy)
    }

    /// 開いたとき: 落ちる zip をやめて標準版へ戻したばかりなら、その知らせを出す
    private func appear() {
        refresh()
        if let notice = WebContentStore.shared.takeFallbackNotice() {
            message = notice
            messageIsError = true
        }
    }

    private func refresh() {
        entries = WebContentStore.shared.entries
        activeId = WebContentStore.shared.activeId
    }

    private func activate(_ entry: WebContentEntry) {
        guard !busy, entry.id != activeId else { return }
        WebContentStore.shared.activate(entry.id)
        message = ""
        viewModel.reloadGraph()
        refresh()
    }

    /// 選んだ zip を取り込み、一覧に足して、それを使う。検査に通らなければ何も変えず、理由を出す
    private func add(_ url: URL) {
        guard !busy else { return }
        let scoped = url.startAccessingSecurityScopedResource()
        defer { if scoped { url.stopAccessingSecurityScopedResource() } }
        do {
            let (entry, outcome, changed) = try WebContentStore.shared.addAndActivate(url)
            switch outcome {
            case .added: message = ""
            case .alreadyAdded: message = "\(entry.manifest.displayName) is already in the list."
            case .replaced: message = "Replaced the zip named \"\(entry.manifest.name)\" with \(entry.manifest.displayName)."
            }
            messageIsError = false
            if changed { viewModel.reloadGraph() }
        } catch {
            message = error.localizedDescription
            messageIsError = true
        }
        refresh()
    }

    private func remove(_ entry: WebContentEntry) {
        guard !busy else { return }
        do {
            if try WebContentStore.shared.remove(entry.id) { viewModel.reloadGraph() }
            message = ""
        } catch {
            message = error.localizedDescription
            messageIsError = true
        }
        refresh()
    }
}
