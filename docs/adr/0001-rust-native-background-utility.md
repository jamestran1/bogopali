# Standalone Background Utility in Rust

Status: superseded by [ADR 0004](0004-dotnet-avalonia-ports-and-adapters.md).

We decided to build `bogopali` as a native standalone background utility written in Rust, using native operating system event hooks (Win32 keyboard hooks for Windows and CoreGraphics Event Taps for macOS).

## Context
We need a cross-platform input method for Pāli that supports Windows and macOS. The alternative was writing OS-level Input Method Editors (IMEs) (such as TSF for Windows and InputMethodKit for macOS) or creating native layout files, but this carries high implementation complexity and requires separate codebases, installer setups, and admin privileges.

## Consequences
Using Rust allows us to build a single, lightweight binary (~5MB) with near-zero memory footprint and zero external runtime dependencies. However, because it hooks global keyboard inputs, the utility must be granted Accessibility permissions on macOS, and users must switch Vietnamese keyboards like UniKey/EVKey to English mode to avoid Hook Conflicts.
