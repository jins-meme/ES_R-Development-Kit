package com.jins_jp.meme.core.ble

import com.jins.meme.academic.util.DataEncryption
import com.jins.meme.academic.util.LogCat
import kotlin.random.Random

private const val TAG = "MemeCipher"

/** 変換対象は 20 byte パケットの 2〜19 byte 目。 */
private const val BODY_OFFSET = 2
private const val BODY_LENGTH = 18

/**
 * BLE パケットの暗号化／復号。jar の [DataEncryption] と同じ変換を、次の 2 点を改めて行う。
 *
 * - jar は呼ぶたびに AES で同じ XOR マスクを作り直す。マスクは端末によらない固定値
 *   (jar の setKey は空実装で、MAC アドレスは使われない)なので、1 回だけ求めて使い回す。
 * - jar は入力配列をその場で書き換えて返す。ここでは常に新しい配列を返し、入力は触らない。
 *
 * 変換は `decode: b[i+2] = (b[i+2] - i) xor mask[i]`、`encode: b[i+2] = (b[i+2] xor mask[i]) + i`
 * (i = 0..17)。20 byte 未満のパケットは jar と同じく、存在するバイトまでを変換する。
 * マスクは鍵をソースへ持ち込まないよう、jar の decode に全ゼロを通して逆算する。
 */
object MemeCipher {

    /** 求めたマスク。jar との突き合わせに失敗した時は null で、以後は jar へ委譲する。 */
    private val mask: ByteArray? by lazy {
        deriveMask { DataEncryption.decode(it) }
            ?.takeIf { verify(it) }
            .also { if (it == null) LogCat.d(TAG, "mask cache disabled; falling back to DataEncryption") }
    }

    fun decode(data: ByteArray): ByteArray {
        val m = mask ?: return DataEncryption.decode(data.copyOf())
        return decodeWith(m, data)
    }

    fun encode(data: ByteArray): ByteArray {
        val m = mask ?: return DataEncryption.encode(data.copyOf())
        return encodeWith(m, data)
    }

    /**
     * 全ゼロのパケットを [jarDecode] に通し、出力から XOR マスクを逆算する。
     * 0 byte 目から見た `(0 - i) xor mask[i] = out[i+2]` を mask について解いている。
     */
    internal fun deriveMask(jarDecode: (ByteArray) -> ByteArray): ByteArray? {
        val out = runCatching { jarDecode(ByteArray(BODY_OFFSET + BODY_LENGTH)) }.getOrNull()
            ?: return null
        if (out.size < BODY_OFFSET + BODY_LENGTH) return null
        return ByteArray(BODY_LENGTH) { i -> (out[i + BODY_OFFSET].toInt() xor -i).toByte() }
    }

    internal fun decodeWith(mask: ByteArray, data: ByteArray): ByteArray {
        val out = data.copyOf()
        for (i in 0 until bodyLength(out)) {
            val j = i + BODY_OFFSET
            out[j] = ((out[j] - i) xor mask[i].toInt()).toByte()
        }
        return out
    }

    internal fun encodeWith(mask: ByteArray, data: ByteArray): ByteArray {
        val out = data.copyOf()
        for (i in 0 until bodyLength(out)) {
            val j = i + BODY_OFFSET
            out[j] = ((out[j].toInt() xor mask[i].toInt()) + i).toByte()
        }
        return out
    }

    private fun bodyLength(data: ByteArray) = minOf(BODY_LENGTH, data.size - BODY_OFFSET)

    /**
     * 逆算したマスクで jar と同じ結果になるかを乱数パケットで確かめる。暗号プロバイダが
     * 使えず jar が素通しした場合などはここで不一致になり、jar への委譲に落ちる。
     */
    private fun verify(mask: ByteArray): Boolean = runCatching {
        val sample = Random.nextBytes(BODY_OFFSET + BODY_LENGTH)
        decodeWith(mask, sample).contentEquals(DataEncryption.decode(sample.copyOf())) &&
            encodeWith(mask, sample).contentEquals(DataEncryption.encode(sample.copyOf()))
    }.getOrDefault(false)
}
