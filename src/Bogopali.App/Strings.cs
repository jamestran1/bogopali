using System.Globalization;
using Bogopali.Core;

namespace Bogopali.App;

/// <summary>User-visible text, in Vietnamese when the Windows display language is Vietnamese, else English.</summary>
internal sealed record Strings(
    string PaliMode,
    string BypassMode,
    string StartWithWindows,
    string OpenConfigurationFile,
    string Exit,
    string Ok,
    string ConfigurationError,
    string StartupDisabledByUser,
    string StartupDisabledByPolicy,
    string StartupFailed,
    string OpenConfigurationFailed)
{
    private static readonly Strings s_english = new(
        PaliMode: "Pāli Mode",
        BypassMode: "Bypass Mode",
        StartWithWindows: "Start with Windows",
        OpenConfigurationFile: "Open configuration file",
        Exit: "Exit",
        Ok: "OK",
        ConfigurationError: "The configuration file could not be read, so bogopali is using the default settings.",
        StartupDisabledByUser: "Starting with Windows was turned off in Windows Settings. To turn it back on, open Settings > Apps > Startup and switch bogopali on.",
        StartupDisabledByPolicy: "Starting with Windows is controlled by your organization's policy.",
        StartupFailed: "The Start with Windows setting could not be changed.",
        OpenConfigurationFailed: "The configuration file could not be opened.");

    private static readonly Strings s_vietnamese = new(
        PaliMode: "Chế độ Pāli",
        BypassMode: "Chế độ gõ thường",
        StartWithWindows: "Khởi động cùng Windows",
        OpenConfigurationFile: "Mở tệp cấu hình",
        Exit: "Thoát",
        Ok: "Đồng ý",
        ConfigurationError: "Không đọc được tệp cấu hình, nên bogopali đang dùng cấu hình mặc định.",
        StartupDisabledByUser: "Tính năng khởi động cùng Windows đã bị tắt trong Cài đặt Windows. Để bật lại, hãy mở Cài đặt > Ứng dụng > Khởi động và bật bogopali.",
        StartupDisabledByPolicy: "Tính năng khởi động cùng Windows do chính sách của tổ chức quản lý.",
        StartupFailed: "Không thay đổi được tùy chọn khởi động cùng Windows.",
        OpenConfigurationFailed: "Không mở được tệp cấu hình.");

    public static Strings For(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == "vi" ? s_vietnamese : s_english;

    public string ModeName(InputMode mode) => mode == InputMode.Pali ? PaliMode : BypassMode;

    public string ToolTip(InputMode mode) => $"bogopali: {ModeName(mode)}";
}
