package com.jins_jp.meme.core.data

import org.junit.Assert.assertEquals
import org.junit.Test

/**
 * [LabelMerger] の検証: タップラベルをデータCSVの ARTIFACT 列へ統合する。
 * 実機計測は NUM(2 列目)、再生はデータ行番号(1 始まり)で対応付ける。
 */
class LabelMergerTest {

    private val header = listOf(
        "// Data mode  : Full",
        "//",
        "//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V",
    )

    private fun row(artifact: String, num: Long) =
        "$artifact,$num,2026/07/13 00:00:00.000,1,2,3,4,5,6,7,8,9,10"

    @Test
    fun mergesByNumIntoArtifactColumn() {
        val lines = header + listOf(row("", 1), row("", 2), row("", 3))
        val out = LabelMerger.merge(
            lines,
            listOf(LabelMerger.Entry(2, "jump")),
            byRowIndex = false,
        )
        assertEquals(header + listOf(row("", 1), row("jump", 2), row("", 3)), out)
    }

    @Test
    fun missingNumFallsForwardToNextRow() {
        // NUM=2 が取りこぼしで欠けていても、直後の行(NUM=4)へ載せて失わない。
        val lines = header + listOf(row("", 1), row("", 4), row("", 5))
        val out = LabelMerger.merge(
            lines,
            listOf(LabelMerger.Entry(2, "walk")),
            byRowIndex = false,
        )
        assertEquals(header + listOf(row("", 1), row("walk", 4), row("", 5)), out)
    }

    @Test
    fun mergesByRowNumberForReplay() {
        // 再生の行番号は 1 始まり: key=1 は 1 行目、key=3 は 3 行目に載る。
        val lines = header + listOf(row("", 100), row("", 101), row("", 102))
        val out = LabelMerger.merge(
            lines,
            listOf(LabelMerger.Entry(1, "a"), LabelMerger.Entry(3, "b")),
            byRowIndex = true,
        )
        assertEquals(header + listOf(row("a", 100), row("", 101), row("b", 102)), out)
    }

    @Test
    fun replayRowNumberMatchesNumColumnOfGaplessCsv() {
        // ダイアログに「マーク NUM=2」と出たラベル(key=2)は、取りこぼしの無い
        // CSV(NUM=1,2,3,…)では NUM=2 の行に載る（1 行後ろへずれない）。
        val lines = header + listOf(row("", 1), row("", 2), row("", 3))
        val out = LabelMerger.merge(
            lines,
            listOf(LabelMerger.Entry(2, "jump")),
            byRowIndex = true,
        )
        assertEquals(header + listOf(row("", 1), row("jump", 2), row("", 3)), out)
    }

    @Test
    fun multipleLabelsOnSameRowAreJoined() {
        val lines = header + listOf(row("", 1), row("", 2))
        val out = LabelMerger.merge(
            lines,
            listOf(LabelMerger.Entry(2, "x"), LabelMerger.Entry(2, "y")),
            byRowIndex = false,
        )
        assertEquals(header + listOf(row("", 1), row("x;y", 2)), out)
    }

    @Test
    fun keepsExistingFreeMarkingWhenNoLabelForRow() {
        val lines = header + listOf(row("X", 1), row("", 2))
        val out = LabelMerger.merge(
            lines,
            listOf(LabelMerger.Entry(2, "sit")),
            byRowIndex = false,
        )
        assertEquals(header + listOf(row("X", 1), row("sit", 2)), out)
    }

    @Test
    fun emptyLabelsReturnLinesUnchanged() {
        val lines = header + listOf(row("", 1))
        assertEquals(lines, LabelMerger.merge(lines, emptyList(), byRowIndex = false))
    }
}
