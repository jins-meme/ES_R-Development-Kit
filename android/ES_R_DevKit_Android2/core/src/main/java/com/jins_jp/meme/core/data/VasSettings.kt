package com.jins_jp.meme.core.data

/** VAS の実施間隔（分）の既定値・下限・上限。 */
const val VAS_INTERVAL_DEFAULT_MIN = 30
const val VAS_INTERVAL_MIN_MIN = 1
const val VAS_INTERVAL_MAX_MIN = 1440

/** VAS の値の範囲と、入力ポップアップのスライダーの初期値。 */
const val VAS_VALUE_MIN = 0
const val VAS_VALUE_MAX = 100
const val VAS_VALUE_DEFAULT = 50

/**
 * VAS 1 項目。[memo] は入力ポップアップで項目の横に出すだけのメモで、アプリには残すが CSV には書かない
 * （CSV に残るのは項目の番号と値だけ。[formatVasArtifact]）。
 */
data class VasItem(val memo: String = "")

/**
 * 設定「Record VAS」。ON のとき、計測開始の直後に 1 回目、その後 [intervalMinutes] 分ごとに VAS の入力を求め、
 * 全項目の値を ARTIFACT 列へ "vas1:50;vas2:30" の形で残す。間隔は全項目共通。
 */
data class VasSettings(
    val enabled: Boolean = false,
    val intervalMinutes: Int = VAS_INTERVAL_DEFAULT_MIN,
    val items: List<VasItem> = listOf(VasItem()),
) {
    val intervalMs: Long get() = intervalMinutes * 60_000L

    /** 計測中に VAS を求めるか（ON で、項目が 1 つ以上あるとき） */
    val active: Boolean get() = enabled && items.isNotEmpty()
}

/**
 * 設定モーダルの間隔の入力を検査する。正の整数（数字だけ。符号・小数点・空白・全角数字は不可）で
 * [VAS_INTERVAL_MIN_MIN]〜[VAS_INTERVAL_MAX_MIN] のときだけ値を返し、それ以外は null。
 */
fun parseVasInterval(text: String): Int? {
    if (text.isEmpty() || text.length > 4 || !text.all { it in '0'..'9' }) return null
    val v = text.toIntOrNull() ?: return null
    return if (v in VAS_INTERVAL_MIN_MIN..VAS_INTERVAL_MAX_MIN) v else null
}

/**
 * ARTIFACT 列へ書く VAS の文字列。項目は 1 始まりの番号で、"vas1:50;vas2:30;vas3:72"。
 * 項目の区切りは ";"（位置などほかのラベルと同じ行に重なったときの区切りと揃える。"_" は値の中の配列に使う）。
 * CSV の区切り(,)や改行・数式の書き出し(= + - @)を含まないので、そのまま列へ入れてよい。
 * 値は [VAS_VALUE_MIN]〜[VAS_VALUE_MAX] に丸める。
 */
fun formatVasArtifact(values: List<Int>): String =
    values.mapIndexed { i, v -> "vas${i + 1}:${v.coerceIn(VAS_VALUE_MIN, VAS_VALUE_MAX)}" }.joinToString(";")
