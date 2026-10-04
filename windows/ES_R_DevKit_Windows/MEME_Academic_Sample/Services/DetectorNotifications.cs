using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace MEME_Academic_Sample.Services;

/// <summary>
/// 判定器の通知(グラフ画面の notify)を Windows の通知として出す。Windows App SDK の AppNotificationManager を使う。
///
/// - アプリはパッケージ化していない(WindowsPackageType=None)。Windows App SDK のランタイム(Windows App Runtime)は
///   **同梱せず**、利用者の PC に入っているものを使う(本人の方針、2026-10-04)。起動時の自動初期化は切ってあり
///   (WindowsAppSdkBootstrapInitialize=false。ランタイムが無いとプロセスごと終わるため)、最初に通知を出そうとしたときに
///   ここで Bootstrap を初期化する。ランタイムが無ければ通知は出さず(計測は続ける)、起動中の最初の 1 回だけ案内のダイアログを出す。
/// - 同じ tag は Tag / Group が同じなので 1 件に置き換わる。時刻はイベントのサンプルの時刻(分かれば)。
/// - クリックでアプリを前面に出す(NotificationInvoked)。
/// UI スレッドから呼ぶ。
/// </summary>
public static class DetectorNotifications
{
    private const string Group = "detector";

    private static bool? available;        // null = まだ試していない
    private static bool helpShown;
    private static Form? mainForm;

    /// <summary>自己テストの間だけ、案内のダイアログを出さずにここへ書く(null なら通常どおりダイアログ)</summary>
    internal static List<string>? SuppressedDialogs { get; set; }

    /// <summary>ランタイムがあって通知が使えるか(最初の <see cref="Show"/> の後で決まる。自己テスト用)</summary>
    public static bool? Available => available;

    /// <summary>使えなかった理由(自己テスト・調べもの用)</summary>
    public static string? LastError { get; private set; }

    /// <summary>通知のクリックで前面に出す窓(起動時に渡す)</summary>
    public static void Attach(Form form) => mainForm = form;

    /// <summary>出す。出せなければ(ランタイムが無い・通知がオフ)false</summary>
    public static bool Show(string tag, string title, string? text, DateTime? utc)
    {
        if (!EnsureInitialized())
        {
            return false;
        }

        try
        {
            return ShowCore(tag, title, text, utc);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"[DetectorNotifications] show failed: {e.Message}");
            LastError = $"show: {e.GetType().Name}: {e.Message}";
            return false;
        }
    }

    // Windows App SDK の型に触るのはこの下の NoInlining のメソッドだけ(ランタイムの初期化前に JIT で触らないように)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ShowCore(string tag, string title, string? text, DateTime? utc)
    {
        var manager = AppNotificationManager.Default;
        if (manager.Setting != AppNotificationSetting.Enabled)
        {
            Debug.WriteLine($"[DetectorNotifications] not shown ({manager.Setting}): [{tag}] {title}");
            return false;
        }

        var b = new AppNotificationBuilder().AddText(title);
        if (text is not null)
        {
            b.AddText(text);
        }

        if (utc is { } t)
        {
            b.SetTimeStamp(t.ToLocalTime());
        }

        var n = b.BuildNotification();
        n.Tag = tag;
        n.Group = Group;
        manager.Show(n);
        return n.Id != 0;
    }

    private static bool EnsureInitialized()
    {
        if (available is { } a)
        {
            return a;
        }

        try
        {
            available = Initialize();
        }
        catch (Exception e)
        {
            // ランタイムの DLL が読めないなど
            Debug.WriteLine($"[DetectorNotifications] initialize failed: {e.Message}");
            LastError = $"{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture} {e}";
            available = false;
        }

        if (available == false)
        {
            ShowRuntimeHelp();
        }

        return available == true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Initialize()
    {
        // ビルドに使った Windows App SDK と同じメジャー・マイナーで、それ以降のランタイムを探す
        if (!Bootstrap.TryInitialize(Microsoft.WindowsAppSDK.Release.MajorMinor, Microsoft.WindowsAppSDK.Release.VersionTag,
                new PackageVersion(Microsoft.WindowsAppSDK.Runtime.Version.UInt64), Bootstrap.InitializeOptions.None, out var hr))
        {
            Debug.WriteLine($"[DetectorNotifications] Windows App Runtime not found (0x{hr:X8})");
            LastError = $"Bootstrap.TryInitialize 0x{hr:X8} ({System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture})";
            return false;
        }

        var manager = AppNotificationManager.Default;
        manager.NotificationInvoked += (_, _) =>
        {
            // クリック: 前面に出す(通知のスレッドから来るので UI スレッドへ)
            var f = mainForm;
            f?.BeginInvoke(() =>
            {
                if (f.WindowState == FormWindowState.Minimized)
                {
                    f.WindowState = FormWindowState.Normal;
                }

                f.Activate();
            });
        };
        manager.Register();
        return true;
    }

    /// <summary>アプリを閉じるときに呼ぶ(登録を外す。初期化していなければ何もしない)</summary>
    public static void Shutdown()
    {
        if (available != true)
        {
            return;
        }

        try
        {
            UnregisterCore();
            Bootstrap.Shutdown();
        }
        catch (Exception e)
        {
            Debug.WriteLine($"[DetectorNotifications] shutdown failed: {e.Message}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UnregisterCore() => AppNotificationManager.Default.Unregister();

    /// <summary>ランタイムが無い: 起動中の最初の 1 回だけ案内する(計測は続ける)</summary>
    private static void ShowRuntimeHelp()
    {
        if (helpShown)
        {
            return;
        }

        helpShown = true;
        // ランタイムは CPU ごと(x64 / ARM64)。公式のインストーラは、通知に要る付属パッケージ(Main・Singleton)も入れる
        // (ほかのアプリの依存として本体だけ入っている PC では、Register が「クラスが登録されていません」で失敗する)
        var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "arm64" : "x64";
        var installer = $"https://aka.ms/windowsappsdk/{RuntimeVersionText}/latest/windowsappruntimeinstall-{arch}.exe";
        var message =
            $"To use notifications, please install the Windows App SDK runtime (Windows App Runtime {RuntimeVersionText}, {arch}).\n\n" +
            "The measurement continues without notifications.\n\nDownload the installer now?";
        if (SuppressedDialogs is { } list)
        {
            list.Add(message);
            return;
        }

        var owner = mainForm;
        if (MessageBox.Show(owner, message, "Notifications", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo(installer) { UseShellExecute = true });
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                Debug.WriteLine($"[DetectorNotifications] could not open {installer}: {e.Message}");
            }
        }
    }

    /// <summary>案内に出すランタイムの版(ビルドに使った Windows App SDK のメジャー.マイナー)</summary>
    private static string RuntimeVersionText
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            var mm = Microsoft.WindowsAppSDK.Release.MajorMinor;
            return $"{mm >> 16}.{mm & 0xFFFF}";
        }
    }
}
