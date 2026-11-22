namespace Bogopali.Core;

/// <summary>Whether typed sequences are translated.</summary>
public enum InputMode
{
    /// <summary>Pāli Mode: matching Telex Rules produce Pāli characters.</summary>
    Pali,

    /// <summary>Bypass Mode: every keystroke reaches the application unchanged.</summary>
    Bypass,
}
