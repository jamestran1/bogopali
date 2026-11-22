using System.Text;

namespace Bogopali.Core.Tests;

public sealed class InputEngineTests
{
    private readonly FakeTimeProvider _clock = new();

    private InputEngine NewEngine(TelexRuleSet? rules = null) => new(rules ?? TelexRuleSet.Default, _clock);

    [Fact]
    public void DiacriticKeyAfterAReplacesTheAWithLongVowel()
    {
        InputEngine engine = NewEngine();

        Assert.Equal(KeyDecision.PassThrough, engine.Handle(InputEvent.Typed('a')));
        Assert.Equal(KeyDecision.Replace(1, "ā"), engine.Handle(InputEvent.Typed('f')));
    }

    [Fact]
    public void LettersWithoutARulePassThrough()
    {
        InputEngine engine = NewEngine();

        Assert.Equal(KeyDecision.PassThrough, engine.Handle(InputEvent.Typed('a')));
        Assert.Equal(KeyDecision.PassThrough, engine.Handle(InputEvent.Typed('b')));
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("baf", "bā")]
    public void TypingProducesExpectedText(string keys, string expected) =>
        Assert.Equal(expected, TextField.Typed(keys));

    [Theory]
    [InlineData("af", "ā")]
    [InlineData("if", "ī")]
    [InlineData("uf", "ū")]
    [InlineData("aa", "ā")]
    [InlineData("ii", "ī")]
    [InlineData("uu", "ū")]
    public void LongVowels(string keys, string expected) => Assert.Equal(expected, TextField.Typed(keys));

    [Theory]
    [InlineData("tf", "ṭ")]
    [InlineData("df", "ḍ")]
    [InlineData("lf", "ḷ")]
    [InlineData("mf", "ṃ")]
    public void RetroflexConsonantsAndNiggahita(string keys, string expected) =>
        Assert.Equal(expected, TextField.Typed(keys));

    [Theory]
    [InlineData("nf", "ṇ")]
    [InlineData("nff", "ñ")]
    [InlineData("nfff", "ṅ")]
    [InlineData("nffff", "n")]
    [InlineData("nfffff", "ṇ")]
    public void DiacriticCycleForN(string keys, string expected) => Assert.Equal(expected, TextField.Typed(keys));

    [Theory]
    [InlineData("mf", "ṃ")]
    [InlineData("mff", "ṁ")]
    [InlineData("mfff", "m")]
    public void DiacriticCycleForM(string keys, string expected) => Assert.Equal(expected, TextField.Typed(keys));

    [Theory]
    [InlineData("nj", "ñ")]
    [InlineData("nq", "ṅ")]
    public void NasalShortcuts(string keys, string expected) => Assert.Equal(expected, TextField.Typed(keys));

    [Theory]
    [InlineData("sutta")]
    [InlineData("dhamma")]
    [InlineData("anna")]
    [InlineData("uddesa")]
    [InlineData("puggala")]
    public void GeminatesStayIntact(string word) => Assert.Equal(word, TextField.Typed(word));

    [Fact]
    public void GeminateRetroflex() => Assert.Equal("aṭṭha", TextField.Typed("atftfha"));

    [Fact]
    public void ThirdLetterDoesNotUndoLongVowel() => Assert.Equal("āa", TextField.Typed("aaa"));

    [Theory]
    [InlineData("of")]
    [InlineData("ef")]
    public void DiacriticKeyAfterLetterWithoutDiacriticIsTypedAsIs(string keys) =>
        Assert.Equal(keys, TextField.Typed(keys));

    [Theory]
    [InlineData("Af", "Ā")]
    [InlineData("AA", "Ā")]
    [InlineData("Aa", "Ā")]
    [InlineData("aA", "ā")]
    [InlineData("Nf", "Ṇ")]
    [InlineData("Nff", "Ñ")]
    [InlineData("Nfff", "Ṅ")]
    [InlineData("Mff", "Ṁ")]
    public void CaseFollowsTheFirstTypedLetter(string keys, string expected) =>
        Assert.Equal(expected, TextField.Typed(keys));

    [Fact]
    public void LongestRuleWins()
    {
        TelexRuleSet rules = new([new("rf", "x"), new("rrf", "ṝ")]);

        Assert.Equal("ṝ", TextField.Typed("rrf", rules));
        Assert.Equal("ax", TextField.Typed("arf", rules));
    }

    [Fact]
    public void LongerRuleErasesEveryMatchedCharacter()
    {
        InputEngine engine = NewEngine(new TelexRuleSet([new("rrf", "ṝ")]));
        _ = engine.Handle(InputEvent.Typed('r'));
        _ = engine.Handle(InputEvent.Typed('r'));

        Assert.Equal(KeyDecision.Replace(2, "ṝ"), engine.Handle(InputEvent.Typed('f')));
    }

