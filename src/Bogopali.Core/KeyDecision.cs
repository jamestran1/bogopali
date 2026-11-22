namespace Bogopali.Core;

/// <summary>What the adapter must do with the keystroke that produced a <see cref="KeyDecision"/>.</summary>
public enum KeyAction
{
    /// <summary>Let the keystroke reach the application unchanged.</summary>
    PassThrough,

    /// <summary>Stop the keystroke from reaching the application.</summary>
    Swallow,

    /// <summary>
    /// Stop the keystroke and perform a Replacement: send <see cref="KeyDecision.EraseCount"/>
    /// Backspaces, then type <see cref="KeyDecision.Text"/>.
    /// </summary>
    Replace,
}

/// <summary>The <see cref="InputEngine"/>'s answer to one <see cref="InputEvent"/>.</summary>
/// <param name="Action">What to do with the triggering keystroke.</param>
/// <param name="EraseCount">
/// For <see cref="KeyAction.Replace"/>, the number of characters already in the text field that the
/// Replacement erases. The triggering keystroke itself is swallowed and never counted, so typing
/// <c>af</c> erases one character (the <c>a</c>) and inserts <c>ā</c>.
/// </param>
/// <param name="Text">For <see cref="KeyAction.Replace"/>, the NFC text to insert; otherwise empty.</param>
public readonly record struct KeyDecision(KeyAction Action, int EraseCount, string Text)
{
    /// <summary>Let the keystroke through.</summary>
    public static KeyDecision PassThrough { get; } = new(KeyAction.PassThrough, 0, string.Empty);

    /// <summary>Stop the keystroke without any Replacement.</summary>
    public static KeyDecision Swallow { get; } = new(KeyAction.Swallow, 0, string.Empty);

    /// <summary>Creates a Replacement decision.</summary>
    /// <param name="eraseCount">Characters to erase before inserting.</param>
    /// <param name="text">Text to insert.</param>
    /// <returns>The decision.</returns>
    public static KeyDecision Replace(int eraseCount, string text) => new(KeyAction.Replace, eraseCount, text);
}
