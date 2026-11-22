using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Bogopali.Core;

namespace Bogopali.Windows;

/// <summary>
/// Global low-level keyboard and mouse hooks. They run on a dedicated thread with its own message loop, so a
/// busy UI thread never delays typing system-wide. Only one instance can be started per process.
/// </summary>
public sealed unsafe class WindowsInputHook : IInputHook
{
    private static WindowsInputHook? s_active;

    private Hotkey _hotkey = Hotkey.Default;
    private Func<InputEvent, KeyDecision> _handler = static _ => KeyDecision.PassThrough;
    private Thread? _thread;
    private uint _threadId;
    private nint _keyboardHook;
    private nint _mouseHook;
    private uint _swallowedHotkeyKey;

    // Caps Lock is tracked here rather than read with GetKeyState on the hook thread, whose own key state
    // need not follow the foreground application. Seeded at start, then flipped on every Caps Lock press.
    private bool _capsLockOn;
    private bool _capsLockHeld;
    private Exception? _startError;

    /// <inheritdoc/>
    public void Start(Hotkey toggleHotkey, Func<InputEvent, KeyDecision> handler)
    {
        ArgumentNullException.ThrowIfNull(toggleHotkey);
        ArgumentNullException.ThrowIfNull(handler);
        if (Interlocked.CompareExchange(ref s_active, this, null) is not null)
        {
            throw new InvalidOperationException("An input hook is already running in this process.");
        }

        _hotkey = toggleHotkey;
        _capsLockOn = (NativeMethods.GetKeyState(VirtualKeys.Capital) & 1) != 0;
        _handler = handler;

        using ManualResetEventSlim started = new();
        _thread = new Thread(() => Run(started))
        {
            IsBackground = true,
            Name = "bogopali input hook",
            Priority = ThreadPriority.AboveNormal,
        };
        _thread.Start();
        started.Wait();

        if (_startError is not null)
        {
            s_active = null;
            throw _startError;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_thread is null)
        {
            return;
        }

        _ = NativeMethods.PostThreadMessageW(_threadId, NativeMethods.WmQuit, 0, 0);
        _ = _thread.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _ = Interlocked.CompareExchange(ref s_active, null, this);
    }

    private void Run(ManualResetEventSlim started)
    {
        _threadId = NativeMethods.GetCurrentThreadId();
        nint module = NativeMethods.GetModuleHandleW(null);
        _keyboardHook = NativeMethods.SetWindowsHookExW(
            NativeMethods.WhKeyboardLowLevel, (nint)(delegate* unmanaged<int, nuint, nint, nint>)&KeyboardProc, module, 0);
        _mouseHook = NativeMethods.SetWindowsHookExW(
            NativeMethods.WhMouseLowLevel, (nint)(delegate* unmanaged<int, nuint, nint, nint>)&MouseProc, module, 0);

        if (_keyboardHook == 0 || _mouseHook == 0)
        {
            _startError = new Win32Exception(Marshal.GetLastPInvokeError(), "Could not install the input hooks.");
            Unhook();
            started.Set();
            return;
        }

        started.Set();

        Message message;
        while (NativeMethods.GetMessageW(&message, 0, 0, 0) > 0)
        {
            // Low-level hooks are called while this thread waits for messages; nothing to dispatch.
        }

        Unhook();
    }

    private void Unhook()
    {
        if (_keyboardHook != 0)
        {
            _ = NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = 0;
        }

        if (_mouseHook != 0)
        {
            _ = NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = 0;
        }
    }

    [UnmanagedCallersOnly]
    private static nint KeyboardProc(int code, nuint message, nint data)
    {
        WindowsInputHook? hook = s_active;
        if (code >= 0 && hook is not null)
        {
            try
            {
                if (hook.OnKey((uint)message, (KeyboardHookData*)data))
                {
                    return 1;
                }
            }
            catch (Exception exception)
            {
                // Never let an exception cross into Windows; the keystroke simply passes through.
                Trace.TraceError($"bogopali keyboard hook: {exception}");
            }
        }

        return NativeMethods.CallNextHookEx(0, code, message, data);
    }

    [UnmanagedCallersOnly]
    private static nint MouseProc(int code, nuint message, nint data)
    {
        WindowsInputHook? hook = s_active;
        if (code >= 0 && hook is not null
            && (uint)message is NativeMethods.WmLeftButtonDown or NativeMethods.WmRightButtonDown
                or NativeMethods.WmMiddleButtonDown or NativeMethods.WmXButtonDown)
        {
            try
            {
                _ = hook._handler(InputEvent.MouseClick);
            }
            catch (Exception exception)
            {
                Trace.TraceError($"bogopali mouse hook: {exception}");
            }
        }

        return NativeMethods.CallNextHookEx(0, code, message, data);
    }

