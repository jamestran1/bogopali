using System.Text;

namespace Bogopali.Core;

/// <summary>
/// Turns platform-neutral input events into decisions: let the key through, swallow it, or perform a
/// Replacement. Holds the Input Buffer and the current <see cref="InputMode"/>.
/// </summary>
/// <remarks>
/// The longest Telex Rule matching the end of the Input Buffer wins. After a Replacement the Input Buffer
/// holds only the produced text, so the Diacritic Key can continue the Diacritic Cycle. The case of the
/// output follows the first character of the matched sequence. This type is not thread-safe.
/// </remarks>
public sealed class InputEngine
{
    private readonly TelexRuleSet _rules;
    private readonly TimeProvider _timeProvider;
    private readonly StringBuilder _buffer = new();
    private DateTimeOffset _lastTyped;

    /// <summary>Creates an engine in Pāli Mode.</summary>
    /// <param name="rules">The Telex Rules to apply.</param>
    /// <param name="timeProvider">The clock used for the inactivity timeout.</param>
    public InputEngine(TelexRuleSet rules, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _rules = rules;
        _timeProvider = timeProvider;
    }

    /// <summary>Raised after the mode changes, with the new mode.</summary>
    public event EventHandler<InputMode>? ModeChanged;

    /// <summary>How long a pause in typing may last before the Input Buffer resets.</summary>
    public static TimeSpan InactivityTimeout { get; } = TimeSpan.FromSeconds(2);

    /// <summary>The current mode.</summary>
    public InputMode Mode { get; private set; } = InputMode.Pali;

    /// <summary>Decides what happens to one input event.</summary>
    /// <param name="inputEvent">The event reported by the adapter.</param>
    /// <returns>The decision for the keystroke that caused the event.</returns>
    public KeyDecision Handle(InputEvent inputEvent)
    {
        switch (inputEvent.Kind)
        {
            case InputEventKind.ToggleHotkey:
                ToggleMode();
                return KeyDecision.Swallow;

            case InputEventKind.Character when Mode == InputMode.Pali:
                return HandleCharacter(inputEvent.Character);

            default:
                _buffer.Clear();
                return KeyDecision.PassThrough;
        }
    }

    /// <summary>Switches between Pāli Mode and Bypass Mode and resets the Input Buffer.</summary>
    public void ToggleMode() => SetMode(Mode == InputMode.Pali ? InputMode.Bypass : InputMode.Pali);

    /// <summary>Sets the mode, resetting the Input Buffer when it changes.</summary>
    /// <param name="mode">The new mode.</param>
    public void SetMode(InputMode mode)
    {
        _buffer.Clear();
        if (mode == Mode)
        {
            return;
        }

        Mode = mode;
        ModeChanged?.Invoke(this, mode);
    }

    private KeyDecision HandleCharacter(char character)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (now - _lastTyped > InactivityTimeout)
        {
            _buffer.Clear();
        }

        _lastTyped = now;
        _ = _buffer.Append(character);

        string typed = _buffer.ToString();
        string lowered = typed.ToLowerInvariant();
        for (int length = Math.Min(_rules.MaxSequenceLength, typed.Length); length > 0; length--)
        {
            if (_rules.TryGetOutput(lowered[^length..], out string? output))
            {
                if (char.IsUpper(typed[^length]))
                {
                    output = output.ToUpperInvariant();
                }

                _ = _buffer.Clear().Append(output);
                return KeyDecision.Replace(length - 1, output);
            }
        }

        // Only the last (longest sequence - 1) characters can ever take part in a match.
        int keep = Math.Max(_rules.MaxSequenceLength - 1, 0);
        if (_buffer.Length > keep)
        {
            _ = _buffer.Remove(0, _buffer.Length - keep);
        }

        return KeyDecision.PassThrough;
    }
}
