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
    private readonly Strings _strings = Strings.For(CultureInfo.CurrentUICulture);
    private readonly IAutostart _autostart = WindowsAutostart.Create();
    private TranslationSession? _session;
    private WindowsInputHook? _hook;
    private TrayIcon? _tray;
    private NativeMenuItem? _modeItem;
    private NativeMenuItem? _autostartItem;
    private string _configPath = string.Empty;

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

        _tray = new TrayIcon { Menu = menu, IsVisible = true };
        _tray.Clicked += (_, _) => _session.ToggleMode();
        TrayIcon.SetIcons(this, [_tray]);
        ShowMode(_session.Mode);

        _hook = new WindowsInputHook();
        _hook.Start(loaded.Config.ToggleHotkey, _session.Handle);

        desktop.Exit += (_, _) => Stop();

        if (loaded.Error is not null)
        {
            MessageWindow.ShowMessage(_strings.ConfigurationError, $"{_configPath}\n{loaded.Error}", _strings.Ok);
        }

        _ = RefreshAutostartAsync();
    }

    private void Stop()
    {
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
        try
        {
            AutostartState state = await _autostart.GetStateAsync().ConfigureAwait(true);
            SetAutostartCheck(state);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Trace.TraceError($"bogopali autostart: {exception}");
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
            MessageWindow.ShowMessage(_strings.StartupFailed, exception.Message, _strings.Ok);
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
            MessageWindow.ShowMessage(_strings.OpenConfigurationFailed, exception.Message, _strings.Ok);
        }
    }
}
