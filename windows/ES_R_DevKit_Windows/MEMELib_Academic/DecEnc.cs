namespace MEMELib_Academic;

/// <summary>
/// ES_R の 20 byte パケットに掛かっている簡易難読化。先頭 2 byte
/// (長さ・オペコード)は素通しで、残り 18 byte を固定鍵で変換する。
/// Mac 版 <c>DecEnc.swift</c> / Android 版 <c>DataEncryption</c> と同じアルゴリズム。
/// </summary>
public static class DecEnc
{
    private static readonly byte[] Key =
    [
        0x39, 0xCC, 0x6D, 0xAB, 0x9E, 0x07, 0x1A, 0xDE, 0x67,
        0x49, 0x71, 0x9A, 0x5B, 0x69, 0x0F, 0x17, 0xC9, 0xB1,
    ];

    /// <summary>鍵長。ヘッダ 2 byte を除いたパケット本体の長さと一致する。</summary>
    public static int KeyLength => Key.Length;

    /// <summary>送信前の変換。buf は 20 byte 必要。</summary>
    public static void Encode(Span<byte> buf)
    {
        if (buf.Length < 2 + Key.Length)
        {
            throw new ArgumentException($"buffer must be at least {2 + Key.Length} bytes", nameof(buf));
        }

        for (int i = 0; i < Key.Length; i++)
        {
            buf[2 + i] = unchecked((byte)((buf[2 + i] ^ Key[i]) + i));
        }
    }

    /// <summary>受信後の復号。<see cref="Encode"/> の逆変換。</summary>
    public static void Decode(Span<byte> buf)
    {
        if (buf.Length < 2 + Key.Length)
        {
            throw new ArgumentException($"buffer must be at least {2 + Key.Length} bytes", nameof(buf));
        }

        for (int i = 0; i < Key.Length; i++)
        {
            buf[2 + i] = unchecked((byte)((byte)(buf[2 + i] - i) ^ Key[i]));
        }
    }
}
