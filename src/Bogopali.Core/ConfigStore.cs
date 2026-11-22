using System.Globalization;
using System.Text;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Parsing;

namespace Bogopali.Core;

/// <summary>The settings bogopali runs with.</summary>
/// <param name="ToggleHotkey">The key combination that switches between Pāli Mode and Bypass Mode.</param>
/// <param name="Rules">The Telex Rules applied in Pāli Mode.</param>
public sealed record BogopaliConfig(Hotkey ToggleHotkey, TelexRuleSet Rules)
{
    /// <summary>The built-in defaults.</summary>
    public static BogopaliConfig Default { get; } = new(Hotkey.Default, TelexRuleSet.Default);
}

/// <summary>Why <c>config.toml</c> could not be used.</summary>
public enum ConfigErrorKind
{
    /// <summary>The file could not be read or created.</summary>
    FileUnreadable,

    /// <summary>The file is not valid TOML; <see cref="ConfigError.Line"/> says where, when known.</summary>
    InvalidToml,

    /// <summary>The Toggle Hotkey is not a valid key combination; <see cref="ConfigError.Subject"/> holds it.</summary>
    InvalidToggleHotkey,

    /// <summary>The <c>rules</c> entry is not a table.</summary>
    RulesNotATable,

    /// <summary>A Telex Rule has no text value; <see cref="ConfigError.Subject"/> holds its sequence.</summary>
    InvalidRule,

    /// <summary>A Telex Rule is defined twice (sequences ignore case); <see cref="ConfigError.Subject"/> holds it.</summary>
    DuplicateRule,
}

/// <summary>
/// A problem with <c>config.toml</c>, structured so the App can describe it in the user's language.
/// </summary>
/// <param name="Kind">What went wrong.</param>
/// <param name="Line">The 1-based line of a TOML syntax error, when known.</param>
/// <param name="Subject">The offending value or rule sequence, when there is one.</param>
/// <param name="TechnicalDetail">The untranslated detail from the parser or operating system, for logs.</param>
public sealed record ConfigError(ConfigErrorKind Kind, int? Line, string? Subject, string TechnicalDetail);

/// <summary>The outcome of <see cref="ConfigStore.Load"/>.</summary>
/// <param name="Config">The configuration to run with; the defaults when <paramref name="Error"/> is set.</param>
/// <param name="Error">Why the configuration file could not be used, or <see langword="null"/> on success.</param>
public sealed record ConfigLoadResult(BogopaliConfig Config, ConfigError? Error);

/// <summary>
/// Reads and writes <c>config.toml</c>. User Telex Rules are merged over the defaults, an empty value
/// disables a default rule, and any problem falls back to the defaults with an error for the App to show.
/// </summary>
/// <param name="directory">The folder that holds <c>config.toml</c>, e.g. the per-user local app data folder.</param>
public sealed class ConfigStore(string directory)
{
    private const string HotkeyKey = "toggle_hotkey";
    private const string RulesKey = "rules";

    private static readonly TomlSerializerOptions s_tomlOptions = new()
    {
        DuplicateKeyHandling = TomlDuplicateKeyHandling.Error,
    };

    /// <summary>The full path of <c>config.toml</c>.</summary>
    public string FilePath { get; } = Path.Combine(directory, "config.toml");

