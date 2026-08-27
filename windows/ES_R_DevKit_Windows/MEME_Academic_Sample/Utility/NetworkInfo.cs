using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MEME_Academic_Sample.Utility;

/// <summary>Mac 版 Common.getIPAddress 相当。TCP 出力の接続先を画面に出すために使う。</summary>
public static class NetworkInfo
{
    /// <summary>稼働中のインターフェイスに割り当てられた IPv4 アドレス。見つからなければ空文字。</summary>
    public static string GetLocalIPv4Address()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                foreach (var address in nic.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(address.Address))
                    {
                        return address.Address.ToString();
                    }
                }
            }
        }
        catch (NetworkInformationException)
        {
            // アダプタ情報が取れない環境では空表示にとどめる。
        }

        return string.Empty;
    }
}
