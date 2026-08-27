using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MEME_Academic_Sample.Services;

/// <summary>
/// 計測データを TCP で外部へ流すサーバ。指定ポートで待ち受け、接続してきた
/// クライアント 1 台へ CSV と同じ書式のヘッダと行を送る。Mac 版 TCPSocket の移植。
/// </summary>
public sealed class TcpOutputServer : IDisposable
{
    private readonly Lock _gate = new();

    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;

    /// <summary>接続してきたクライアントへ最初に送るヘッダ。計測開始時に差し替える。</summary>
    private string _header = string.Empty;

    /// <summary>状態表示用の文字列("Listen" / "Accepted" / "Listen Error" など)。</summary>
    public event Action<string>? StatusChanged;

    public bool IsConnected
    {
        get
        {
            lock (_gate)
            {
                return _client?.Connected == true;
            }
        }
    }

    /// <summary>待ち受けを開始する。既に動いていれば一度止めてから開き直す。</summary>
    public bool Start(string port)
    {
        Stop();

        if (!ushort.TryParse(port, out var portNumber) || portNumber == 0)
        {
            StatusChanged?.Invoke("Status : Invalid port");
            return false;
        }

        try
        {
            var listener = new TcpListener(IPAddress.Any, portNumber);
            listener.Start();
            var cts = new CancellationTokenSource();
            lock (_gate)
            {
                _listener = listener;
                _cts = cts;
            }

            _ = AcceptLoopAsync(listener, cts.Token);
            StatusChanged?.Invoke("Status : Listen");
            return true;
        }
        catch (SocketException e)
        {
            StatusChanged?.Invoke($"Status : Listen Error ({e.SocketErrorCode})");
            return false;
        }
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        lock (_gate)
        {
            cts = _cts;
            _cts = null;
            CloseClientCore();
            _listener?.Stop();
            _listener = null;
        }

        cts?.Cancel();
        cts?.Dispose();
        StatusChanged?.Invoke("Status : ");
    }

    /// <summary>計測開始時にヘッダを設定する。接続済みのクライアントへは即座に送る。</summary>
    public void SetHeader(string header)
    {
        lock (_gate)
        {
            _header = header;
        }

        if (IsConnected)
        {
            Send(header, appendNewLine: false);
        }
    }

    /// <summary>1 行送る。未接続なら何もしない。</summary>
    public void Send(string line, bool appendNewLine = true)
    {
        byte[] payload;
        NetworkStream? stream;
        lock (_gate)
        {
            stream = _stream;
            if (stream is null)
            {
                return;
            }

            payload = Encoding.UTF8.GetBytes(appendNewLine ? line + "\r\n" : line);
        }

        try
        {
            stream.Write(payload, 0, payload.Length);
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException or SocketException)
        {
            lock (_gate)
            {
                CloseClientCore();
            }

            StatusChanged?.Invoke("Status : Disconnected");
        }
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(token);
            }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }

            string header;
            lock (_gate)
            {
                // 同時に 1 台だけ受け付ける。既に繋がっていれば新しい方を切る。
                if (_client?.Connected == true)
                {
                    client.Dispose();
                    continue;
                }

                CloseClientCore();
                _client = client;
                _stream = client.GetStream();
                header = _header;
            }

            StatusChanged?.Invoke("Status : Accepted");
            if (header.Length > 0)
            {
                Send(header, appendNewLine: false);
            }

            _ = WatchForDisconnectAsync(client, token);
        }
    }

    /// <summary>
    /// クライアントからの入力は使わないが、切断を検知するために読み続ける。
    /// 0 バイト読み取りが切断のサイン。
    /// </summary>
    private async Task WatchForDisconnectAsync(TcpClient client, CancellationToken token)
    {
        var buffer = new byte[256];
        try
        {
            var stream = client.GetStream();
            while (!token.IsCancellationRequested)
            {
                if (await stream.ReadAsync(buffer, token) == 0)
                {
                    break;
                }
            }
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException or OperationCanceledException or SocketException)
        {
            // 切断として扱う。
        }

        var wasCurrent = false;
        lock (_gate)
        {
            if (ReferenceEquals(_client, client))
            {
                CloseClientCore();
                wasCurrent = true;
            }
        }

        if (wasCurrent)
        {
            StatusChanged?.Invoke("Status : Listen");
        }
    }

    private void CloseClientCore()
    {
        _stream?.Dispose();
        _stream = null;
        _client?.Dispose();
        _client = null;
    }

    public void Dispose() => Stop();
}
