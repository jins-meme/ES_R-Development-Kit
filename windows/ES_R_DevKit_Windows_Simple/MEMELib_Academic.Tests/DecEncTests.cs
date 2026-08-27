using MEMELib_Academic;
using Xunit;

namespace MEMELib_Academic.Tests;

public class DecEncTests
{
    [Fact]
    public void EncodeThenDecode_RestoresOriginal()
    {
        var original = new byte[MemeProtocol.PacketLength];
        for (var i = 0; i < original.Length; i++)
        {
            original[i] = (byte)(i * 7 + 3);
        }

        var buffer = (byte[])original.Clone();
        DecEnc.Encode(buffer);
        Assert.NotEqual(original, buffer);

        DecEnc.Decode(buffer);
        Assert.Equal(original, buffer);
    }

    [Fact]
    public void Encode_LeavesHeaderUntouched()
    {
        var buffer = new byte[MemeProtocol.PacketLength];
        buffer[0] = MemeProtocol.PacketLength;
        buffer[1] = MemeProtocol.AdnStartStopSend;

        DecEnc.Encode(buffer);

        Assert.Equal(MemeProtocol.PacketLength, buffer[0]);
        Assert.Equal(MemeProtocol.AdnStartStopSend, buffer[1]);
    }

    /// <summary>
    /// 鍵はヘッダ 2 byte を除いた 18 byte をちょうど覆う。ここがずれると
    /// パケット末尾が変換されず、端末がコマンドを解釈できなくなる。
    /// </summary>
    [Fact]
    public void KeyCoversPacketBody()
    {
        Assert.Equal(MemeProtocol.PacketLength - 2, DecEnc.KeyLength);
    }

    [Fact]
    public void Encode_MatchesReferenceImplementation()
    {
        // Mac 版 DecEnc.swift の鍵先頭 3 byte (0x39, 0xCC, 0x6D) に対する期待値。
        // enc = (v ^ key[i]) + i
        var buffer = new byte[MemeProtocol.PacketLength];
        buffer[2] = 0x00;
        buffer[3] = 0x00;
        buffer[4] = 0x00;

        DecEnc.Encode(buffer);

        Assert.Equal(0x39, buffer[2]);
        Assert.Equal((byte)(0xCC + 1), buffer[3]);
        Assert.Equal((byte)(0x6D + 2), buffer[4]);
    }

    [Fact]
    public void Encode_ThrowsOnShortBuffer()
    {
        var buffer = new byte[10];
        Assert.Throws<ArgumentException>(() => DecEnc.Encode(buffer));
    }
}
