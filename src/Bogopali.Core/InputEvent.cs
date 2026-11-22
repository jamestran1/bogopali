namespace Bogopali.Core;

/// <summary>The kind of a platform-neutral <see cref="InputEvent"/>.</summary>
public enum InputEventKind
{
    /// <summary>A printable character was typed, with its case.</summary>
    Character,

    /// <summary>A key that moves the caret: an arrow key, Home, End, Page Up or Page Down.</summary>
    NavigationKey,

    /// <summary>A key that edits or leaves the text: Enter, Tab, Escape, Backspace or Delete.</summary>
    EditingKey,

    /// <summary>A mouse button was pressed anywhere on the screen.</summary>
    MouseClick,

    /// <summary>A key was pressed while Ctrl, Alt or Win was held down (a shortcut).</summary>
    ModifiedKeystroke,

    /// <summary>The Toggle Hotkey was pressed.</summary>
    ToggleHotkey,
}

/// <summary>
/// A platform-neutral keyboard or mouse event that an adapter reports to the <see cref="InputEngine"/>.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="Character">The typed character when <paramref name="Kind"/> is <see cref="InputEventKind.Character"/>; otherwise <c>'\0'</c>.</param>
public readonly record struct InputEvent(InputEventKind Kind, char Character = '\0')
{
    /// <summary>A navigation key (arrows, Home, End, Page Up, Page Down).</summary>
    public static InputEvent NavigationKey { get; } = new(InputEventKind.NavigationKey);

    /// <summary>An editing key (Enter, Tab, Escape, Backspace, Delete).</summary>
    public static InputEvent EditingKey { get; } = new(InputEventKind.EditingKey);

    /// <summary>A mouse click.</summary>
    public static InputEvent MouseClick { get; } = new(InputEventKind.MouseClick);

    /// <summary>A keystroke made with Ctrl, Alt or Win held down.</summary>
    public static InputEvent ModifiedKeystroke { get; } = new(InputEventKind.ModifiedKeystroke);

    /// <summary>The Toggle Hotkey.</summary>
    public static InputEvent ToggleHotkey { get; } = new(InputEventKind.ToggleHotkey);

    /// <summary>Creates an event for a typed character.</summary>
    /// <param name="character">The character as typed, including its case.</param>
    /// <returns>The event.</returns>
    public static InputEvent Typed(char character) => new(InputEventKind.Character, character);
}
