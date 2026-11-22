using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bogopali.Core;

/// <summary>Modifier keys that are part of a <see cref="Hotkey"/>.</summary>
[Flags]
public enum HotkeyModifiers
{
    /// <summary>No modifier.</summary>
    None = 0,

    /// <summary>Ctrl.</summary>
    Control = 1,

    /// <summary>Alt.</summary>
    Alt = 2,

    /// <summary>Shift. When absent, the Shift state is ignored while matching.</summary>
    Shift = 4,

    /// <summary>The Windows key (Command on macOS).</summary>
    Windows = 8,
}

/// <summary>
/// A platform-neutral key combination such as the Toggle Hotkey, written like <c>Alt + `</c>,
/// <c>Ctrl + Alt + p</c> or <c>Ctrl + +</c>.
/// </summary>
/// <param name="Modifiers">The modifiers that must be held.</param>
/// <param name="Key">
/// Either one printable character (letters are lowercase) or a named key: <c>Space</c> or <c>F1</c> to <c>F24</c>.
/// </param>
public sealed record Hotkey(HotkeyModifiers Modifiers, string Key)
{
    /// <summary>The default Toggle Hotkey, <c>Alt + `</c>.</summary>
    public static Hotkey Default { get; } = new(HotkeyModifiers.Alt, "`");

    /// <summary>
    /// Parses a hotkey. Parts are separated by <c>+</c>; a trailing <c>+</c> is the key itself. Modifier names
    /// (<c>Ctrl</c>, <c>Control</c>, <c>Alt</c>, <c>Shift</c>, <c>Win</c>, <c>Windows</c>) are case-insensitive.
    /// A printable key needs Ctrl, Alt or Win so that it never blocks normal typing.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="hotkey">The parsed hotkey.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> is a valid hotkey.</returns>
    public static bool TryParse(string? text, [NotNullWhen(true)] out Hotkey? hotkey)
    {
        hotkey = null;
        string trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return false;
        }

        string keyText;
        string modifierText;
        if (trimmed.EndsWith('+'))
        {
            keyText = "+";
            modifierText = trimmed[..^1].TrimEnd();
            if (modifierText.Length > 0)
            {
                if (!modifierText.EndsWith('+'))
                {
                    return false;
                }

                modifierText = modifierText[..^1];
            }
        }
        else
        {
            int separator = trimmed.LastIndexOf('+');
            keyText = trimmed[(separator + 1)..].Trim();
            modifierText = separator < 0 ? string.Empty : trimmed[..separator];
            if (separator >= 0 && modifierText.Trim().Length == 0)
            {
                return false;
            }
        }

        if (!TryParseModifiers(modifierText, out HotkeyModifiers modifiers) || !TryParseKey(keyText, out string? key))
        {
            return false;
        }

        bool printable = key.Length == 1;
        if (printable && (modifiers & (HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Windows)) == 0)
        {
            return false;
        }

        hotkey = new Hotkey(modifiers, key);
        return true;
    }

    /// <summary>Formats the hotkey in canonical form, e.g. <c>Ctrl + Alt + p</c>.</summary>
    /// <returns>The canonical text, which <see cref="TryParse"/> accepts.</returns>
    public override string ToString()
    {
        List<string> parts = [];
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(Key);
        return string.Join(" + ", parts);
    }

    private static bool TryParseModifiers(string text, out HotkeyModifiers modifiers)
    {
        modifiers = HotkeyModifiers.None;
        if (text.Trim().Length == 0)
        {
            return true;
        }

        foreach (string part in text.Split('+'))
        {
            HotkeyModifiers modifier = part.Trim().ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => HotkeyModifiers.Control,
                "ALT" => HotkeyModifiers.Alt,
                "SHIFT" => HotkeyModifiers.Shift,
                "WIN" or "WINDOWS" => HotkeyModifiers.Windows,
                _ => HotkeyModifiers.None,
            };

            if (modifier == HotkeyModifiers.None || modifiers.HasFlag(modifier))
            {
                return false;
            }

            modifiers |= modifier;
        }

        return true;
    }

    private static bool TryParseKey(string text, [NotNullWhen(true)] out string? key)
    {
        key = null;
        if (text.Length == 1 && !char.IsWhiteSpace(text[0]) && !char.IsControl(text[0]))
        {
            key = text.ToLowerInvariant();
            return true;
        }

        string upper = text.ToUpperInvariant();
        if (upper == "SPACE")
        {
            key = "Space";
            return true;
        }

        if (upper.Length is 2 or 3 && upper[0] == 'F'
            && int.TryParse(upper.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out int number)
            && number is >= 1 and <= 24)
        {
            key = string.Create(CultureInfo.InvariantCulture, $"F{number}");
            return true;
        }

        return false;
    }
}
