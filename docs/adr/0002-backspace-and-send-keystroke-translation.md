# Backspace-and-Send Keystroke Translation

We decided to translate Telex-style input sequences into Pāli Unicode characters using the "Backspace-and-Send" mechanism, tracking inputs in an active memory buffer that is cleared on navigation, timeout, or mouse clicks.

## Context
Unlike OS-level IMEs, a hook-based background utility does not have direct access to the text field's internal state or cursor position. To convert a typed sequence (e.g. `aa` into `ā`), the utility must dynamically calculate the number of characters to delete, send simulated `Backspace` events to erase the screen content, and then inject the target Unicode character.

## Consequences
This approach works in almost all text inputs across the operating system (web browsers, text editors, terminal, etc.) without requiring specific application support. However, it requires resetting the input buffer on any cursor-moving keys, mouse clicks, or typing pauses (2-second timeout) to prevent incorrect replacements when the user navigates away from the active input position.
