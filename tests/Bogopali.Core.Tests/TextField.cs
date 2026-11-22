using System.Text;

namespace Bogopali.Core.Tests;

/// <summary>
/// Simulates a text field that receives keystrokes through an <see cref="InputEngine"/>, applying each
/// decision the way an adapter would.
/// </summary>
internal sealed class TextField(InputEngine engine)
{
    private readonly StringBuilder _text = new();

    public string Text => _text.ToString();

    public TextField Type(string keys)
    {
        foreach (char key in keys)
        {
            KeyDecision decision = engine.Handle(InputEvent.Typed(key));
            switch (decision.Action)
            {
                case KeyAction.PassThrough:
                    _ = _text.Append(key);
                    break;
                case KeyAction.Replace:
                    _ = _text.Remove(_text.Length - decision.EraseCount, decision.EraseCount).Append(decision.Text);
                    break;
                default:
                    break;
            }
        }

        return this;
    }

    public TextField Press(InputEvent inputEvent)
    {
        _ = engine.Handle(inputEvent);
        return this;
    }

    public static string Typed(string keys, TelexRuleSet? rules = null) =>
        new TextField(new InputEngine(rules ?? TelexRuleSet.Default, new FakeTimeProvider())).Type(keys).Text;
}
