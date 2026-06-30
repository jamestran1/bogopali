# Product Requirement Document (PRD): bogopali

## Problem Statement

Users who study, write, or translate Pāli texts using Romanized transliteration (containing diacritics like ā, ī, ū, ḍ, ḷ, ṃ, ṅ, ñ, ṇ, ṭ) on Windows and macOS do not have a lightweight, dedicated, cross-platform Telex-style input method. Current options either require installing complex third-party input method engines (like RIME), building complicated custom keyboard layouts, or manually copying/pasting symbols, which slows down writing and creates friction. Furthermore, standard Vietnamese input methods (like UniKey/EVKey) conflict with Pāli characters (e.g., typing `aa` outputs `â` instead of `ā`), requiring users to constantly toggle their layouts.

## Solution

Build `bogopali` as a lightweight (~5MB), standalone, background utility for Windows and macOS. It will capture keyboard inputs globally and automatically convert Telex-style sequences (e.g., `aa` to `ā`, `dd` to `ḍ`, `nj` to `ñ`) using a "Backspace-and-Send" virtual keystroke simulation. It will live in the system tray (Windows) or menu bar (macOS) with a simple visual indicator showing whether it is active (Pāli Mode) or bypassed (Bypass Mode), toggled easily via a customizable hotkey (defaulting to `Alt + \``) to prevent conflicts with Vietnamese keyboards.

## User Stories

1. As a Pāli writer, I want to type `aa`, `ii`, and `uu` to quickly input the long vowels `ā`, `ī`, and `ū`, so that I don't have to copy-paste diacritics.
2. As a Pāli writer, I want to type double consonants like `dd`, `tt`, `nn`, `ll`, and `mm` to input retroflex letters `ḍ`, `ṭ`, `ṇ`, `ḷ` and the niggahīta `ṃ`, so that my typing flow remains fast and natural.
3. As a Pāli writer, I want to type `nj` to get `ñ` and `nq` to get `ṅ`, so that I can easily type palatal and velar nasals.
4. As a Pāli writer, I want the utility to respect capitalization (e.g., `AA` -> `Ā`, `Aa` -> `Ā`, `aa` -> `ā`), so that proper nouns and sentence starters are correctly capitalized.
5. As a Pāli writer, I want to toggle between Pāli Mode and Bypass Mode using the `Alt + \`` hotkey, so that I can switch modes rapidly without opening any window.
6. As a Pāli writer, I want to see a clear status indicator icon (e.g., "P" for Pāli Mode, "B" for Bypass Mode) in the system tray or menu bar, so that I know which mode is currently active.
7. As a Pāli writer, I want the input buffer to reset immediately when I click the mouse or press arrow keys, so that the utility does not incorrectly replace characters when I navigate or edit different parts of the text.
8. As a Pāli writer, I want the input buffer to reset after a 2-second timeout of inactivity, so that pauses in my typing do not lead to accidental character conversions.
9. As a Pāli writer, I want to customize the hotkey and conversion rules in a local `config.toml` file, so that I can adapt the utility to my personal habits or add Sanskrit characters.
10. As a Pāli writer, I want the application to start automatically when my computer boots, so that I don't have to manually launch it every time.
11. As a Pāli writer, I want to download and run the application as a portable ZIP file, so that I don't need administrator privileges to install it.

## Implementation Decisions

### Modules to Build
*   **Config (`config.rs`)**: Reads, writes, and parses the `config.toml` file, merging user overrides with default rules.
*   **Translator (`translator.rs`)**: Stores the temporary typing buffer and matches it against active conversion rules. This module is platform-independent.
*   **OS Hook (`os/mod.rs`, `os/windows.rs`, `os/macos.rs`)**:
    *   *Windows*: Implements Win32 low-level keyboard hook (`WH_KEYBOARD_LL`) and tray icon using Win32 API.
    *   *macOS*: Implements CoreGraphics event tap (`CGEventTap`) and menu bar status item.
    *   Injects virtual keystrokes (Backspace and Unicode string inputs) when notified by the translator.
    *   Detects mouse clicks globally to reset the translator buffer.
    *   Registers/unregisters the application for OS autostart.

### Testing Seam
The primary testing seam is the boundary between the `Translator` and the OS event hook loop. The `Translator` will accept incoming raw character keystrokes and output a list of actions (`Action::Bypass`, `Action::Replace { backspaces: usize, value: String }`). This allows us to unit test the entire Telex conversion logic, capitalization rules, buffer timeout, and navigation key resets without relying on OS-level hooks or simulating real mouse/keyboard events.

## Testing Decisions

### What Makes a Good Test
A good test verifies that typing sequences result in the correct target output actions (e.g. typing `a` then `a` generates `Action::Replace { backspaces: 1, value: "ā" }`) and that typing buffer clears under navigation and mouse actions. Tests should focus on the public behavior of the `Translator` engine rather than internal state representation.

### Modules to Test
*   **`translator`**: We will write comprehensive unit tests covering:
    *   Default Telex conversions (`aa` -> `ā`, `dd` -> `ḍ`, etc.).
    *   Capitalization cases (`AA`, `Aa`, `aA`).
    *   Buffer resets on non-character keys (Backspace, Enter, Arrows, Escape).
    *   Buffer resets on mouse clicks and timeouts.
*   **`config`**: Unit tests verifying that custom rules override default rules, and invalid TOML formats fallback safely to defaults.

## Out of Scope
*   **GUI Settings Window**: No dedicated settings window will be built. Configuration changes must be made via the `config.toml` file, which can be opened directly from the tray/menu bar context menu.
*   **Vietnamese Telex engine**: This utility does not support typing Vietnamese; it is strictly a Pāli input method. Users must use UniKey/EVKey for Vietnamese.
*   **Mobile Platforms**: iOS and Android support are out of scope for this utility (handled separately by RIME-based diacritics keyboards).

## Further Notes
Users should be instructed to switch their Vietnamese input engines (UniKey, EVKey, OpenKey, or built-in macOS Telex) to English mode when typing Pāli in `bogopali` to prevent hook conflicts.
