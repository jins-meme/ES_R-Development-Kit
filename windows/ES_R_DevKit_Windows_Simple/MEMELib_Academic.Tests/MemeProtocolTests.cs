using System.Buffers.Binary;
using MEMELib_Academic;
using Xunit;

namespace MEMELib_Academic.Tests;

public class MemeProtocolTests
{
    [Fact]
    public void StartStop_MatchesAndroidMemeCommands()
    {
        var start = MemeProtocol.StartStop(true);
        Assert.Equal(MemeProtocol.PacketLength, start.Length);
        Assert.Equal(MemeProtocol.PacketLength, start[0]);
        Assert.Equal(MemeProtocol.AdnStartStopSend, start[1]);
        Assert.Equal(0x01, start[2]);

        var stop = MemeProtocol.StartStop(false);
        Assert.Equal(0x00, stop[2]);
    }

    /// <summary>
    /// mode は byte4、quality は byte5。Mac 版 memeAdnSetMode と同じ配置。
    /// Full = 2 / High = 1 という値そのものが端末仕様。
    /// </summary>
    [Fact]
    public void SetMode_PlacesModeAndQuality()
    {
        var command = MemeProtocol.SetMode(MEMEMode.Full, MEMEQuality.High);

        Assert.Equal(MemeProtocol.AdnSetMode, command[1]);
        Assert.Equal(2, command[4]);
        Assert.Equal(1, command[5]);
    }

    [Fact]
    public void Set6AxisParams_PlacesRanges()
    {
        var command = MemeProtocol.Set6AxisParams(MEMEAccelRange.Range8G, MEMEGyroRange.Range2000dps);

        Assert.Equal(MemeProtocol.AdnSet6AxisParams, command[1]);
        Assert.Equal(2, command[2]);
        Assert.Equal(3, command[3]);
    }

    [Fact]
    public void ParseFullData_ReadsCounterAndBatteryFromPackedHeader()
    {
        // cnt は 12bit、battLv は上位 4bit。cnt = 0x123, battLv = 4。
        var packet = BuildFullPacket(count: 0x123, battery: 4);

        var data = MemeProtocol.ParseFullData(packet);

        Assert.Equal(0x123, data.Cnt);
        Assert.Equal(4, data.BattLv);
    }

    [Fact]
    public void ParseFullData_ReadsSignedLittleEndianSensorValues()
    {
        var packet = BuildFullPacket(count: 1, battery: 3);
        WriteInt16(packet, 4, 100);      // AccX
        WriteInt16(packet, 6, -200);     // AccY
        WriteInt16(packet, 8, 300);      // AccZ
        WriteInt16(packet, 10, -400);    // GyroX
        WriteInt16(packet, 12, 500);     // GyroY
        WriteInt16(packet, 14, -600);    // GyroZ
        WriteInt16(packet, 16, 1000);    // EogL
        WriteInt16(packet, 18, -300);    // EogR

        var data = MemeProtocol.ParseFullData(packet);

        Assert.Equal(100, data.AccX);
        Assert.Equal(-200, data.AccY);
        Assert.Equal(300, data.AccZ);
        Assert.Equal(-400, data.GyroX);
        Assert.Equal(500, data.GyroY);
        Assert.Equal(-600, data.GyroZ);
        Assert.Equal(1000, data.EogL);
        Assert.Equal(-300, data.EogR);

        // EogH = EogL - EogR, EogV = -(EogL + EogR) / 2
        Assert.Equal(1300, data.EogH);
        Assert.Equal(-350, data.EogV);
    }

    [Fact]
    public void IsValidPacket_RejectsWrongLengthMarker()
    {
        var packet = new byte[MemeProtocol.PacketLength];
        packet[0] = 0x10;
        Assert.False(MemeProtocol.IsValidPacket(packet));

        packet[0] = MemeProtocol.PacketLength;
        Assert.True(MemeProtocol.IsValidPacket(packet));
    }

    [Fact]
    public void ParseFirmwareVersion_ReadsBytesInReverseOrder()
    {
        var packet = new byte[MemeProtocol.PacketLength];
        packet[0] = MemeProtocol.PacketLength;
        packet[1] = MemeProtocol.AupReportDevInfo;
        packet[4] = 3; // revision
        packet[5] = 2; // minor
        packet[6] = 1; // major

        var version = MemeProtocol.ParseFirmwareVersion(packet);

        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Build);
    }

    [Theory]
    [InlineData("ESRG2_0", true)]
    [InlineData("ESRG2_5", true)]
    [InlineData("ESRG2_6", false)]
    [InlineData("JINSG2_0", false)]
    [InlineData("", false)]
    public void DeviceNameRegex_MatchesEsRNamesOnly(string name, bool expected)
    {
        Assert.Equal(expected, MemeProtocol.DeviceNameRegex.IsMatch(name));
    }

    private static byte[] BuildFullPacket(int count, int battery)
    {
        var packet = new byte[MemeProtocol.PacketLength];
        packet[0] = MemeProtocol.PacketLength;
        packet[1] = MemeProtocol.AupReportAcademia2;

        var head = (ushort)((count & 0x0FFF) | ((battery & 0x0F) << 12));
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), head);
        return packet;
    }

    private static void WriteInt16(byte[] packet, int offset, short value) =>
        BinaryPrimitives.WriteInt16LittleEndian(packet.AsSpan(offset, 2), value);
}
