namespace Bogopali.Core.Tests;

public sealed class TranslationSessionTests
{
    private sealed class RecordingSender : IKeystrokeSender
    {
        public List<(int EraseCount, string Text)> Sent { get; } = [];

        public void Send(int eraseCount, string text) => Sent.Add((eraseCount, text));
    }

    private readonly RecordingSender _sender = new();

    private TranslationSession NewSession() =>
        new(new InputEngine(TelexRuleSet.Default, new FakeTimeProvider()), _sender);

    [Fact]
    public void ReplacementIsSentAndTheKeyIsNotPassedThrough()
    {
        TranslationSession session = NewSession();

        Assert.Equal(KeyAction.PassThrough, session.Handle(InputEvent.Typed('a')).Action);
        Assert.Equal(KeyAction.Replace, session.Handle(InputEvent.Typed('f')).Action);
        Assert.Equal([(1, "ā")], _sender.Sent);
    }

    [Fact]
    public void PassThroughSendsNothing()
    {
        TranslationSession session = NewSession();

        _ = session.Handle(InputEvent.Typed('h'));
        _ = session.Handle(InputEvent.Typed('i'));

        Assert.Empty(_sender.Sent);
    }

    [Fact]
    public void ToggleModeReportsTheNewMode()
    {
        TranslationSession session = NewSession();
        List<InputMode> changes = [];
        session.ModeChanged += (_, mode) => changes.Add(mode);

        session.ToggleMode();

        Assert.Equal(InputMode.Bypass, session.Mode);
        Assert.Equal([InputMode.Bypass], changes);
        _ = session.Handle(InputEvent.Typed('a'));
        _ = session.Handle(InputEvent.Typed('f'));
        Assert.Empty(_sender.Sent);
    }
}
