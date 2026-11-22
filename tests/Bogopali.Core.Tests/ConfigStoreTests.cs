namespace Bogopali.Core.Tests;

public sealed class ConfigStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "bogopali-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private ConfigStore Store => new(_directory);

    private ConfigLoadResult LoadWith(string toml)
    {
        _ = Directory.CreateDirectory(_directory);
        File.WriteAllText(Store.FilePath, toml);
        return Store.Load();
    }

    private static string Typed(ConfigLoadResult result, string keys) => TextField.Typed(keys, result.Config.Rules);

    [Fact]
    public void FilePathIsConfigTomlInTheDirectory() =>
        Assert.Equal(Path.Combine(_directory, "config.toml"), Store.FilePath);

    [Fact]
    public void MissingFileWritesTheDefaults()
    {
        ConfigLoadResult result = Store.Load();

        Assert.Null(result.Error);
        Assert.True(File.Exists(Store.FilePath));
        Assert.Equal(Hotkey.Default, result.Config.ToggleHotkey);
        Assert.Equal("ā", Typed(result, "af"));
    }

    [Fact]
    public void WrittenDefaultsLoadBackAsTheDefaults()
    {
        _ = Store.Load();

        ConfigLoadResult reloaded = Store.Load();

        Assert.Null(reloaded.Error);
        Assert.Equal(Hotkey.Default, reloaded.Config.ToggleHotkey);
        Assert.Equal(TelexRuleSet.Default.Count, reloaded.Config.Rules.Count);
        Assert.All(TelexRuleSet.DefaultRules, rule => Assert.Equal(TextField.Typed(rule.Key), Typed(reloaded, rule.Key)));
    }

    [Fact]
    public void UserRulesMergeOverDefaults()
    {
        ConfigLoadResult result = LoadWith("""
            [rules]
            sf = "ṣ"
            """);

        Assert.Null(result.Error);
        Assert.Equal("ṣ", Typed(result, "sf"));
        Assert.Equal("ā", Typed(result, "af"));
        Assert.Equal(TelexRuleSet.Default.Count + 1, result.Config.Rules.Count);
    }

    [Fact]
    public void UserRuleOverridesADefaultRule()
    {
        ConfigLoadResult result = LoadWith("""
            [rules]
            af = "â"
            """);

        Assert.Equal("â", Typed(result, "af"));
        Assert.Equal("ā", Typed(result, "aa"));
    }

    [Fact]
    public void EmptyValueDisablesADefaultRule()
    {
        ConfigLoadResult result = LoadWith("""
            [rules]
            aa = ""
            """);

        Assert.Null(result.Error);
        Assert.Equal("aa", Typed(result, "aa"));
        Assert.Equal("ā", Typed(result, "af"));
        Assert.Equal(TelexRuleSet.Default.Count - 1, result.Config.Rules.Count);
    }

    [Theory]
    [InlineData("Ctrl + Alt + p", HotkeyModifiers.Control | HotkeyModifiers.Alt, "p")]
    [InlineData("Ctrl + +", HotkeyModifiers.Control, "+")]
    [InlineData("Ctrl + Shift + P", HotkeyModifiers.Control | HotkeyModifiers.Shift, "p")]
    public void CustomToggleHotkey(string text, HotkeyModifiers modifiers, string key)
    {
        ConfigLoadResult result = LoadWith($"toggle_hotkey = \"{text}\"");

        Assert.Null(result.Error);
        Assert.Equal(new Hotkey(modifiers, key), result.Config.ToggleHotkey);
    }

    [Theory]
    [InlineData("toggle_hotkey = ")]
    [InlineData("[rules\naf = \"x\"")]
    [InlineData("\ntoggle_hotkey = \"Alt + `\"\ntoggle_hotkey = \"Alt + `\"")]
    public void InvalidTomlFallsBackToDefaultsAndReportsTheError(string toml)
    {
        ConfigLoadResult result = LoadWith(toml);

        Assert.NotNull(result.Error);
        Assert.Equal(ConfigErrorKind.InvalidToml, result.Error.Kind);
        Assert.Equal(Hotkey.Default, result.Config.ToggleHotkey);
        Assert.Equal("ā", Typed(result, "af"));
    }

    [Theory]
    [InlineData("[rules\naf = \"x\"", 1)]
    [InlineData("\ntoggle_hotkey = \"Alt + `\"\ntoggle_hotkey = \"Alt + `\"", 3)]
    public void InvalidTomlReportsTheLine(string toml, int line) =>
        Assert.Equal(line, LoadWith(toml).Error?.Line);

    [Theory]
    [InlineData("toggle_hotkey = \"Hyper + p\"", ConfigErrorKind.InvalidToggleHotkey, "Hyper + p")]
    [InlineData("toggle_hotkey = 5", ConfigErrorKind.InvalidToggleHotkey, "5")]
    [InlineData("rules = \"af\"", ConfigErrorKind.RulesNotATable, null)]
    [InlineData("[rules]\naf = 1", ConfigErrorKind.InvalidRule, "af")]
    [InlineData("[rules]\nAF = \"x\"\naf = \"y\"", ConfigErrorKind.DuplicateRule, "af")]
    public void InvalidValuesFallBackToDefaultsAndReportTheError(string toml, ConfigErrorKind kind, string? subject)
    {
        ConfigLoadResult result = LoadWith(toml);

        Assert.NotNull(result.Error);
        Assert.Equal(kind, result.Error.Kind);
        Assert.Equal(subject, result.Error.Subject);
        Assert.Equal(Hotkey.Default, result.Config.ToggleHotkey);
        Assert.Equal(TelexRuleSet.Default.Count, result.Config.Rules.Count);
    }

    [Fact]
    public void InvalidFileIsLeftUntouched()
    {
        const string Broken = "toggle_hotkey = ";

        _ = LoadWith(Broken);

        Assert.Equal(Broken, File.ReadAllText(Store.FilePath));
    }
}
