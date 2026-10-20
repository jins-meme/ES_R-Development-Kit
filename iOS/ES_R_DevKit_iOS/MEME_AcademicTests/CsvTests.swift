//
//  CsvTests.swift
//  MEME_AcademicTests
//

import XCTest
@testable import MEME_Academic

@MainActor
final class CsvTests: XCTestCase {

    private var made: [URL] = []

    override func tearDown() async throws {
        for u in made { try? FileManager.default.removeItem(at: u) }
    }

    private func write(rows: Int, compress: Bool) throws -> URL {
        let w = CsvWriter()
        w.start(address: "TEST\(UUID().uuidString.prefix(8))", mode: .full, transMode: MEMEQuality_High,
                accelRange: 1, gyroRange: 2, compress: compress)
        for n in 1...rows {
            w.append(CsvRow(values: Array(repeating: Int64(n), count: 10), num: n,
                            date: Date(timeIntervalSince1970: 1_760_000_000 + Double(n) / 100), isFreeMarking: n == 3))
        }
        XCTAssertEqual(w.rowCount, rows)
        let url = try XCTUnwrap(w.stop())
        made.append(url)
        return url
    }

    func testPlainCsvLayout() throws {
        let url = try write(rows: 150, compress: false)
        XCTAssertEqual(url.pathExtension, "csv")
        let lines = try CsvFile.readText(at: url).split(separator: "\n").map(String.init)
        XCTAssertEqual(lines[0], "// Data mode  : Full")
        XCTAssertEqual(lines[2], "// Acceleration sensor's range  : 4g")
        XCTAssertEqual(lines[3], "// Gyroscope sensor's range  : 1000dps")
        XCTAssertEqual(lines[5], "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V")
        XCTAssertEqual(lines.count, 6 + 150)
        XCTAssertTrue(lines[6].hasPrefix(",1,2025/10/09 08:53:20.0"), lines[6])
        XCTAssertTrue(lines[8].hasPrefix("X,3,"))
    }

    /// gz は 100 行ごとに 1 メンバー。連結したままで全部読める
    func testGzipMembersReadBack() throws {
        let url = try write(rows: 250, compress: true)
        XCTAssertTrue(url.lastPathComponent.hasSuffix(".csv.gz"))
        XCTAssertTrue(Gzip.isGzipped(try Data(contentsOf: url)))
        XCTAssertEqual(try CsvFile.readText(at: url).split(separator: "\n").count, 6 + 250)
    }

    /// ライブのサンプル番号 i はデータ行 i − 1 に書き戻す(先頭パケットは CSV に無い)
    func testArtifactWriteBack() throws {
        let url = try write(rows: 10, compress: true)
        var buf = ArtifactBuffer()
        buf.add(i: 5, text: "blink, left")
        buf.add(i: 0, text: "dropped")
        XCTAssertFalse(buf.add(i: 6, text: "=SUM(A1)"))
        buf.flush(to: url, numbering: .liveSample)
        let lines = try CsvFile.readText(at: url).split(separator: "\n").map(String.init)
        XCTAssertTrue(lines[6 + 4].hasPrefix("blink  left,5,"), lines[6 + 4])
        XCTAssertFalse(lines.contains { $0.hasPrefix("dropped") })
    }
}
