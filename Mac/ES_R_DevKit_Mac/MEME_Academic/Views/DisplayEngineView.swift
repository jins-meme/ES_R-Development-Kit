//
//  DisplayEngineView.swift
//  MEME_Academic
//
//  Display Engine ダイアログ。グラフ画面(WebView)の中身 zip の一覧を出し、使う 1 つを選ぶ・取り込む・消す。
//  置き場と検査は WebContentStore。同梱の標準版(Standard)は規定で、消せない。
//

import SwiftUI
import AppKit
import UniformTypeIdentifiers

struct DisplayEngineView: View {

    @Environment(\.dismiss) private var dismiss
    @Environment(MEMEViewModel.self) private var viewModel
    /// 計測中・再生中は切り替えない(グラフ画面を読み込み直すと表示中のものが消えるため。Android と同じ)
    private var busy: Bool { viewModel.isInputDisabled }

    @State private var entries: [WebContentEntry] = []
    @State private var activeId: String = WebContentStore.builtInId
    @State private var message: String = ""
    /// message が失敗の理由か(赤)、知らせか(灰)
    @State private var messageIsError: Bool = false
    @State private var removing: WebContentEntry?

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Display Engine")
                .font(.title2).bold()
            Text("The zip that draws the graph. You can keep several, but only one is active.")
                .foregroundStyle(.secondary)

            VStack(spacing: 0) {
                ForEach(entries) { entry in
                    row(entry)
                    if entry.id != entries.last?.id { Divider() }
                }
            }
            .background(Color(nsColor: .controlBackgroundColor))
            .clipShape(RoundedRectangle(cornerRadius: 6))
            .overlay(RoundedRectangle(cornerRadius: 6).stroke(Color.secondary.opacity(0.3), lineWidth: 1))

            // 取り込み・削除に失敗した理由(赤)か、取り込んだ結果の知らせ(同じファイルだった・置き換えた)
            if !message.isEmpty {
                Text(message)
                    .font(.callout)
                    .foregroundStyle(messageIsError ? Color.red : Color.secondary)
                    .fixedSize(horizontal: false, vertical: true)
            }
            if busy {
                Text("Stop the measurement or replay to change the display engine.")
                    .font(.callout)
                    .foregroundStyle(.secondary)
            }

            Divider()

            HStack {
                Button("Add zip…") { addZip() }
                    .disabled(busy)
                Spacer()
                Button("Done") { dismiss() }
                    .keyboardShortcut(.defaultAction)
            }
        }
        .padding(24)
        .frame(width: 560)
        .onAppear { refresh() }
        .confirmationDialog("Delete \(removing?.manifest.displayName ?? "")?",
                            isPresented: Binding(get: { removing != nil }, set: { if !$0 { removing = nil } }),
                            presenting: removing) { entry in
            Button("Delete", role: .destructive) { remove(entry) }
            Button("Cancel", role: .cancel) {}
        } message: { _ in
            Text("The zip is removed from this app. To use it again, add the zip file again.")
        }
    }

    private func row(_ entry: WebContentEntry) -> some View {
        let isActive = entry.id == activeId
        return HStack(spacing: 10) {
            Button { activate(entry) } label: {
                Image(systemName: isActive ? "largecircle.fill.circle" : "circle")
                    .foregroundStyle(isActive ? Color.accentColor : Color.secondary)
                    .imageScale(.large)
            }
            .buttonStyle(.plain)
            .disabled(busy)
            .help(isActive ? "Active" : "Use this one")

            VStack(alignment: .leading, spacing: 2) {
                Text(entry.manifest.displayName).monospacedDigit()
                Text(entry.isBuiltIn ? "Built-in (default)" : entry.manifest.name)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
            Spacer()
            if isActive {
                Text("Active").font(.caption).foregroundStyle(.secondary)
            }
            if !entry.isBuiltIn {
                Button("Delete") { removing = entry }
                    .disabled(busy)
            }
        }
        .padding(.horizontal, 12)
        .padding(.vertical, 8)
        .contentShape(Rectangle())
        .onTapGesture { activate(entry) }
    }

    private func refresh() {
        let store = WebContentStore.shared
        entries = store.entries
        activeId = store.activeId
    }

    private func activate(_ entry: WebContentEntry) {
        guard !busy, entry.id != activeId else { return }
        WebContentStore.shared.activate(entry.id)
        message = ""
        viewModel.reloadGraph()
        refresh()
    }

    /// zip を選んで取り込み、一覧に足す(有効にはしない)。検査に通らなければ何も変えず、理由を出す。
    /// 同じファイルなら何もしない。同じ name のものを使っていて置き換えたときは、グラフ画面を読み込み直す。
    private func addZip() {
        guard !busy else { return }
        let panel = NSOpenPanel()
        panel.canChooseFiles = true
        panel.canChooseDirectories = false
        panel.allowsMultipleSelection = false
        panel.allowedContentTypes = [.zip]
        guard panel.runModal() == .OK, let url = panel.url else { return }
        do {
            let (entry, outcome, replacedActive) = try WebContentStore.shared.add(url)
            switch outcome {
            case .added: message = ""
            case .alreadyAdded: message = "\(entry.manifest.displayName) is already in the list."
            case .replaced: message = "Replaced the zip named \"\(entry.manifest.name)\" with \(entry.manifest.displayName)."
            }
            messageIsError = false
            if replacedActive { viewModel.reloadGraph() }
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