    /// <summary>
    /// Loads the configuration. When the file does not exist it is created with the defaults.
    /// An invalid file is never overwritten.
    /// </summary>
    /// <returns>The configuration and any error.</returns>
    public ConfigLoadResult Load()
    {
        string text;
        try
        {
            if (!File.Exists(FilePath))
            {
                _ = Directory.CreateDirectory(directory);
                File.WriteAllText(FilePath, DefaultFileContent(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return new ConfigLoadResult(BogopaliConfig.Default, null);
            }

            text = File.ReadAllText(FilePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Fallback(ConfigErrorKind.FileUnreadable, exception.Message);
        }

        TomlTable table;
        try
        {
            // Strict parsing with validation rejects what TOML forbids, such as duplicate keys.
            _ = SyntaxParser.ParseStrict(text, FilePath, validate: true);
            table = TomlSerializer.Deserialize<TomlTable>(text, s_tomlOptions) ?? [];
        }
        catch (TomlException exception)
        {
            return Fallback(ConfigErrorKind.InvalidToml, exception.Message, line: exception.Line);
        }

        return Interpret(table);
    }

    /// <summary>The text written to a new <c>config.toml</c>.</summary>
    /// <returns>A commented TOML document holding the default Toggle Hotkey and Telex Rules.</returns>
    public static string DefaultFileContent()
    {
        StringBuilder builder = new();
        _ = builder
            .AppendLine("# Pāli Key configuration. Changes apply the next time Pāli Key starts.")
            .AppendLine()
            .AppendLine("# Key combination that switches between Pali Mode and Bypass Mode,")
            .AppendLine("# e.g. \"Alt + `\", \"Ctrl + Alt + p\", \"Ctrl + +\" or \"Ctrl + F12\".")
            .AppendLine(CultureInfo.InvariantCulture, $"{HotkeyKey} = \"{Hotkey.Default}\"")
            .AppendLine()
            .AppendLine("# Telex Rules: typed sequence = output. Rules here are merged over the built-in")
            .AppendLine("# defaults; set a rule to \"\" to disable it. The longest matching sequence wins.")
            .AppendLine($"[{RulesKey}]");
        foreach ((string sequence, string output) in TelexRuleSet.DefaultRules)
        {
            _ = builder.AppendLine(CultureInfo.InvariantCulture, $"\"{sequence}\" = \"{output}\"");
        }

        return builder.ToString();
    }

    private static ConfigLoadResult Interpret(TomlTable table)
    {
        Hotkey hotkey = Hotkey.Default;
        if (table.TryGetValue(HotkeyKey, out object? hotkeyValue))
        {
            if (hotkeyValue is not string hotkeyText || !Hotkey.TryParse(hotkeyText, out Hotkey? parsed))
            {
                string text = Convert.ToString(hotkeyValue, CultureInfo.InvariantCulture) ?? string.Empty;
                return Fallback(ConfigErrorKind.InvalidToggleHotkey, $"'{HotkeyKey}' is not a valid key combination: {text}", text);
            }

            hotkey = parsed;
        }

        Dictionary<string, string> rules = new(StringComparer.Ordinal);
        foreach ((string sequence, string output) in TelexRuleSet.DefaultRules)
        {
            rules[sequence] = output;
        }

        if (table.TryGetValue(RulesKey, out object? rulesValue))
        {
            if (rulesValue is not TomlTable userRules)
            {
                return Fallback(ConfigErrorKind.RulesNotATable, $"'{RulesKey}' must be a table of rules.");
            }

            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach ((string rawSequence, object? value) in userRules)
            {
                string sequence = rawSequence.Normalize(NormalizationForm.FormC).ToLowerInvariant();
                if (value is not string output)
                {
                    return Fallback(ConfigErrorKind.InvalidRule, $"The rule '{rawSequence}' must have a text value.", rawSequence);
                }

                if (!seen.Add(sequence))
                {
                    return Fallback(ConfigErrorKind.DuplicateRule, $"The rule '{rawSequence}' is defined more than once.", rawSequence);
                }

                if (output.Length == 0)
                {
                    _ = rules.Remove(sequence);
                }
                else
                {
                    rules[sequence] = output;
                }
            }
        }

        try
        {
            return new ConfigLoadResult(new BogopaliConfig(hotkey, new TelexRuleSet(rules)), null);
        }
        catch (ArgumentException exception)
        {
            // Only an empty sequence can still be rejected here.
            return Fallback(ConfigErrorKind.InvalidRule, exception.Message, string.Empty);
        }
    }

    private static ConfigLoadResult Fallback(ConfigErrorKind kind, string technicalDetail, string? subject = null, int? line = null) =>
        new(BogopaliConfig.Default, new ConfigError(kind, line, subject, technicalDetail));
}
