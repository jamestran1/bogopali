# bogopali

A small background utility for Windows that lets you type Romanized Pāli with diacritics (ā ī ū ṭ ḍ ṇ ñ ṅ ḷ ṃ) in any application: Word, browsers, Notepad, chat apps.

Type the letter, then `f` for the diacritic.

## Typing

| Type | Get | Notes |
| --- | --- | --- |
| `af` or `aa` | ā | also `if`/`ii` → ī, `uf`/`uu` → ū |
| `tf`, `df`, `lf` | ṭ, ḍ, ḷ | |
| `mf` | ṃ | press `f` again for ṁ, again for m |
| `nf` | ṇ | press `f` again for ñ, then ṅ, then n |
| `nj`, `nq` | ñ, ṅ | shortcuts |
| `Af`, `Aa`, `AA` | Ā | the first letter decides the case |

Doubled consonants stay as typed, so *sutta*, *dhamma* and *anna* need nothing special. *aṭṭha* is `atftfha`.

The Toggle Hotkey `Alt + \`` switches between **Pāli Mode** (tray icon `ā`) and **Bypass Mode** (tray icon `a`), where every key reaches the application unchanged. Use Bypass Mode for English, where words such as *if* or *half* would otherwise change. Left-clicking the tray icon also switches the mode.

The tray menu (right click) offers **Pāli Mode**, **Start with Windows**, **Open configuration file** and **Exit**. It appears in Vietnamese when the Windows display language is Vietnamese, and in English otherwise.

## Installation

### Microsoft Store (recommended)

Install bogopali from the Microsoft Store. It needs no admin rights and updates automatically.

### Portable ZIP

1. Download `bogopali-<version>-win-x64.zip` from [GitHub Releases](https://github.com/jamestran1/bogopali/releases).
2. Extract it anywhere you can write to, for example your Documents folder, and run `bogopali.exe`. No installation or admin rights are needed.

The ZIP build is not code-signed, so Windows SmartScreen may say *"Windows protected your PC"* the first time. This is expected: choose **More info**, then **Run anyway**.

## Using bogopali with UniKey or EVKey

Vietnamese input tools also capture the keyboard, and two tools rewriting the same keystrokes conflict (a Hook Conflict). While typing Pāli, switch UniKey or EVKey to English mode (usually `Ctrl + Shift` or `Alt + Z`). Switch bogopali to Bypass Mode when you go back to typing Vietnamese.

## Configuration

On first start bogopali writes `config.toml` to `%LOCALAPPDATA%\bogopali` (the Store build keeps it in its own app data folder). Choose **Open configuration file** in the tray menu to edit it; changes apply the next time bogopali starts.

```toml
toggle_hotkey = "Ctrl + Alt + p"   # e.g. "Alt + `", "Ctrl + +", "Ctrl + F12"

[rules]
sf = "ṣ"   # add a rule
aa = ""    # disable a default rule
```

Rules are merged over the defaults and the longest matching sequence wins. If the file cannot be read, bogopali tells you and runs with the defaults.

## Development

Requires the .NET 11 SDK. See [AGENTS.md](AGENTS.md) for build and test commands, [GLOSSARY.md](GLOSSARY.md) for the domain language and [docs/adr](docs/adr) for the architectural decisions. The specification is issue [#9](https://github.com/jamestran1/bogopali/issues/9).

### Releasing

Pushing a tag such as `v0.1.0` runs `.github/workflows/release.yml`, which builds an unsigned MSIX and the portable ZIP with `packaging/build-release.ps1` (PowerShell 7 and the Windows SDK) and attaches both to a GitHub Release.

- Before the first Store submission, reserve the app in Partner Center and set the repository variables `MSIX_PACKAGE_NAME`, `MSIX_PUBLISHER` and `MSIX_PUBLISHER_DISPLAY_NAME` to its Product identity values. The first submission is uploaded manually; the Store signs the package.
- To sideload the MSIX for testing, sign it with a trusted certificate whose subject matches `MSIX_PUBLISHER`, for example with `signtool sign /fd SHA256 /f test.pfx bogopali-<version>-win-x64.msix`.
