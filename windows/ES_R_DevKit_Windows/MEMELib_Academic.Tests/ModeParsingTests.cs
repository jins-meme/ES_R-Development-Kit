using System.Buffers.Binary;
using MEMELib_Academic;
using Xunit;

namespace MEMELib_Academic.Tests;

/// <summary>
/// Standard(0x98) / Quaternion(0x9A) の展開。期待値は Mac 版
/// MEMELib_Academic.swift の dataAnalysis と同じ並びで検算している。
/// </summary>
public class ModeParsingTests
{
    [Fact]
    public void ParseStandardData_ReadsBothEogPairs()
    {
        var packet = BuildPacket(MemeProtocol.AupReportAcademia1, count: 0x0AB, battery: 2);
        WriteInt16(packet, 4, 11);      // AccX
        WriteInt16(packet, 6, -22);     // AccY
        WriteInt16(packet, 8, 33);      // AccZ
        WriteInt16(packet, 10, 1000);   // EogL1
        WriteInt16(packet, 12, -200);   // EogR1
        WriteInt16(packet, 14, 500);    // EogL2
        WriteInt16(packet, 16, 300);    // EogR2

        var data = MemeProtocol.ParseStandardData(packet);

        Assert.Equal(0x0AB, data.Cnt);
        Assert.Equal(2, data.BattLv);
        Assert.Equal(11, data.AccX);
        Assert.Equal(-22, data.AccY);
        Assert.Equal(33, data.AccZ);
        Assert.Equal(1000, data.EogL1);
        Assert.Equal(-200, data.EogR1);
        Assert.Equal(500, data.EogL2);
        Assert.Equal(300, data.EogR2);

        // H = L - R, V = -(L + R) / 2 を組ごとに求める。
        Assert.Equal(1200, data.EogH1);
        Assert.Equal(200, data.EogH2);
        Assert.Equal(-400, data.EogV1);
        Assert.Equal(-400, data.EogV2);
    }

    [Fact]
    public void ParseQuaternionData_ReadsFourSignedInt32()
    {
        var packet = BuildPacket(MemeProtocol.AupReportAcademia3, count: 4095, battery: 5);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(4, 4), 1_000_000);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(8, 4), -1_000_000);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(12, 4), 2_147_483_647);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(16, 4), -2_147_483_648);

        var data = MemeProtocol.ParseQuaternionData(packet);

        Assert.Equal(4095, data.Cnt);
        Assert.Equal(5, data.BattLv);
        Assert.Equal(1_000_000, data.QuaternionW);
        Assert.Equal(-1_000_000, data.QuaternionX);
        Assert.Equal(2_147_483_647, data.QuaternionY);
        Assert.Equal(-2_147_483_648, data.QuaternionZ);
    }

    [Fact]
    public void ParseStandardData_RejectsWrongLengthMarker()
    {
        var packet = BuildPacket(MemeProtocol.AupReportAcademia1, count: 1, battery: 1);
        packet[0] = 0x10;
        Assert.Throws<ArgumentException>(() => MemeProtocol.ParseStandardData(packet));
    }

    private static byte[] BuildPacket(byte type, int count, int battery)
    {
        var packet = new byte[MemeProtocol.PacketLength];
        packet[0] = MemeProtocol.PacketLength;
        packet[1] = type;
        var head = (ushort)((count & 0x0FFF) | ((battery & 0x0F) << 12));
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), head);
        return packet;
    }

    private static void WriteInt16(byte[] packet, int offset, short value) =>
        BinaryPrimitives.WriteInt16LittleEndian(packet.AsSpan(offset, 2), value);
}
