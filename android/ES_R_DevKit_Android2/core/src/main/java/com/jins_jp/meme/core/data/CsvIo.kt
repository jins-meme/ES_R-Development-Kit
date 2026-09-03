package com.jins_jp.meme.core.data

import java.io.BufferedInputStream
import java.io.InputStream
import java.util.zip.GZIPInputStream

/** 非圧縮CSVの拡張子と MIME。サイドカー(分類・切断ログ)は常にこちら。 */
const val CSV_EXTENSION = ".csv"
const val CSV_MIME = "text/csv"

/** gz 圧縮CSVの拡張子と MIME。 */
const val CSV_GZ_EXTENSION = ".csv.gz"
const val CSV_GZ_MIME = "application/gzip"

/**
 * 本体データCSVのファイル名。[compress] は設定「保存時に gz 圧縮する」(既定 ON)。
 *
 * 圧縮すると 100Hz 全保存(1 か月で 18GB の桁)が実測で **1/2.9** になる
 * (Full/100Hz の 51871 行 4.02MB → 1.39MB)。[CsvWriter] は flush ごとに独立した
 * gzip メンバを追記するので、1 本のストリームにまとめた場合(3.1 倍)より 8% ほど
 * 大きいが、途中で落ちてもそこまで展開できることを優先している。
 */
fun dataFileName(base: String, compress: Boolean): String =
    base + if (compress) CSV_GZ_EXTENSION else CSV_EXTENSION

/** 本体データCSVの MIME。詳細は [dataFileName]。 */
fun dataFileMime(compress: Boolean): String = if (compress) CSV_GZ_MIME else CSV_MIME

private const val GZIP_MAGIC_0 = 0x1F
private const val GZIP_MAGIC_1 = 0x8B

/**
 * gzip なら [GZIPInputStream] で包んで返し、そうでなければ（バッファして）そのまま返す。
 *
 * 判定は**ファイル名でなく先頭 2 バイトのマジック**で行う。再生で開くのはファイル
 * ダイアログが返す content:// URI で、表示名や拡張子が取れるとは限らないため。
 * これで .csv.gz も .csv も、設定を切り替えながら記録した混在も、拡張子を変えられた
 * ファイルも、同じ経路で読める。
 *
 * [CsvWriter] は flush ごとに独立した gzip メンバを 1 つ追記するが、連結された
 * gzip メンバは 1 本のストリームとして読める(RFC 1952)ので、[GZIPInputStream] は
 * そのまま全行を返す（`gunzip` や Python の `gzip` も同じ）。
 */
fun decompressIfGzip(input: InputStream): InputStream {
    val buffered = input as? BufferedInputStream ?: BufferedInputStream(input)
    buffered.mark(2)
    val b0 = buffered.read()
    val b1 = buffered.read()
    buffered.reset()
    return if (b0 == GZIP_MAGIC_0 && b1 == GZIP_MAGIC_1) GZIPInputStream(buffered) else buffered
}
