package com.jins_jp.meme.core.ble

import org.junit.Assert.assertArrayEquals
import org.junit.Assert.assertNull
import org.junit.Test
import kotlin.random.Random

/**
 * [MemeCipher] の検証。JVM には jar が使う AES/CBC/PKCS7Padding のプロバイダが無いため、
 * jar の decode/encode をバイトコードどおりに書き写した参照実装と、任意のマスクで突き合わせる。
 */
class MemeCipherTest {

    private val mask = Random(42).nextBytes(18)

    /** jar の DataEncryption.decode と同じ処理(入力を書き換え、短いパケットは途中まで)。 */
    private fun jarDecode(data: ByteArray): ByteArray {
        try {
            for (i in 0 until 18) {
                data[i + 2] = ((data[i + 2] - i) xor mask[i].toInt()).toByte()
            }
        } catch (_: IndexOutOfBoundsException) {
        }
        return data
    }

    private fun jarEncode(data: ByteArray): ByteArray {
        try {
            for (i in 0 until 18) {
                data[i + 2] = ((data[i + 2].toInt() xor mask[i].toInt()) + i).toByte()
            }
        } catch (_: IndexOutOfBoundsException) {
        }
        return data
    }

    @Test
    fun deriveMaskRecoversJarMask() {
        assertArrayEquals(mask, MemeCipher.deriveMask(::jarDecode))
    }

    @Test
    fun deriveMaskFailsWhenJarThrows() {
        assertNull(MemeCipher.deriveMask { error("no provider") })
    }

    @Test
    fun matchesJarForAllLengths() {
        val rnd = Random(7)
        for (len in 0..24) {
            repeat(50) {
                val input = rnd.nextBytes(len)
                assertArrayEquals(jarDecode(input.copyOf()), MemeCipher.decodeWith(mask, input))
                assertArrayEquals(jarEncode(input.copyOf()), MemeCipher.encodeWith(mask, input))
            }
        }
    }

    @Test
    fun doesNotMutateInput() {
        val input = Random(1).nextBytes(20)
        val before = input.copyOf()
        MemeCipher.decodeWith(mask, input)
        MemeCipher.encodeWith(mask, input)
        assertArrayEquals(before, input)
    }

    @Test
    fun encodeThenDecodeRoundTrips() {
        val input = Random(2).nextBytes(20)
        assertArrayEquals(input, MemeCipher.decodeWith(mask, MemeCipher.encodeWith(mask, input)))
    }
}
