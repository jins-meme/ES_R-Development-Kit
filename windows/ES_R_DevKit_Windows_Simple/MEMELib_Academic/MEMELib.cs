using System.Reflection;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace MEMELib_Academic;

public delegate void memePeripheralFoundDelegate(object sender, MEMEStatus result, MEMEDevice? device);

public delegate void memePeripheralConnectedDelegate(object sender, MEMEStatus result);

public delegate void memePeripheralDisconnectedDelegate(object sender, MEMEStatus result);

public delegate void memeAcademicFullDataReceivedDelegate(object sender, AcademicFullData fullData);

/// <summary>
/// JINS MEME ES_R を Windows 本体の BLE Central として直接扱うライブラリ。
/// 旧 SDK が必要としていた USB ドングル(仮想 COM ポート)は使わない。
/// 公開 API は旧 <c>MEMELib_Academic.dll</c> の形をおおむね踏襲している。
/// </summary>
public sealed class MEMELib : IDisposable
{
    private static readonly TimeSpan ScanTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);

    public event memePeripheralFoundDelegate? memePeripheralFound;
    public event memePeripheralConnectedDelegate? memePeripheralConnected;
    public event memePeripheralDisconnectedDelegate? memePeripheralDisconnected;
    public event memeAcademicFullDataReceivedDelegate? memeAcademicFullDataReceived;

    private readonly Lock _gate = new();
    private readonly Dictionary<ulong, MEMEDevice> _found = [];
    private readonly CsvFileWriter _csv = new();

    private BluetoothLEAdvertisementWatcher? _watcher;
    private Timer? _scanTimer;
    private Timer? _connectTimer;

    private BluetoothLEDevice? _device;
    private GattDeviceService? _service;
    private GattSession? _session;
    private GattCharacteristic? _rx;
    private GattCharacteristic? _tx;
    private BluetoothLEPreferredConnectionParametersRequest? _connectionParameters;

    private bool _scanning;
    private bool _connected;
    private bool _measuring;
    private bool _disposed;

    // 送信の順序を保証するためのチェーン。setAccelRange → startDataReport の
    // 順序が入れ替わると端末側の設定が反映されないため、直列化する。
    private Task _sendChain = Task.CompletedTask;

    private Version _fwVersion = new(0, 0, 0);
    private MEMEMode _mode = MEMEMode.Full;
    private MEMEQuality _quality = MEMEQuality.High;
    private MEMEAccelRange _accelRange = MEMEAccelRange.Range2G;
    private MEMEGyroRange _gyroRange = MEMEGyroRange.Range250dps;

    #region Scan

    /// <summary>
    /// ES_R のスキャンを開始する。見つかるたびに <see cref="memePeripheralFound"/> が
    /// MEMELIB_OK で、タイムアウト時は MEMELIB_TIMEOUT で呼ばれる。
    /// </summary>
    public MEMEStatus startScanningPeripherals()
    {
        lock (_gate)
        {
            if (_connected)
            {
                return MEMEStatus.MEMELIB_NG;
            }

            if (_scanning)
            {
                return MEMEStatus.MEMELIB_OK;
            }

            _found.Clear();

            // 広告フィルタは掛けない。サービス UUID を広告に載せない個体を
            // 名前でも拾えるようにするため、判定はコールバック側で行う。
            _watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active,
            };
            _watcher.Received += OnAdvertisementReceived;

            try
            {
                _watcher.Start();
            }
            catch (Exception)
            {
                // Bluetooth が OFF、またはアダプタが BLE 非対応。
                _watcher.Received -= OnAdvertisementReceived;
                _watcher = null;
                return MEMEStatus.MEMELIB_NG;
            }

            _scanning = true;
            _scanTimer = new Timer(_ => OnScanTimeout(), null, ScanTimeout, Timeout.InfiniteTimeSpan);
        }

        // ペアリング済みで広告を出していない端末も候補に含める。
        _ = ReportPairedDevicesAsync();
        return MEMEStatus.MEMELIB_OK;
    }

    public MEMEStatus stopScanningPeripherals()
    {
        lock (_gate)
        {
            if (!_scanning)
            {
                return MEMEStatus.MEMELIB_NG;
            }

            StopScanCore();
            return MEMEStatus.MEMELIB_OK;
        }
    }

    private void StopScanCore()
    {
        _scanTimer?.Dispose();
        _scanTimer = null;

        if (_watcher is not null)
        {
            _watcher.Received -= OnAdvertisementReceived;
            try
            {
                _watcher.Stop();
            }
            catch (Exception)
            {
                // 既に停止済み。
            }

            _watcher = null;
        }

        _scanning = false;
    }

    private void OnScanTimeout()
    {
        lock (_gate)
        {
            StopScanCore();
        }

        memePeripheralFound?.Invoke(this, MEMEStatus.MEMELIB_TIMEOUT, null);
    }

    private void OnAdvertisementReceived(
        BluetoothLEAdvertisementWatcher sender,
        BluetoothLEAdvertisementReceivedEventArgs args)
    {
        var name = args.Advertisement.LocalName ?? string.Empty;
        var isMeme = args.Advertisement.ServiceUuids.Contains(MemeProtocol.ServiceUuid)
                     || MemeProtocol.DeviceNameRegex.IsMatch(name);
        if (!isMeme)
        {
            return;
        }

        ReportFound(args.BluetoothAddress, name);
    }

    private async Task ReportPairedDevicesAsync()
    {
        try
        {
            var selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
            var paired = await DeviceInformation.FindAllAsync(selector);
            foreach (var info in paired)
            {
                if (!MemeProtocol.DeviceNameRegex.IsMatch(info.Name))
                {
                    continue;
                }

                using var device = await BluetoothLEDevice.FromIdAsync(info.Id);
                if (device is not null)
                {
                    ReportFound(device.BluetoothAddress, device.Name);
                }
            }
        }
        catch (Exception)
        {
            // ペアリング済み一覧が引けなくてもスキャン自体は継続する。
        }
    }

    private void ReportFound(ulong address, string name)
    {
        MEMEDevice device;
        lock (_gate)
        {
            if (!_scanning || _found.ContainsKey(address))
            {
                return;
            }

            device = new MEMEDevice(address, name);
            _found[address] = device;
        }

        memePeripheralFound?.Invoke(this, MEMEStatus.MEMELIB_OK, device);
    }

    #endregion

    #region Connect

    /// <summary>
    /// 指定の ES_R へ接続する。GATT の探索と初期問い合わせ(0xA1 → 0xA3 → 0xA9)まで
    /// 済んだ時点で <see cref="memePeripheralConnected"/> が MEMELIB_OK で呼ばれる。
    /// </summary>
    public MEMEStatus connectPeripheral(MEMEDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        lock (_gate)
        {
            if (_connected)
            {
                return MEMEStatus.MEMELIB_NG;
            }

            StopScanCore();
            _connectTimer = new Timer(_ => OnConnectTimeout(), null, ConnectTimeout, Timeout.InfiniteTimeSpan);
        }

        _ = ConnectAsync(device);
        return MEMEStatus.MEMELIB_OK;
    }

    /// <summary>アドレス文字列("28A183055C47" 形式)から接続する。</summary>
    public MEMEStatus connectPeripheral(string deviceAddress)
    {
        if (!ulong.TryParse(deviceAddress, System.Globalization.NumberStyles.HexNumber, null, out var address))
        {
            return MEMEStatus.MEMELIB_NG;
        }

        return connectPeripheral(new MEMEDevice(address, string.Empty));
    }

    private async Task ConnectAsync(MEMEDevice target)
    {
        try
        {
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(target.BluetoothAddress);
            if (device is null)
            {
                FailConnect();
                return;
            }

            _device = device;
            device.ConnectionStatusChanged += OnConnectionStatusChanged;

            // 初回は必ず端末に問い合わせる。Windows のキャッシュを使うと、
            // ファーム更新後などに古いハンドルを掴んで通信できないことがある。
            var services = await device.GetGattServicesForUuidAsync(
                MemeProtocol.ServiceUuid, BluetoothCacheMode.Uncached);
            if (services.Status != GattCommunicationStatus.Success || services.Services.Count == 0)
            {
                FailConnect();
                return;
            }

            _service = services.Services[0];

            // GattSession を保持している間だけ Windows は接続を維持する。
            _session = await GattSession.FromDeviceIdAsync(device.BluetoothDeviceId);
            _session.MaintainConnection = true;

            // 100Hz の通知を取りこぼさないよう接続間隔を詰める(Windows 11 以降)。
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                _connectionParameters = device.RequestPreferredConnectionParameters(
                    BluetoothLEPreferredConnectionParameters.ThroughputOptimized);
            }

            var rx = await _service.GetCharacteristicsForUuidAsync(
                MemeProtocol.RxCharacteristicUuid, BluetoothCacheMode.Uncached);
            var tx = await _service.GetCharacteristicsForUuidAsync(
                MemeProtocol.TxCharacteristicUuid, BluetoothCacheMode.Uncached);
            if (rx.Status != GattCommunicationStatus.Success || rx.Characteristics.Count == 0 ||
                tx.Status != GattCommunicationStatus.Success || tx.Characteristics.Count == 0)
            {
                FailConnect();
                return;
            }

            _rx = rx.Characteristics[0];
            _tx = tx.Characteristics[0];
            _rx.ValueChanged += OnValueChanged;

            var cccd = await _rx.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (cccd != GattCommunicationStatus.Success)
            {
                FailConnect();
                return;
            }

            _connected = true;

            // ここから 0x81 → 0x83 → 0x89 と応答が返り、0x89 で接続完了を通知する。
            Send(MemeProtocol.GetDeviceInfo());
        }
        catch (Exception)
        {
            FailConnect();
        }
    }

    private void FailConnect()
    {
        Teardown();
        memePeripheralConnected?.Invoke(this, MEMEStatus.MEMELIB_NG);
    }

    private void OnConnectTimeout()
    {
        // 接続そのものが張れなかったか、ハンドシェイク(0xA1 → 0xA9)が返らなかった。
        Teardown();
        memePeripheralConnected?.Invoke(this, MEMEStatus.MEMELIB_TIMEOUT);
    }

    public MEMEStatus disconnectPeripheral()
    {
        if (!_connected)
        {
            return MEMEStatus.MEMELIB_NG;
        }

        Teardown();
        memePeripheralDisconnected?.Invoke(this, MEMEStatus.MEMELIB_OK);
        return MEMEStatus.MEMELIB_OK;
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (sender.ConnectionStatus != BluetoothConnectionStatus.Disconnected || !_connected)
        {
            return;
        }

        Teardown();
        memePeripheralDisconnected?.Invoke(this, MEMEStatus.MEMELIB_NG);
    }

    private void Teardown()
    {
        lock (_gate)
        {
            _connectTimer?.Dispose();
            _connectTimer = null;

            if (_rx is not null)
            {
                _rx.ValueChanged -= OnValueChanged;
                _rx = null;
            }

            _tx = null;

            _connectionParameters?.Dispose();
            _connectionParameters = null;

            if (_session is not null)
            {
                _session.MaintainConnection = false;
                _session.Dispose();
                _session = null;
            }

            _service?.Dispose();
            _service = null;

            if (_device is not null)
            {
                _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _device.Dispose();
                _device = null;
            }

            _connected = false;
            _measuring = false;
        }

        _csv.Close();
    }

    #endregion

    #region Send / Receive

    private void Send(byte[] plain)
    {
        lock (_gate)
        {
            _sendChain = _sendChain.ContinueWith(
                _ => SendAsync(plain), TaskScheduler.Default).Unwrap();
        }
    }

    private async Task SendAsync(byte[] plain)
    {
        var characteristic = _tx;
        if (characteristic is null)
        {
            return;
        }

        try
        {
            DecEnc.Encode(plain);
            var writer = new DataWriter();
            writer.WriteBytes(plain);
            await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse);
        }
        catch (Exception)
        {
            // 切断直後の書き込みなど。切断は ConnectionStatusChanged 側で通知する。
        }
    }

    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        var raw = new byte[args.CharacteristicValue.Length];
        using (var reader = DataReader.FromBuffer(args.CharacteristicValue))
        {
            reader.ReadBytes(raw);
        }

        // MTU が拡張されている場合は 20 byte のフレームが複数まとまって届く。
        for (var offset = 0; offset + MemeProtocol.PacketLength <= raw.Length;
             offset += MemeProtocol.PacketLength)
        {
            var packet = raw.AsSpan(offset, MemeProtocol.PacketLength).ToArray();
            DecEnc.Decode(packet);
            Dispatch(packet);
        }
    }

    private void Dispatch(byte[] packet)
    {
        if (!MemeProtocol.IsValidPacket(packet))
        {
            return;
        }

        switch (packet[1])
        {
            case MemeProtocol.AupReportDevInfo:
                _fwVersion = MemeProtocol.ParseFirmwareVersion(packet);
                Send(MemeProtocol.GetMode());
                break;

            case MemeProtocol.AupReportMode:
                _mode = (MEMEMode)packet[4];
                _quality = (MEMEQuality)packet[5];
                Send(MemeProtocol.Get6AxisParams());
                break;

            case MemeProtocol.AupReport6AxisParams:
                _accelRange = (MEMEAccelRange)packet[2];
                _gyroRange = (MEMEGyroRange)packet[3];
                lock (_gate)
                {
                    _connectTimer?.Dispose();
                    _connectTimer = null;
                }

                memePeripheralConnected?.Invoke(this, MEMEStatus.MEMELIB_OK);
                break;

            case MemeProtocol.AupReportAcademia2:
                memeAcademicFullDataReceived?.Invoke(this, MemeProtocol.ParseFullData(packet));
                break;

            default:
                // 0x8F(応答) / 0x98 / 0x9A は Full モードのサンプルでは使わない。
                break;
        }
    }

    #endregion

    #region Settings

    public MEMEMode getMode() => _mode;

    public MEMEQuality getQuality() => _quality;

    public MEMEAccelRange getAccelRange() => _accelRange;

    public MEMEGyroRange getGyroRange() => _gyroRange;

    public MEMEStatus setMode(MEMEMode mode, MEMEQuality quality)
    {
        if (!_connected)
        {
            return MEMEStatus.MEMELIB_NG;
        }

        _mode = mode;
        _quality = quality;
        Send(MemeProtocol.SetMode(mode, quality));
        return MEMEStatus.MEMELIB_OK;
    }

    public MEMEStatus setAccelRange(MEMEAccelRange range)
    {
        if (!_connected)
        {
            return MEMEStatus.MEMELIB_NG;
        }

        _accelRange = range;
        Send(MemeProtocol.Set6AxisParams(_accelRange, _gyroRange));
        return MEMEStatus.MEMELIB_OK;
    }

    public MEMEStatus setGyroRange(MEMEGyroRange range)
    {
        if (!_connected)
        {
            return MEMEStatus.MEMELIB_NG;
        }

        _gyroRange = range;
        Send(MemeProtocol.Set6AxisParams(_accelRange, _gyroRange));
        return MEMEStatus.MEMELIB_OK;
    }

    public MEMEStatus startDataReport()
    {
        if (!_connected || _measuring)
        {
            return MEMEStatus.MEMELIB_NG;
        }

        _measuring = true;
        Send(MemeProtocol.StartStop(true));
        return MEMEStatus.MEMELIB_OK;
    }

    public MEMEStatus stopDataReport()
    {
        if (!_connected || !_measuring)
        {
            return MEMEStatus.MEMELIB_NG;
        }

        _measuring = false;
        Send(MemeProtocol.StartStop(false));
        _csv.Close();
        return MEMEStatus.MEMELIB_OK;
    }

    #endregion

    #region Version

    /// <summary>AUP_REPORT_DEV_INFO(0x81) で取得した ES_R 本体のファームウェアバージョン。</summary>
    public string getFWVersion() =>
        $"{_fwVersion.Major}.{_fwVersion.Minor}.{_fwVersion.Build}";

    /// <summary>
    /// ハードウェアバージョン。0x81 のレポートから読み出す位置が
    /// Mac / Android の参照実装にも無いため、本ライブラリでは提供しない。
    /// </summary>
    public string getHWVersion() => string.Empty;

    public string getSDKVersion() =>
        typeof(MEMELib).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0]
        ?? typeof(MEMELib).Assembly.GetName().Version?.ToString(3)
        ?? "0.0.0";

    #endregion

    /// <summary>CSV へ 1 行追記する。ファイルは計測停止まで開いたまま保持される。</summary>
    public void saveData(string directoryName, string fileName, string writeData) =>
        _csv.WriteLine(directoryName, fileName, writeData);

    /// <summary>CSV を閉じてバッファをフラッシュする。</summary>
    public void closeSaveFile() => _csv.Close();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        lock (_gate)
        {
            StopScanCore();
        }

        Teardown();
        _csv.Dispose();
    }
}
