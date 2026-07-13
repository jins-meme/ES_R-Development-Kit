package com.jins_jp.meme.core.plugin

import com.jins_jp.meme.core.data.CsvWriter
import com.jins_jp.meme.core.data.MeasurementSettings

/**
 * 計測パイプラインへの汎用拡張点。アプリ固有の信号処理（検出器・推定器など）を
 * core の MainViewModel へ差し込むためのフック集で、core 自体はプラグインを
 * 一切登録しない（空レジストリ）。全メソッドは no-op 既定実装を持ち、必要な
 * フックだけを実装すればよい。
 *
 * データの流れ:
 *  - [onSample] は受信した全サンプル（グラフ間引き前）ごとに呼ばれる。
 *  - [onPlotPoint] は EOG プロット点がグラフへ発行された直後（間引き後）に呼ばれ、
 *    プロット通し番号とサンプル位置の対応付けに使える。
 *  - プラグインからのグラフ描画は [onAttached] で渡されるエミッタへ任意の
 *    ペイロードを流す。core は GraphEvent.Custom として同一ストリームに載せる
 *    だけで、ペイロードの型はアプリ側の知識。
 */
interface AlgoPlugin {
    /** ViewModel 生成時に一度だけ呼ばれる。[emitGraph] は GraphEvent.Custom の発行口。 */
    fun onAttached(emitGraph: (Any) -> Unit) {}

    /**
     * 計測開始時に呼ばれる（グラフ Reset 発行済み・計測開始コマンド送信前）。
     * 実機計測では csv.start() 済み。mock 再生では本体 CSV を開かないので、
     * サイドカーが必要なプラグインはここで [CsvWriter.startClassificationOnly] を呼ぶ。
     */
    fun onMeasurementStart(
        settings: MeasurementSettings,
        csv: CsvWriter,
        address: String,
        mockEnabled: Boolean,
    ) {}

    /** 計測停止時、csv.stop() の直前に呼ばれる（未確定データのフラッシュ用）。 */
    fun onMeasurementStop(csv: CsvWriter) {}

    /** 切断検知時、csv.stop() の直前に呼ばれる。計測中の予期しない切断でも呼ばれる。 */
    fun onDisconnected(csv: CsvWriter) {}

    /**
     * 受信サンプルごと（間引き前・全サンプル）に呼ばれる。[values] はパケットの生値、
     * [totalCount] は単調増加のサンプル通し番号（NUM）、[timeMs] はサンプルの GMT 時刻。
     */
    fun onSample(
        packetType: Byte,
        values: IntArray,
        totalCount: Long,
        timeMs: Long,
        csv: CsvWriter,
    ) {}

    /**
     * EOG プロット点の発行直後（間引き後）に呼ばれる。[plotIndex] はプロット点の
     * 通し番号（1 始まり）、[graphSkip] は間引き率（プロット 1 点あたりのサンプル数）。
     */
    fun onPlotPoint(packetType: Byte, values: IntArray, plotIndex: Long, graphSkip: Long) {}
}
