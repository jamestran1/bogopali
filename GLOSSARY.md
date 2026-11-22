# Pāli Key (bogopali)

A lightweight standalone background utility for Windows and macOS that turns Telex-style sequences into Romanized Pāli characters with diacritics, in any text field.

## Modes

**Pāli Mode**:
The state in which typed sequences are watched and matching Telex Rules produce Pāli characters.
_Avoid_: Active state, Active Mode, Translation Mode, parsing mode

**Bypass Mode**:
The state in which every keystroke reaches the operating system unchanged.
_Avoid_: Inactive state, Disabled Mode, Off Mode, english mode

**Toggle Hotkey**:
The user-configurable key combination (default `Alt + \``) that switches between Pāli Mode and Bypass Mode.
_Avoid_: Hotkey, Shortcut, Toggle key, switcher

**Status Indicator**:
The tray icon (Windows) or menu bar item (macOS) that shows which mode is active.
_Avoid_: Icon, tray menu

## Translation

**Telex Rule**:
A mapping from a typed sequence (e.g. `aa`) to the Pāli character it produces (e.g. `ā`).
_Avoid_: Conversion rule, Mapping, Translation macro

**Diacritic Key**:
The single key (`f`) typed right after a base letter to give it its diacritic, e.g. `af` gives `ā`.
_Avoid_: Marker, modifier key, tone key, dấu key

**Diacritic Cycle**:
Pressing the Diacritic Key again on a letter with several diacritic forms to step to the next form, ending back at the plain letter (e.g. `n` → `ṇ` → `ñ` → `ṅ` → `n`).
_Avoid_: Rotation, toggle, multi-tap

**Niggahīta**:
The Pāli nasal sign, written `ṃ` by default and `ṁ` as its alternative form.
_Avoid_: Anusvāra, nasal dot

**Input Buffer**:
The characters typed since the last reset, matched against Telex Rules to decide whether a Replacement happens.
_Avoid_: Buffer, Typing buffer, Key history, Input queue, keyboard stack

**Replacement**:
The act of erasing the matched typed characters from the text field and putting the Pāli character in their place.
_Avoid_: Injection, Backspace-and-Send action, conversion

## Environment

**Hook Conflict**:
Contention that occurs when Pāli Key and another input utility (such as UniKey or EVKey) both capture global keyboard input at the same time.
_Avoid_: Keyboard crash, hook clash
