namespace Bogopali.Windows;

/// <summary>Windows virtual-key codes used by the adapter.</summary>
internal static class VirtualKeys
{
    public const int Back = 0x08;
    public const int Tab = 0x09;
    public const int Return = 0x0D;
    public const int Shift = 0x10;
    public const int Control = 0x11;
    public const int Menu = 0x12;
    public const int Pause = 0x13;
    public const int Capital = 0x14;
    public const int Escape = 0x1B;
    public const int Space = 0x20;
    public const int Prior = 0x21;
    public const int Next = 0x22;
    public const int End = 0x23;
    public const int Home = 0x24;
    public const int Left = 0x25;
    public const int Up = 0x26;
    public const int Right = 0x27;
    public const int Down = 0x28;
    public const int Insert = 0x2D;
    public const int Delete = 0x2E;
    public const int LeftWindows = 0x5B;
    public const int RightWindows = 0x5C;
    public const int Add = 0x6B;
    public const int F1 = 0x70;
    public const int NumLock = 0x90;
    public const int Scroll = 0x91;
    public const int LeftShift = 0xA0;
    public const int RightShift = 0xA1;
    public const int LeftControl = 0xA2;
    public const int RightControl = 0xA3;
    public const int LeftMenu = 0xA4;
    public const int RightMenu = 0xA5;

    // An unassigned key code. Injected between Alt or Win and its release after a swallowed hotkey,
    // so the application does not see a lone Alt tap (which opens its menu bar) or Win tap (Start menu).
    public const int MenuMask = 0xE8;

    public static bool IsModifier(uint key) => key is Shift or Control or Menu or LeftWindows or RightWindows
        or Capital or NumLock or Scroll
        or LeftShift or RightShift or LeftControl or RightControl or LeftMenu or RightMenu;

    public static bool IsNavigation(uint key) => key is Left or Up or Right or Down or Home or End or Prior or Next;

    public static bool IsEditing(uint key) => key is Return or Tab or Escape or Back or Delete or Insert;
}