    [Fact]
    public void ReplacementsArePrecomposed()
    {
        // The rule is written decomposed (a + combining macron); the output must still be NFC.
        TelexRuleSet rules = new([new("xf", "ā")]);

        string text = TextField.Typed("xf", rules);

        Assert.Equal("ā", text);
        Assert.True(text.IsNormalized(NormalizationForm.FormC));
        Assert.All(TelexRuleSet.DefaultRules, rule => Assert.True(TextField.Typed(rule.Key).IsNormalized(NormalizationForm.FormC)));
    }

    public static TheoryData<InputEvent> ResetEvents =>
    [
        InputEvent.NavigationKey,
        InputEvent.EditingKey,
        InputEvent.MouseClick,
        InputEvent.ModifiedKeystroke,
    ];

    [Theory]
    [MemberData(nameof(ResetEvents))]
    public void ResetTriggersPreventAReplacement(InputEvent reset)
    {
        string text = new TextField(NewEngine()).Type("a").Press(reset).Type("f").Text;

        Assert.Equal("af", text);
    }

    [Theory]
    [MemberData(nameof(ResetEvents))]
    public void ResetTriggersPassThrough(InputEvent reset) =>
        Assert.Equal(KeyDecision.PassThrough, NewEngine().Handle(reset));

    [Fact]
    public void ResetEndsTheDiacriticCycle()
    {
        string text = new TextField(NewEngine()).Type("nf").Press(InputEvent.NavigationKey).Type("f").Text;

        Assert.Equal("ṇf", text);
    }

    [Fact]
    public void InactivityLongerThanTimeoutResets()
    {
        TextField field = new TextField(NewEngine()).Type("a");
        _clock.Advance(InputEngine.InactivityTimeout + TimeSpan.FromMilliseconds(1));

        Assert.Equal("af", field.Type("f").Text);
    }

    [Fact]
    public void TypingWithinTimeoutStillReplaces()
    {
        TextField field = new TextField(NewEngine()).Type("a");
        _clock.Advance(InputEngine.InactivityTimeout);

        Assert.Equal("ā", field.Type("f").Text);
    }

    [Fact]
    public void StartsInPaliMode() => Assert.Equal(InputMode.Pali, NewEngine().Mode);

    [Fact]
    public void ToggleHotkeyIsSwallowedAndSwitchesToBypassMode()
    {
        InputEngine engine = NewEngine();
        List<InputMode> changes = [];
        engine.ModeChanged += (_, mode) => changes.Add(mode);

        Assert.Equal(KeyDecision.Swallow, engine.Handle(InputEvent.ToggleHotkey));
        Assert.Equal(InputMode.Bypass, engine.Mode);
        Assert.Equal([InputMode.Bypass], changes);
    }

    [Theory]
    [InlineData("af")]
    [InlineData("aa")]
    [InlineData("nf")]
    public void BypassModePassesEverything(string keys)
    {
        InputEngine engine = NewEngine();
        engine.ToggleMode();

        Assert.Equal(keys, new TextField(engine).Type(keys).Text);
    }

    [Fact]
    public void SwitchingBackRestoresTranslation()
    {
        InputEngine engine = NewEngine();
        List<InputMode> changes = [];
        engine.ModeChanged += (_, mode) => changes.Add(mode);

        string text = new TextField(engine).Press(InputEvent.ToggleHotkey).Press(InputEvent.ToggleHotkey).Type("af").Text;

        Assert.Equal("ā", text);
        Assert.Equal([InputMode.Bypass, InputMode.Pali], changes);
    }

    [Fact]
    public void SwitchingModeResetsTheInputBuffer()
    {
        string text = new TextField(NewEngine())
            .Type("a").Press(InputEvent.ToggleHotkey).Press(InputEvent.ToggleHotkey).Type("f").Text;

        Assert.Equal("af", text);
    }

    [Fact]
    public void ToggleModeFromTheTrayFlipsTheMode()
    {
        InputEngine engine = NewEngine();

        engine.ToggleMode();
        Assert.Equal(InputMode.Bypass, engine.Mode);

        engine.ToggleMode();
        Assert.Equal(InputMode.Pali, engine.Mode);
    }

    [Fact]
    public void SettingTheSameModeRaisesNoEvent()
    {
        InputEngine engine = NewEngine();
        int raised = 0;
        engine.ModeChanged += (_, _) => raised++;

        engine.SetMode(InputMode.Pali);

        Assert.Equal(0, raised);
    }
}
