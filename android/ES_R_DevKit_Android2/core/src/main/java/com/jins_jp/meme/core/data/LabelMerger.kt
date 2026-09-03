package com.jins_jp.meme.core.data

import java.io.Reader
import java.io.Writer

/**
 * チャートタップで付けたラベルを、計測/再生停止時にデータCSVの ARTIFACT 列へ
 * 統合する。CSV の行構造(コメント行・列数)には触れず、対象データ行の先頭列
 * だけを置き換える。
 *
 * 入出力とも 1 行ずつ流す（[merge] のストリーム版）。100Hz の長時間計測は
 * 数百MBのテキストになるため、全行を `List<String>` に載せると OOM する。
 */
object LabelMerger {

    /**
     * ラベル 1 件。[key] は実機計測ではサンプル通し番号(NUM 列の値)、CSV 再生では
     * ソースCSVのデータ行番号(1 始まり)。再生はシークで受信累計とソース位置が
     * ずれるため NUM 列ではなく行番号で対応付けるが、1 始まりに揃えることで
     * 取りこぼしの無いCSV(NUM が 1,2,3,…)ではダイアログに表示した NUM と
     * ラベルが載る行の NUM 列が一致する。
     */
    data class Entry(val key: Long, val text: String)

    /** 行末は CSV 本体と同じ CRLF で書き出す。 */
    private const val LINE_SEPARATOR = "\r\n"

    /**
     * [reader] を 1 行ずつ読み、ARTIFACT 列へラベルを載せて [writer] へ書く。
     * メモリに残るのは [labels] とその時点の 1 行だけ。対応付けの規則は
     * [merge] のリスト版と同じ（実装も共有している）。
     */
    fun merge(reader: Reader, writer: Writer, labels: List<Entry>, byRowIndex: Boolean) {
        val state = MergeState(labels, byRowIndex)
        reader.forEachLine { line ->
            writer.write(state.apply(line))
            writer.write(LINE_SEPARATOR)
        }
    }

    /**
     * [lines](データCSVの全行)のうち、各ラベルの [Entry.key] 以上で最初のデータ行の
     * ARTIFACT 列(先頭列)へラベル文字列を書き込んだ結果を返す。key がぴったりの行が
     * ない場合(パケット取りこぼしで NUM が飛んだ等)も直後の行に載せて失わない。
     * 同じ行に複数のラベルが重なったときは ";" で連結する。
     *
     * 全行をメモリに載せるので、実運用の書き戻しではストリーム版を使うこと。
     * こちらは短い入力（テスト）向けに残している。
     */
    fun merge(lines: List<String>, labels: List<Entry>, byRowIndex: Boolean): List<String> {
        if (labels.isEmpty()) return lines
        val state = MergeState(labels, byRowIndex)
        return lines.map { state.apply(it) }
    }

    /**
     * 1 行ずつ受け取ってラベルを載せる本体。行番号と「次に載せるラベル」の位置を
     * 持ち越すため、ストリーム版・リスト版で同じインスタンスを使い回す。
     */
    private class MergeState(labels: List<Entry>, private val byRowIndex: Boolean) {
        private val sorted = labels.sortedBy { it.key }
        private var next = 0
        // データ行番号は 1 始まり（[Entry.key] の再生時の座標系と同じ）。
        private var rowNumber = 1L

        fun apply(line: String): String {
            val trimmed = line.trim()
            val comma = line.indexOf(',')
            if (trimmed.isEmpty() || trimmed.startsWith("//") || comma < 0 || next >= sorted.size) {
                return line
            }
            val key: Long
            if (byRowIndex) {
                key = rowNumber
            } else {
                // データ行は ARTIFACT,NUM,DATE,... — NUM は 2 列目。
                val num = trimmed.split(',').getOrNull(1)?.trim()?.toLongOrNull()
                    ?: return line
                key = num
            }
            rowNumber++
            val texts = ArrayList<String>(1)
            while (next < sorted.size && sorted[next].key <= key) {
                texts.add(sorted[next].text)
                next++
            }
            return if (texts.isEmpty()) line else texts.joinToString(";") + line.substring(comma)
        }
    }
}
