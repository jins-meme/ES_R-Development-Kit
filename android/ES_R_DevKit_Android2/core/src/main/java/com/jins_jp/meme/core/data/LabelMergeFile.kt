package com.jins_jp.meme.core.data

import android.content.Context
import android.net.Uri
import com.jins.meme.academic.util.LogCat
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.File
import java.io.FileInputStream
import java.io.FileOutputStream
import java.io.OutputStreamWriter
import java.util.zip.GZIPInputStream
import java.util.zip.GZIPOutputStream

private const val TAG = "LabelMergeFile"

/**
 * [labels] を [target] のデータCSV（content:// の URI）の ARTIFACT 列へ統合する（[LabelMerger.merge]）。
 *
 * 追記ではラベル行だけ差し替えられないので全体を書き直すが、**CSV をメモリに載せずに 1 行ずつ流す**。
 * 100Hz の実測は 1 時間で約 28MB のテキストになり、`List<String>` へ読み込むと数時間の計測でヒープを使い切る。
 * いったんキャッシュの一時ファイルへ書き切ってから本体へ流し込むので、途中で失敗しても元のファイルは壊れない。
 * 読んだ形式のまま書き戻す（本体データCSVは設定により .csv.gz か .csv、再生元の過去ファイルは非圧縮のこともある）。
 *
 * **完了まで返らない**（suspend）。計測完了時の共有は書き戻し済みのファイルを渡す必要があり、
 * 投げっぱなしだと共有シートが統合前のCSVを掴む。失敗してもログに残すだけで投げない。
 */
suspend fun mergeLabelsIntoCsv(context: Context, target: Uri, labels: List<LabelMerger.Entry>, byRowIndex: Boolean) {
    if (labels.isEmpty()) return
    val resolver = context.contentResolver
    withContext(Dispatchers.IO) {
        val tmp = runCatching { File.createTempFile("label_merge", ".tmp", context.cacheDir) }
            .getOrNull() ?: return@withContext
        try {
            runCatching {
                val written = resolver.openInputStream(target)?.use { ins ->
                    val decoded = decompressIfGzip(ins)
                    val compress = decoded is GZIPInputStream
                    FileOutputStream(tmp).use { fos ->
                        val sink = if (compress) GZIPOutputStream(fos) else fos
                        // BufferedWriter の close が連鎖して GZIPOutputStream の
                        // finish とトレーラ書き出しまで行う。
                        OutputStreamWriter(sink, Charsets.UTF_8).buffered().use { w ->
                            decoded.bufferedReader(Charsets.UTF_8).use { r ->
                                LabelMerger.merge(r, w, labels, byRowIndex)
                            }
                        }
                    }
                    true
                } ?: false
                // 一時ファイルが完成した時だけ本体を置き換える。
                if (written) {
                    resolver.openOutputStream(target, "wt")?.use { os ->
                        FileInputStream(tmp).use { it.copyTo(os) }
                    }
                }
            }.onFailure { e ->
                LogCat.d(TAG, "label merge failed: $e")
            }
        } finally {
            tmp.delete()
        }
    }
}
