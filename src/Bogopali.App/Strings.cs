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
    string OpenConfigurationFailed,
    string ConfigFile,
    string ConfigFileUnreadable,
    string ConfigInvalidToml,
    string ConfigInvalidTomlAtLine,
    string ConfigInvalidToggleHotkey,
    string ConfigRulesNotATable,
    string ConfigInvalidRule,
    string ConfigDuplicateRule)
{
    private static readonly Strings s_english = new(
        PaliMode: "Pāli Mode",
        BypassMode: "Bypass Mode",
        StartWithWindows: "Start with Windows",
        OpenConfigurationFile: "Open configuration file",
        Exit: "Exit",
        Ok: "OK",
        ConfigurationError: "The configuration file could not be read, so Pāli Key is using the default settings.",
        StartupDisabledByUser: "Starting with Windows was turned off in Windows Settings. To turn it back on, open Settings > Apps > Startup and switch Pāli Key on.",
        StartupDisabledByPolicy: "Starting with Windows is controlled by your organization's policy.",
        StartupFailed: "The Start with Windows setting could not be changed.",
        OpenConfigurationFailed: "The configuration file could not be opened.",
        ConfigFile: "File: {0}",
        ConfigFileUnreadable: "Windows did not allow the file to be read or created.",
        ConfigInvalidToml: "The file contains a syntax error.",
        ConfigInvalidTomlAtLine: "Line {0} contains a syntax error.",
        ConfigInvalidToggleHotkey: "\"{0}\" is not a valid key combination for toggle_hotkey.",
        ConfigRulesNotATable: "The rules must be listed under a [rules] heading.",
        ConfigInvalidRule: "The rule \"{0}\" needs a text value in quotes.",
        ConfigDuplicateRule: "The rule \"{0}\" is defined more than once.");

    private static readonly Strings s_vietnamese = new(
        PaliMode: "Chế độ Pāli",
        BypassMode: "Chế độ gõ thường",
        StartWithWindows: "Khởi động cùng Windows",
        OpenConfigurationFile: "Mở tệp cấu hình",
        Exit: "Thoát",
        Ok: "Đồng ý",
        ConfigurationError: "Không đọc được tệp cấu hình, nên Pāli Key đang dùng cấu hình mặc định.",
        StartupDisabledByUser: "Tính năng khởi động cùng Windows đã bị tắt trong Cài đặt Windows. Để bật lại, hãy mở Cài đặt > Ứng dụng > Khởi động và bật Pāli Key.",
        StartupDisabledByPolicy: "Tính năng khởi động cùng Windows do chính sách của tổ chức quản lý.",
        StartupFailed: "Không thay đổi được tùy chọn khởi động cùng Windows.",
        OpenConfigurationFailed: "Không mở được tệp cấu hình.",
        ConfigFile: "Tệp: {0}",
        ConfigFileUnreadable: "Windows không cho phép đọc hoặc tạo tệp này.",
        ConfigInvalidToml: "Tệp có lỗi cú pháp.",
        ConfigInvalidTomlAtLine: "Dòng {0} có lỗi cú pháp.",
        ConfigInvalidToggleHotkey: "\"{0}\" không phải là tổ hợp phím hợp lệ cho toggle_hotkey.",
        ConfigRulesNotATable: "Các quy tắc phải được liệt kê dưới tiêu đề [rules].",
        ConfigInvalidRule: "Quy tắc \"{0}\" cần một giá trị văn bản đặt trong dấu ngoặc kép.",
        ConfigDuplicateRule: "Quy tắc \"{0}\" được khai báo nhiều lần.");

    public static Strings For(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == "vi" ? s_vietnamese : s_english;

    public string ModeName(InputMode mode) => mode == InputMode.Pali ? PaliMode : BypassMode;

    public string ToolTip(InputMode mode) => $"Pāli Key: {ModeName(mode)}";

    /// <summary>Describes a configuration problem in this language, followed by the file path.</summary>
    public string Describe(ConfigError error, string filePath)
    {
        string problem = error.Kind switch
        {
            ConfigErrorKind.FileUnreadable => ConfigFileUnreadable,
            ConfigErrorKind.InvalidToml when error.Line is int line =>
                Fill(ConfigInvalidTomlAtLine, line.ToString(CultureInfo.InvariantCulture)),
            ConfigErrorKind.InvalidToml => ConfigInvalidToml,
            ConfigErrorKind.InvalidToggleHotkey => Fill(ConfigInvalidToggleHotkey, error.Subject),
            ConfigErrorKind.RulesNotATable => ConfigRulesNotATable,
            ConfigErrorKind.InvalidRule => Fill(ConfigInvalidRule, error.Subject),
            ConfigErrorKind.DuplicateRule => Fill(ConfigDuplicateRule, error.Subject),
            _ => ConfigInvalidToml,
        };

        return $"{problem}\n{Fill(ConfigFile, filePath)}";
    }

    private static string Fill(string template, string? value) => template.Replace("{0}", value, StringComparison.Ordinal);
}
