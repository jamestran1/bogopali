namespace Bogopali.Core;

/// <summary>
/// Connects an <see cref="InputEngine"/> to an <see cref="IKeystrokeSender"/>: every Replacement the engine
/// decides on is sent. Safe to call from the hook thread and the UI thread at the same time.
/// </summary>
public sealed class TranslationSession
{
    private readonly Lock _lock = new();
    private readonly InputEngine _engine;
    private readonly IKeystrokeSender _sender;

    /// <summary>Creates a session.</summary>
    /// <param name="engine">The engine that makes the decisions.</param>
    /// <param name="sender">The port that performs Replacements.</param>
    public TranslationSession(InputEngine engine, IKeystrokeSender sender)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(sender);

        _engine = engine;
        _sender = sender;
        _engine.ModeChanged += (_, mode) => ModeChanged?.Invoke(this, mode);
    }

    /// <summary>Raised after the mode changes, on the thread that caused the change.</summary>
    public event EventHandler<InputMode>? ModeChanged;

    /// <summary>The current mode.</summary>
    public InputMode Mode
    {
        get
        {
            lock (_lock)
            {
                return _engine.Mode;
            }
        }
    }

    /// <summary>Decides on an event and performs any resulting Replacement.</summary>
    /// <param name="inputEvent">The event from the input hook.</param>
    /// <returns>The decision, which the hook applies to the keystroke.</returns>
    public KeyDecision Handle(InputEvent inputEvent)
    {
        KeyDecision decision;
        lock (_lock)
        {
            decision = _engine.Handle(inputEvent);
        }

        if (decision.Action == KeyAction.Replace)
        {
            _sender.Send(decision.EraseCount, decision.Text);
        }

        return decision;
    }

    /// <summary>Switches between Pāli Mode and Bypass Mode, e.g. from the tray.</summary>
    public void ToggleMode()
    {
        lock (_lock)
        {
            _engine.ToggleMode();
        }
    }
}
