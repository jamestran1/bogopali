# PRD: Project Scaffolding & Configuration Loader (Issue #2)

## Problem Statement

Users need `bogopali` to run with customizable options, such as changing the `Toggle Hotkey` or adding custom Telex conversion rules for Pāli/Sanskrit. However, the application currently lacks any configuration loader, default settings, or initial project scaffolding to write this code in.

## Solution

Initialize the Rust binary project scaffolding. Build a platform-independent configuration loader (`config.rs`) that reads, writes, and parses a local `config.toml` file. It must merge user-defined overrides with default rules and handle errors robustly (e.g., missing files, syntax errors) by falling back to safe defaults.

## User Stories

1. As a Pāli writer, I want the application to load default rules (like `aa` -> `ā`, `ii` -> `ī`, etc.) automatically, so that I don't have to configure anything to start typing standard Pāli.
2. As a Pāli writer, I want the default `Toggle Hotkey` to be `Alt + \``, so that it doesn't conflict with standard Vietnamese typing keys.
3. As a Pāli writer, I want to customize the `Toggle Hotkey` in `config.toml`, so that I can map the mode switch to my preferred key combination.
4. As a Pāli writer, I want to add new rules (e.g. for Sanskrit characters like `sh` -> `ṣ`) or override existing rules in `config.toml`, so that I can adapt the input method to my typing habits.
5. As a Pāli writer, I want the application to automatically create a default `config.toml` file in the application executable's directory if one is missing, so that I have a template file to edit.
6. As a Pāli writer, I want the application to fallback to defaults instead of crashing if `config.toml` has invalid syntax, so that my typing is not interrupted by configuration errors.

## Implementation Decisions

- **Project Scaffolding**: Standard Rust binary package initialized. Dependencies `serde` (with `derive` feature) and `toml` added to `Cargo.toml`.
- **Module `config.rs`**:
  - **Config Schema**: Defines the `Config` struct containing `hotkey` (String) and `rules` (HashMap of input sequence to replacement).
  - **Defaults**: Implements `Default` to return default Pāli hotkey (`Alt + \``) and the 10 default Telex rules.
  - **Path Resolution**: Resolves `config.toml` dynamically relative to the running executable path (`std::env::current_exe()`), falling back to CWD for testing or where path retrieval fails.
  - **File Loading and Merging**: Deserializes the TOML into a partial structure, then merges user overrides into default values.
  - **File Writing**: Serializes and writes default configs back to the resolved path if the file is missing.
  - **Robust Error Handling**: Eprints/logs syntax errors to `stderr`, does not crash (`panic`), and falls back to defaults.

## Testing Decisions

- **Testing Seam**: The public interface of the `Config` module (`Config::load_from_path(path)` and `Config::save_to_path(path)`).
- **Test Strategy**: Integration-style unit tests using real temporary file system paths (e.g. `tempfile` crate or std filesystem API to create temp dirs). No mocking of internal fields.
- **Modules to Test**: `config`
- **Test Cases**:
  - `test_default_config`: Verify default settings have correct keys and values.
  - `test_load_missing_config`: Verify loading from a non-existent path writes default config and returns defaults.
  - `test_load_valid_config_with_overrides`: Verify custom keys are merged, default keys are overridden, and untouched defaults are kept.
  - `test_load_invalid_toml_fallback`: Verify parsing errors result in fallback to defaults without panicking.

## Out of Scope

- A global system settings GUI window.
- Runtime config hot-reloading (config is loaded once at start).

## Further Notes

- Uses standard Rust libraries and cross-platform paths.
- The `ready-for-agent` label is applied to the issue tracker.
