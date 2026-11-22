namespace Bogopali.Core.Tests;

public sealed class HotkeyTests
{
    public static TheoryData<string, HotkeyModifiers, string> ValidHotkeys => new()
    {
        { "Alt + `", HotkeyModifiers.Alt, "`" },
        { "Ctrl + Alt + p", HotkeyModifiers.Control | HotkeyModifiers.Alt, "p" },
        { "Ctrl + Shift + P", HotkeyModifiers.Control | HotkeyModifiers.Shift, "p" },
        { "Ctrl + Alt + K", HotkeyModifiers.Control | HotkeyModifiers.Alt, "k" },
        { "Ctrl + +", HotkeyModifiers.Control, "+" },
        { "Ctrl++", HotkeyModifiers.Control, "+" },
        { "Ctrl + F12", HotkeyModifiers.Control, "F12" },
        { "control+alt+space", HotkeyModifiers.Control | HotkeyModifiers.Alt, "Space" },
        { "Win + p", HotkeyModifiers.Windows, "p" },
        { "F9", HotkeyModifiers.None, "F9" },
    };

    [Theory]
    [MemberData(nameof(ValidHotkeys))]
    public void ParsesValidHotkeys(string text, HotkeyModifiers modifiers, string key)
    {
        Assert.True(Hotkey.TryParse(text, out Hotkey? hotkey));
        Assert.Equal(new Hotkey(modifiers, key), hotkey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Alt +")]
    [InlineData("Alt + pp")]
    [InlineData("Hyper + p")]
    [InlineData("Alt + Alt + p")]
    [InlineData("Ctrl + + p")]
    [InlineData("p")]
    [InlineData("Shift + p")]
    [InlineData("F25")]
    public void RejectsInvalidHotkeys(string text) => Assert.False(Hotkey.TryParse(text, out _));

    [Fact]
    public void DefaultIsAltBacktick() => Assert.Equal(new Hotkey(HotkeyModifiers.Alt, "`"), Hotkey.Default);

    [Theory]
    [InlineData("Alt + `")]
    [InlineData("Ctrl + Alt + p")]
    [InlineData("Ctrl + +")]
    [InlineData("Ctrl + Shift + Win + F1")]
    public void FormatsInCanonicalFormThatParsesBack(string canonical)
    {
        Assert.True(Hotkey.TryParse(canonical, out Hotkey? hotkey));
        Assert.Equal(canonical, hotkey.ToString());
    }
}
