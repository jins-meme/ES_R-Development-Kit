package com.jins_jp.meme.core.chart

/**
 * Fixed-size, zero-filled ring buffer for the live graph.
 * Always holds `capacity` samples. New samples overwrite the oldest slot,
 * producing a right-to-left scroll where the X scale never changes.
 */
class GraphBuffer(private val capacity: Int) {
    private val ys = FloatArray(capacity)
    private var head = 0

    fun add(y: Float) {
        ys[head] = y
        head = (head + 1) % capacity
    }

    fun clear() {
        for (i in ys.indices) ys[i] = 0f
        head = 0
    }

    fun snapshotY(): FloatArray {
        val out = FloatArray(capacity)
        for (i in 0 until capacity) {
            out[i] = ys[(head + i) % capacity]
        }
        return out
    }
}