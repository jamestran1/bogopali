namespace Bogopali.Core;

/// <summary>
/// Port for the global input hook. An adapter captures keyboard and mouse input system-wide, turns it into
/// <see cref="InputEvent"/>s, and applies each returned <see cref="KeyDecision"/> to the keystroke.
/// Adapters must ignore keystrokes that bogopali injected itself.
/// </summary>
public interface IInputHook : IDisposable
{
    /// <summary>Installs the hook.</summary>
    /// <param name="toggleHotkey">The key combination to report as <see cref="InputEventKind.ToggleHotkey"/>.</param>
    /// <param name="handler">
    /// Called for every event, possibly on a dedicated hook thread. The decision for a mouse click is ignored.
    /// </param>
    void Start(Hotkey toggleHotkey, Func<InputEvent, KeyDecision> handler);
}

/// <summary>Port that performs a Replacement in the focused application.</summary>
public interface IKeystrokeSender
{
    /// <summary>Erases characters before the caret and types new text, marking the keystrokes as injected.</summary>
    /// <param name="eraseCount">How many characters to erase.</param>
    /// <param name="text">The text to type.</param>
    void Send(int eraseCount, string text);
}

/// <summary>Whether bogopali starts when the user signs in.</summary>
public enum AutostartState
{
    /// <summary>Startup is off and can be turned on.</summary>
    Disabled,

    /// <summary>Startup is on.</summary>
    Enabled,

    /// <summary>The user turned startup off in the operating system settings; only they can turn it back on there.</summary>
    DisabledByUser,

    /// <summary>A policy turned startup off.</summary>
    DisabledByPolicy,

    /// <summary>A policy turned startup on.</summary>
    EnabledByPolicy,
}

/// <summary>Port for starting bogopali when the user signs in.</summary>
public interface IAutostart
{
    /// <summary>Reads the real startup state, including changes made in the operating system settings.</summary>
    /// <returns>The current state.</returns>
    Task<AutostartState> GetStateAsync();

    /// <summary>Asks for startup to be turned on.</summary>
    /// <returns>The resulting state, which stays off when the user or a policy disabled startup.</returns>
    Task<AutostartState> EnableAsync();

    /// <summary>Turns startup off.</summary>
    /// <returns>The resulting state.</returns>
    Task<AutostartState> DisableAsync();
}
