package com.jins_jp.meme.academic.ui.main

/**
 * Fixed-size ring buffer of samples for live-graphs.
 * Each entry is a (x, y) pair; we keep x as a logical sample index.
 */
class GraphBuffer(private val capacity: Int) {
    private val xs = LongArray(capacity)
    private val ys = FloatArray(capacity)
    private var head = 0
    private var size = 0

    val length: Int get() = size

    fun add(x: Long, y: Float) {
        val pos = (head + size) % capacity
        if (size < capacity) {
            xs[pos] = x; ys[pos] = y; size++
        } else {
            xs[head] = x; ys[head] = y
            head = (head + 1) % capacity
        }
    }

    fun clear() {
        head = 0; size = 0
    }

    fun snapshotY(): FloatArray {
        if (size == 0) return FloatArray(0)
        val out = FloatArray(size)
        for (i in 0 until size) out[i] = ys[(head + i) % capacity]
        return out
    }
}