    /// <summary>Handles one keystroke.</summary>
    /// <returns><see langword="true"/> to stop the keystroke from reaching the application.</returns>
    private bool OnKey(uint message, KeyboardHookData* data)
    {
        if ((data->Flags & NativeMethods.LowLevelKeyInjected) != 0 && data->ExtraInfo == WindowsKeystrokeSender.Marker)
        {
            return false;
        }

        uint key = data->VirtualKey;
        if (key == VirtualKeys.Capital)
        {
            TrackCapsLock(message);
        }

        if (message is NativeMethods.WmKeyUp or NativeMethods.WmSysKeyUp)
        {
            if (key == _swallowedHotkeyKey)
            {
                _swallowedHotkeyKey = 0;
                return true;
            }

            return false;
        }

        if (message is not (NativeMethods.WmKeyDown or NativeMethods.WmSysKeyDown) || VirtualKeys.IsModifier(key))
        {
            return false;
        }

        HotkeyModifiers held = HeldModifiers();
        nint layout = NativeMethods.GetKeyboardLayout(
            NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), 0));

        if (IsToggleHotkey(key, held, layout))
        {
            KeyDecision decision = _handler(InputEvent.ToggleHotkey);
            if (decision.Action == KeyAction.PassThrough)
            {
                return false;
            }

            _swallowedHotkeyKey = key;
            if ((held & (HotkeyModifiers.Alt | HotkeyModifiers.Windows)) != 0)
            {
                WindowsKeystrokeSender.SendMenuMask();
            }

            return true;
        }

        InputEvent inputEvent;
        if ((held & (HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Windows)) != 0)
        {
            inputEvent = InputEvent.ModifiedKeystroke;
        }
        else if (VirtualKeys.IsNavigation(key))
        {
            inputEvent = InputEvent.NavigationKey;
        }
        else if (VirtualKeys.IsEditing(key))
        {
            inputEvent = InputEvent.EditingKey;
        }
        else
        {
            char? character = Translate(key, data->ScanCode, held, _capsLockOn, layout);

            // Keys without a character (function keys, dead keys) reset the Input Buffer like editing keys.
            inputEvent = character is { } typed ? InputEvent.Typed(typed) : InputEvent.EditingKey;
        }

        return _handler(inputEvent).Action != KeyAction.PassThrough;
    }

    private bool IsToggleHotkey(uint key, HotkeyModifiers held, nint layout)
    {
        HotkeyModifiers required = _hotkey.Modifiers;
        HotkeyModifiers compared = HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Windows;
        if ((required & HotkeyModifiers.Shift) != 0)
        {
            compared |= HotkeyModifiers.Shift;
        }

        if ((held & compared) != (required & compared))
        {
            return false;
        }

        string name = _hotkey.Key;
        if (name == "Space")
        {
            return key == VirtualKeys.Space;
        }

        if (name.Length > 1)
        {
            // F1 to F24, validated by Hotkey.TryParse.
            return key == VirtualKeys.F1 + int.Parse(name.AsSpan(1), provider: null) - 1;
        }

        if (name == "+" && key == VirtualKeys.Add)
        {
            return true;
        }

        short scan = NativeMethods.VkKeyScanExW(name[0], layout);
        return scan != -1 && key == (uint)(scan & 0xFF);
    }

    private static HotkeyModifiers HeldModifiers()
    {
        HotkeyModifiers held = HotkeyModifiers.None;
        if (IsDown(VirtualKeys.Control))
        {
            held |= HotkeyModifiers.Control;
        }

        if (IsDown(VirtualKeys.Menu))
        {
            held |= HotkeyModifiers.Alt;
        }

        if (IsDown(VirtualKeys.Shift))
        {
            held |= HotkeyModifiers.Shift;
        }

        if (IsDown(VirtualKeys.LeftWindows) || IsDown(VirtualKeys.RightWindows))
        {
            held |= HotkeyModifiers.Windows;
        }

        return held;
    }

    private static bool IsDown(int key) => (NativeMethods.GetAsyncKeyState(key) & 0x8000) != 0;

    private void TrackCapsLock(uint message)
    {
        if (message is NativeMethods.WmKeyDown or NativeMethods.WmSysKeyDown)
        {
            // Auto-repeat sends more key-downs while the key is held; only the first one toggles.
            if (!_capsLockHeld)
            {
                _capsLockOn = !_capsLockOn;
                _capsLockHeld = true;
            }
        }
        else
        {
            _capsLockHeld = false;
        }
    }

    private static char? Translate(uint key, uint scanCode, HotkeyModifiers held, bool capsLockOn, nint layout)
    {
        byte* state = stackalloc byte[256];
        new Span<byte>(state, 256).Clear();
        if ((held & HotkeyModifiers.Shift) != 0)
        {
            state[VirtualKeys.Shift] = 0x80;
        }

        if (capsLockOn)
        {
            state[VirtualKeys.Capital] = 0x01;
        }

        char* buffer = stackalloc char[8];
        int count = NativeMethods.ToUnicodeEx(
            key, scanCode, state, buffer, 8, NativeMethods.ToUnicodeDoNotChangeKeyboardState, layout);

        return count == 1 && !char.IsControl(buffer[0]) ? buffer[0] : null;
    }
}
