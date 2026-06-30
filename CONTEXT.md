# bogopali

A lightweight, cross-platform standalone typing utility for Windows and macOS. It runs in the background and intercepts keyboard input to translate Telex-style sequences into Pāli Unicode characters with diacritics.

## Language

**Pāli Mode**:
The active state of the utility where typed sequences are intercepted, stored in the Input Buffer, and converted into Pāli Unicode characters.
_Avoid_: Active state, parsing mode

**Bypass Mode**:
The inactive state of the utility where all keystrokes are passed through directly to the operating system without processing.
_Avoid_: Inactive state, passive mode, english mode

**Input Buffer**:
A temporary queue of recently typed characters kept in memory, used to detect conversion patterns and calculate the number of backspaces needed to replace the sequence.
_Avoid_: Buffer, key history, keyboard stack

**Toggle Hotkey**:
The configurable keyboard shortcut (defaulting to `Alt + \``) used to switch the utility between Pāli Mode and Bypass Mode.
_Avoid_: Shortcut, switcher, toggler

**Status Indicator**:
The visual representation (System Tray Icon on Windows, Menu Bar Icon on macOS) showing the current active mode.
_Avoid_: Icon, tray menu

**Hook Conflict**:
A situation where multiple input utilities (like bogopali and UniKey/EVKey) register global keyboard hooks simultaneously, causing key capture contention.
_Avoid_: Keyboard crash, hook clash
