//
//  CacheStoreTests.swift
//  MEME_AcademicTests
//
//  日別キャッシュ(CacheStore)の一覧・読み・書き・名前の検査(webview/BRIDGE.md の Cache files。Android の CacheStoreTest.kt と同じ確かめ)。
//

import XCTest
@testable import MEME_Academic

final class CacheStoreTests: XCTestCase {

    private var root: URL!
    private var store: CacheStore!
    private let dev = "D3FE9978A82E"

    override func setUp() {
        root = FileManager.default.temporaryDirectory.appendingPathComponent("cache-\(UUID().uuidString)", isDirectory: true)
        store = CacheStore(root: root)
    }

    override func tearDown() { try? FileManager.default.removeItem(at: root) }

    func testListOfMissingFolderIsEmpty() {
        let r = store.run(kind: "cacheList", device: dev, name: "", text: "")
        XCTAssertEqual(r["ok"] as? Bool, true)
        XCTAssertEqual((r["files"] as? [Any])?.count, 0)
    }

    func testWriteReadListAndOverwrite() throws {
        var r = store.run(kind: "cacheWrite", device: dev, name: "2026-10-27.json", text: "{\"a\":\"日本語\"}")
        XCTAssertEqual(r["ok"] as? Bool, true)
        XCTAssertEqual(r["size"] as? Int, "{\"a\":\"日本語\"}".utf8.count)
        r = store.run(kind: "cacheWrite", device: dev, name: "2026-10-27.json", text: "{\"b\":2}")     // 置き換え
        XCTAssertEqual(r["ok"] as? Bool, true)
        r = store.run(kind: "cacheRead", device: dev, name: "2026-10-27.json", text: "")
        XCTAssertEqual(r["text"] as? String, "{\"b\":2}")
        // .tmp は残らない・一覧は日付の JSON だけ
        try "x".write(to: root.appendingPathComponent(dev).appendingPathComponent("notes.txt"), atomically: true, encoding: .utf8)
        let names = try FileManager.default.contentsOfDirectory(atPath: root.appendingPathComponent(dev).path)
        XCTAssertFalse(names.contains { $0.hasSuffix(".tmp") })
        r = store.run(kind: "cacheList", device: dev, name: "", text: "")
        let files = try XCTUnwrap(r["files"] as? [[String: Any]])
        XCTAssertEqual(files.map { $0["name"] as? String }, ["2026-10-27.json"])
        XCTAssertEqual(files[0]["size"] as? Int, 7)
        XCTAssertGreaterThan(files[0]["mtime"] as? Double ?? 0, 1.7e12)
    }

    func testErrors() {
        XCTAssertEqual(store.run(kind: "cacheRead", device: dev, name: "2026-01-01.json", text: "")["error"] as? String, "not_found")
        XCTAssertEqual(store.run(kind: "cacheList", device: "d3fe9978a82e", name: "", text: "")["error"] as? String, "bad_name")
        XCTAssertEqual(store.run(kind: "cacheRead", device: "../" + dev, name: "2026-01-01.json", text: "")["error"] as? String, "bad_name")
        XCTAssertEqual(store.run(kind: "cacheWrite", device: dev, name: "../x.json", text: "")["error"] as? String, "bad_name")
        XCTAssertEqual(store.run(kind: "cacheWrite", device: dev, name: "2026-01-01.json.tmp", text: "")["error"] as? String, "bad_name")
        XCTAssertEqual(store.run(kind: "cacheDelete", device: dev, name: "2026-01-01.json", text: "")["error"] as? String, "bad_kind")
        let big = String(repeating: "a", count: CacheStore.maxBytes + 1)
        XCTAssertEqual(store.run(kind: "cacheWrite", device: dev, name: "2026-01-01.json", text: big)["error"] as? String, "too_large")
    }

    func testDeviceNames() {
        XCTAssertEqual(CacheStore.deviceOf(address: "d3:fe:99:78:a8:2e"), "D3FE9978A82E")
        XCTAssertNil(CacheStore.deviceOf(address: ""))
        XCTAssertNil(CacheStore.deviceOf(address: "ESRG2_5"))
        XCTAssertEqual(CacheStore.deviceOf(fileName: "d3fe9978a82e_20261026234146.csv.gz"), "D3FE9978A82E")
        XCTAssertNil(CacheStore.deviceOf(fileName: "ESR_20261026234146.csv.gz"))
    }

    func testAnswerPutsIdFirst() throws {
        let s = CacheStore.answer(["ok": true, "size": 3], idJSON: "7")
        let o = try XCTUnwrap(try JSONSerialization.jsonObject(with: Data(s.utf8)) as? [String: Any])
        XCTAssertEqual(o["id"] as? Int, 7)
        XCTAssertEqual(o["size"] as? Int, 3)
        XCTAssertEqual(CacheStore.answer([:], idJSON: "\"a\""), "{\"id\":\"a\"}")
    }

    func testHandleAnswersOnMain() {
        let done = expectation(description: "answer")
        store.handle(idJSON: "1", kind: "cacheList", device: dev, name: "", text: "") { json in
            XCTAssertTrue(Thread.isMainThread)
            XCTAssertTrue(json.hasPrefix("{\"id\":1,"))
            done.fulfill()
        }
        wait(for: [done], timeout: 5)
    }
}
