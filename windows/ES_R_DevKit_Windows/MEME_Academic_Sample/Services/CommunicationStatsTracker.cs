using Timer = System.Threading.Timer;

namespace MEME_Academic_Sample.Services;

/// <summary>
/// パケットカウンタの差分計算と、成功率／通信率の更新を担当するトラッカー。
/// Mac 版 CommunicationStatsTracker の移植。
/// </summary>
/// <remarks>
/// <c>quality</c> は 100Hz なら 1、50Hz なら 2。サンプリング周波数は 100 / quality Hz。
/// </remarks>
public sealed class CommunicationStatsTracker : IDisposable
{
    /// <summary>通信率の集計ウィンドウ。</summary>
    private static readonly TimeSpan CommunicationWindow = TimeSpan.FromSeconds(1);

    private readonly Lock _gate = new();
    private Timer? _communicationTimer;

    private int _prevCount = -1;
    private int _quality = 1;
    private int _dataCount;
    private int _dataCountInWindow;
    private DateTime? _firstDataUtc;

    /// <summary>成功率(%) と表示文字列を通知する。BLE 受信スレッドから呼ばれる。</summary>
    public event Action<double, string>? SuccessRateChanged;

    /// <summary>通信率(%) と表示文字列を通知する。タイマースレッドから呼ばれる。</summary>
    public event Action<double, string>? CommunicationChanged;

    /// <summary>受信カウンタの差分を積算した単調増加の通し番号。CSV の NUM 列に使う。</summary>
    public int TotalCount { get; private set; }

    /// <summary>取りこぼしたと推定されるサンプル数。</summary>
    public int ErrorCount { get; private set; }

    public void Reset()
    {
        lock (_gate)
        {
            _prevCount = -1;
            TotalCount = 0;
            ErrorCount = 0;
            _quality = 1;
            _dataCount = 0;
            _dataCountInWindow = 0;
            _firstDataUtc = null;
        }
    }

    public void StartMeasurement(int quality)
    {
        lock (_gate)
        {
            // 起点は最初のデータ受信時に確定させる(BumpDataCount 参照)。
            _firstDataUtc = null;
            _quality = Math.Max(quality, 1);
            _communicationTimer?.Dispose();
            _communicationTimer = new Timer(_ => TickCommunication(), null, CommunicationWindow, CommunicationWindow);
        }
    }

    public void StopMeasurement()
    {
        lock (_gate)
        {
            _communicationTimer?.Dispose();
            _communicationTimer = null;
        }
    }

    /// <summary>
    /// データ受信時に呼ぶ。デバイスのカウンタ(12bit, 0..4095)は 0 から始まるとは限らないため、
    /// 最初の 1 個は基準の取得だけに使い記録しない(false を返す)。
    /// 2 個目以降は受信カウンタの差分を積算して <see cref="TotalCount"/> を単調増加させる。
    /// </summary>
    public bool RegisterPacket(int count)
    {
        lock (_gate)
        {
            if (_prevCount < 0)
            {
                _prevCount = count;
                return false;
            }

            var diff = _prevCount < count ? count - _prevCount : 0x1000 - _prevCount + count;
            _prevCount = count;

            TotalCount += diff;
            if (diff - 1 > 0)
            {
                ErrorCount += diff - 1;
            }

            return true;
        }
    }

    /// <summary>
    /// 受信完了ごとに呼ぶ。成功率の再計算は約 0.2 秒間隔
    /// (100Hz なら 20 件、50Hz なら 10 件ごと)に集約する。
    /// </summary>
    public void BumpDataCount()
    {
        double rate;
        lock (_gate)
        {
            _firstDataUtc ??= DateTime.UtcNow;
            _dataCount++;
            _dataCountInWindow++;

            var stride = Math.Max(20 / _quality, 1);
            if (_dataCount % stride != 0)
            {
                return;
            }

            var elapsed = (DateTime.UtcNow - _firstDataUtc.Value).TotalSeconds;
            rate = elapsed > 0 ? _dataCount / (elapsed * 100.0 / _quality) * 100.0 : 0;
        }

        var clamped = Math.Min(rate, 100.0);
        SuccessRateChanged?.Invoke(clamped, $"{clamped:0.0}%");
    }

    private void TickCommunication()
    {
        double communication;
        lock (_gate)
        {
            communication = _dataCountInWindow / (CommunicationWindow.TotalSeconds * 100.0 / _quality) * 100.0;
            _dataCountInWindow = 0;
        }

        var clamped = Math.Min(communication, 100.0);
        CommunicationChanged?.Invoke(clamped, $"{clamped:0.0}%");
    }

    public void Dispose() => StopMeasurement();
}
