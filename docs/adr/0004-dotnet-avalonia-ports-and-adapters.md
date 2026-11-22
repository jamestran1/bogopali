# C# .NET 11 and Avalonia with Ports and Adapters

Supersedes [ADR 0001](0001-rust-native-background-utility.md).

We decided to build bogopali in C# on .NET 11 with Avalonia for the UI, structured as ports and adapters: a platform-neutral Core owns all translation logic and defines the ports (keyboard hook, keystroke sender, autostart), and each operating system gets its own adapter project. Windows is the first and only adapter for now; macOS comes later.

## Context

The primary goal shifted to publishing on the Microsoft Store and making the tool easy for monastics to install. The Store accepts apps in any language, so Rust was not ruled out by the Store itself; the deciding factors were:

- The maintainer works in .NET daily, so delivery and maintenance are faster.
- MSIX packaging (free Store signing, automatic updates, no SmartScreen warning) is first-class in the .NET tooling.
- Very little Rust code existed (a config loader), so switching cost was near zero.

WinUI 3 and WPF were considered but are Windows-only. Avalonia keeps the WPF-style XAML the maintainer prefers, has a built-in cross-platform tray icon, and lets the UI layer carry over to macOS. .NET 11 (STS) was chosen over .NET 10 (LTS) because both reach end of support in November 2028.

## Consequences

- The "~5MB binary" target from ADR 0001 is dropped; a self-contained .NET app is larger, which is acceptable because the Store handles download and updates.
- Configuration moves to the per-user local app data folder, because an MSIX install directory is read-only.
- Autostart uses the MSIX StartupTask for the Store build and the per-user Run registry key for the portable ZIP build.
- The keyboard hook and Replacement remain platform-specific in every adapter; no UI framework removes that. On Linux under Wayland global key capture is not available, so Linux would need an IBus or Fcitx engine rather than an adapter of this kind.
