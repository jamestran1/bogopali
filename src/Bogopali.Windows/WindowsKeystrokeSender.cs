using System.ComponentModel;
using System.Runtime.InteropServices;
using Bogopali.Core;

namespace Bogopali.Windows;

/// <summary>
/// Performs Replacements with <c>SendInput</c>: Backspaces, then the text as Unicode keystrokes. Every
/// injected keystroke carries <see cref="Marker"/> so the input hook can recognize and ignore it.
/// </summary>
public sealed class WindowsKeystrokeSender : IKeystrokeSender
{
    /// <summary>The value placed in <c>dwExtraInfo</c> of every keystroke bogopali injects ("BGPL").</summary>
    internal const nuint Marker = 0x4247504C;

    /// <inheritdoc/>
    public void Send(int eraseCount, string text)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(eraseCount);
        ArgumentNullException.ThrowIfNull(text);

        Input[] inputs = new Input[(eraseCount + text.Length) * 2];
        int index = 0;
        for (int i = 0; i < eraseCount; i++)
        {
            inputs[index++] = Key(VirtualKeys.Back, 0, 0);
            inputs[index++] = Key(VirtualKeys.Back, 0, NativeMethods.KeyEventKeyUp);
        }

        foreach (char character in text)
        {
            inputs[index++] = Key(0, character, NativeMethods.KeyEventUnicode);
            inputs[index++] = Key(0, character, NativeMethods.KeyEventUnicode | NativeMethods.KeyEventKeyUp);
        }

        SendAll(inputs);
    }

    /// <summary>Taps the unassigned menu-mask key so a held Alt or Win is not seen as a lone tap.</summary>
    internal static void SendMenuMask() => SendAll(
    [
        Key(VirtualKeys.MenuMask, 0, 0),
        Key(VirtualKeys.MenuMask, 0, NativeMethods.KeyEventKeyUp),
    ]);

    private static Input Key(int virtualKey, ushort scan, uint flags) => new()
    {
        Type = NativeMethods.InputKeyboard,
        Data = new InputUnion
        {
            Keyboard = new KeyboardInput
            {
                VirtualKey = (ushort)virtualKey,
                ScanCode = scan,
                Flags = flags,
                ExtraInfo = Marker,
            },
        },
    };

    private static unsafe void SendAll(Input[] inputs)
    {
        if (inputs.Length == 0)
        {
            return;
        }

        fixed (Input* pointer = inputs)
        {
            uint sent = NativeMethods.SendInput((uint)inputs.Length, pointer, sizeof(Input));
            if (sent != inputs.Length)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }
    }
}
