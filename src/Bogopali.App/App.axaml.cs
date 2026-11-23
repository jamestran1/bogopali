using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Bogopali.Core;
using Bogopali.Windows;

namespace Bogopali.App;

internal sealed partial class App : Application, IDisposable
{
    private readonly IAutostart _autostart = WindowsAutostart.Create();
    private TranslationSession? _session;
    private WindowsInputHook? _hook;
    private TrayIcon? _tray;
    private NativeMenuItem? _modeItem;
    private NativeMenuItem? _autostartItem;
    private static readonly TimeSpan s_autostartPollInterval = TimeSpan.FromSeconds(3);

    private readonly Strings _strings = Strings.For(CultureInfo.CurrentUICulture);
    private string _configPath = string.Empty;
    private DispatcherTimer? _autostartPoll;
    private bool _refreshingAutostart;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public void Dispose() => Stop();

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Start(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void Start(IClassicDesktopStyleApplicationLifetime desktop)
    {
        ConfigStore store = new(WindowsPackage.GetConfigDirectory());
        _configPath = store.FilePath;
        ConfigLoadResult loaded = store.Load();

        _session = new TranslationSession(new InputEngine(loaded.Config.Rules, TimeProvider.System), new WindowsKeystrokeSender());
        _session.ModeChanged += (_, mode) => Dispatcher.UIThread.Post(() => ShowMode(mode));

        _modeItem = new NativeMenuItem(_strings.PaliMode) { ToggleType = MenuItemToggleType.CheckBox };
        _modeItem.Click += (_, _) => _session.ToggleMode();

        _autostartItem = new NativeMenuItem(_strings.StartWithWindows) { ToggleType = MenuItemToggleType.CheckBox };
        _autostartItem.Click += OnAutostartClicked;

        NativeMenuItem openConfigItem = new(_strings.OpenConfigurationFile);
        openConfigItem.Click += (_, _) => OpenConfigurationFile();

        NativeMenuItem exitItem = new(_strings.Exit);
        exitItem.Click += (_, _) => desktop.Shutdown();

        NativeMenu menu = [_modeItem, _autostartItem, openConfigItem, new NativeMenuItemSeparator(), exitItem];
        menu.Opening += async (_, _) => await RefreshAutostartAsync().ConfigureAwait(true);

        // The Windows tray menu does not raise Opening, so the check would keep its old state after the user
        // turns startup off in Windows Settings. Re-read the state periodically instead.
        _autostartPoll = new DispatcherTimer { Interval = s_autostartPollInterval };
        _autostartPoll.Tick += async (_, _) => await RefreshAutostartAsync().ConfigureAwait(true);
        _autostartPoll.Start();

        _tray = new TrayIcon { Menu = menu, IsVisible = true };
        _tray.Clicked += (_, _) => _session.ToggleMode();
        TrayIcon.SetIcons(this, [_tray]);
        ShowMode(_session.Mode);

        _hook = new WindowsInputHook();
        _hook.Start(loaded.Config.ToggleHotkey, _session.Handle);

        desktop.Exit += (_, _) => Stop();

        if (loaded.Error is not null)
        {
            // The technical detail is untranslated, so it goes to the trace and the user sees a localized description.
            Trace.TraceWarning($"bogopali configuration: {loaded.Error.TechnicalDetail}");
            MessageWindow.ShowMessage(_strings.ConfigurationError, _strings.Describe(loaded.Error, _configPath), _strings.Ok);
        }

        _ = RefreshAutostartAsync();
    }

    private void Stop()
    {
        _autostartPoll?.Stop();
        _autostartPoll = null;
        _hook?.Dispose();
        _hook = null;
        if (_tray is not null)
        {
            _tray.IsVisible = false;
            _tray.Dispose();
            _tray = null;
        }
    }

    private void ShowMode(InputMode mode)
    {
        if (_tray is null || _modeItem is null)
        {
            return;
        }

        _tray.Icon = StatusIcons.For(mode);
        _tray.ToolTipText = _strings.ToolTip(mode);
        _modeItem.IsChecked = mode == InputMode.Pali;
    }

    private async Task RefreshAutostartAsync()
    {
        // A slow StartupTask query must not stack up behind the next timer tick.
        if (_refreshingAutostart)
        {
            return;
        }

        _refreshingAutostart = true;
        try
        {
            AutostartState state = await _autostart.GetStateAsync().ConfigureAwait(true);
            SetAutostartCheck(state);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Trace.TraceError($"bogopali autostart: {exception}");
        }
        finally
        {
            _refreshingAutostart = false;
        }
    }

    private async void OnAutostartClicked(object? sender, EventArgs e)
    {
        try
        {
            AutostartState current = await _autostart.GetStateAsync().ConfigureAwait(true);
            bool turnOn = current is not (AutostartState.Enabled or AutostartState.EnabledByPolicy);
            AutostartState state = turnOn
                ? await _autostart.EnableAsync().ConfigureAwait(true)
                : await _autostart.DisableAsync().ConfigureAwait(true);
            SetAutostartCheck(state);

            string? problem = state switch
            {
                AutostartState.DisabledByUser when turnOn => _strings.StartupDisabledByUser,
                AutostartState.DisabledByPolicy or AutostartState.EnabledByPolicy => _strings.StartupDisabledByPolicy,
                _ => null,
            };
            if (problem is not null)
            {
                MessageWindow.ShowMessage(problem, null, _strings.Ok);
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Trace.TraceError($"bogopali autostart: {exception}");
            MessageWindow.ShowMessage(_strings.StartupFailed, null, _strings.Ok);
        }
    }

    private void SetAutostartCheck(AutostartState state)
    {
        if (_autostartItem is not null)
        {
            _autostartItem.IsChecked = state is AutostartState.Enabled or AutostartState.EnabledByPolicy;
        }
    }

    private void OpenConfigurationFile()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                // Recreates the defaults if the user deleted the file while bogopali was running.
                _ = new ConfigStore(Path.GetDirectoryName(_configPath)!).Load();
            }

            try
            {
                _ = Process.Start(new ProcessStartInfo(_configPath) { UseShellExecute = true });
            }
            catch (Win32Exception)
            {
                // No application is associated with .toml files.
                _ = Process.Start(new ProcessStartInfo("notepad.exe") { ArgumentList = { _configPath }, UseShellExecute = false });
            }
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException)
        {
            Trace.TraceError($"bogopali open configuration: {exception}");
            MessageWindow.ShowMessage(_strings.OpenConfigurationFailed, _strings.Describe(
                new ConfigError(ConfigErrorKind.FileUnreadable, null, null, exception.Message), _configPath), _strings.Ok);
        }
    }
}
