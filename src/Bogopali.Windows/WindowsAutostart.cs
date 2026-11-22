using Bogopali.Core;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace Bogopali.Windows;

/// <summary>
/// "Start with Windows". The MSIX build uses its <c>StartupTask</c>; the portable build uses the per-user
/// <c>Run</c> registry key, which needs no admin rights.
/// </summary>
public static class WindowsAutostart
{
    /// <summary>The <c>TaskId</c> of the StartupTask declared in the package manifest.</summary>
    public const string StartupTaskId = "BogopaliStartup";

    /// <summary>Creates the autostart adapter that matches how bogopali was installed.</summary>
    /// <returns>The adapter.</returns>
    public static IAutostart Create() =>
        WindowsPackage.IsPackaged ? new StartupTaskAutostart() : new RunKeyAutostart(Environment.ProcessPath ?? string.Empty);

    private sealed class StartupTaskAutostart : IAutostart
    {
        public async Task<AutostartState> GetStateAsync() => Map((await GetTaskAsync().ConfigureAwait(false)).State);

        public async Task<AutostartState> EnableAsync()
        {
            StartupTask task = await GetTaskAsync().ConfigureAwait(false);
            return Map(await task.RequestEnableAsync());
        }

        public async Task<AutostartState> DisableAsync()
        {
            StartupTask task = await GetTaskAsync().ConfigureAwait(false);
            task.Disable();
            return Map(task.State);
        }

        private static async Task<StartupTask> GetTaskAsync() => await StartupTask.GetAsync(StartupTaskId);

        private static AutostartState Map(StartupTaskState state) => state switch
        {
            StartupTaskState.Enabled => AutostartState.Enabled,
            StartupTaskState.DisabledByUser => AutostartState.DisabledByUser,
            StartupTaskState.DisabledByPolicy => AutostartState.DisabledByPolicy,
            StartupTaskState.EnabledByPolicy => AutostartState.EnabledByPolicy,
            _ => AutostartState.Disabled,
        };
    }

    /// <summary>
    /// Uses <c>HKCU\...\Run</c>. Task Manager and Windows Settings record a disabled entry under
    /// <c>Explorer\StartupApproved\Run</c> (odd first byte), which is read so the menu shows the real state.
    /// </summary>
    private sealed class RunKeyAutostart(string executablePath) : IAutostart
    {
        private const string ValueName = "bogopali";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

        private string Command { get; } = $"\"{executablePath}\"";

        public Task<AutostartState> GetStateAsync()
        {
            using RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunKey);
            // An entry for another path (the user moved the extracted folder) would not start this copy.
            if (run?.GetValue(ValueName) is not string command || command != Command)
            {
                return Task.FromResult(AutostartState.Disabled);
            }

            using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(ApprovedKey);
            bool disabledByUser = approved?.GetValue(ValueName) is byte[] { Length: > 0 } flags && (flags[0] & 1) != 0;
            return Task.FromResult(disabledByUser ? AutostartState.Disabled : AutostartState.Enabled);
        }

        public Task<AutostartState> EnableAsync()
        {
            using (RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                run.SetValue(ValueName, Command, RegistryValueKind.String);
            }

            ClearApproval();
            return GetStateAsync();
        }

        public Task<AutostartState> DisableAsync()
        {
            using (RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true))
            {
                run?.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            ClearApproval();
            return GetStateAsync();
        }

        private static void ClearApproval()
        {
            using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, writable: true);
            approved?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
