package com.jins_jp.meme.core.data

/**
 * チャートタップで付けたラベルを、計測/再生停止時にデータCSVの ARTIFACT 列へ
 * 統合する。CSV の行構造(コメント行・列数)には触れず、対象データ行の先頭列
 * だけを置き換える。
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

    /**
     * [lines](データCSVの全行)のうち、各ラベルの [Entry.key] 以上で最初のデータ行の
     * ARTIFACT 列(先頭列)へラベル文字列を書き込んだ結果を返す。key がぴったりの行が
     * ない場合(パケット取りこぼしで NUM が飛んだ等)も直後の行に載せて失わない。
     * 同じ行に複数のラベルが重なったときは ";" で連結する。
     */
    fun merge(lines: List<String>, labels: List<Entry>, byRowIndex: Boolean): List<String> {
        if (labels.isEmpty()) return lines
        val sorted = labels.sortedBy { it.key }
        var next = 0
        // データ行番号は 1 始まり（[Entry.key] の再生時の座標系と同じ）。
        var rowNumber = 1L
        val out = ArrayList<String>(lines.size)
        for (line in lines) {
            val trimmed = line.trim()
            val comma = line.indexOf(',')
            if (trimmed.isEmpty() || trimmed.startsWith("//") || comma < 0 || next >= sorted.size) {
                out.add(line)
                continue
            }
            val key: Long
            if (byRowIndex) {
                key = rowNumber
            } else {
                // データ行は ARTIFACT,NUM,DATE,... — NUM は 2 列目。
                val num = trimmed.split(',').getOrNull(1)?.trim()?.toLongOrNull()
                if (num == null) { out.add(line); continue }
                key = num
            }
            rowNumber++
            val texts = ArrayList<String>(1)
            while (next < sorted.size && sorted[next].key <= key) {
                texts.add(sorted[next].text)
                next++
            }
            if (texts.isEmpty()) {
                out.add(line)
            } else {
                out.add(texts.joinToString(";") + line.substring(comma))
            }
        }
        return out
    }
}
