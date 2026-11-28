# Avalonia .NET 11 menu bar app on the Mac App Store

Research for issue #37. Question: can Pāli Key (plain `net11.0` Avalonia app, no `macos` workload) ship on the
Mac App Store as a sandboxed, universal (arm64 + x64), menu-bar-only app on macOS 13+?

Each point is marked **Verified** (primary source linked, or reproduced locally) or **Inference** (reasoned, not
confirmed). Local experiments ran on macOS 27.2.1 (Apple silicon), .NET SDK 11.0.100 (runtime 11.0.0), Avalonia
12.1.6, Xcode 27.2, ad-hoc signed with the App Sandbox entitlement. No distribution certificate was available, so
nothing was uploaded.

## Verdict

**Yes, with one hard constraint: the app must not run on the default multi-file CoreCLR host.** A sandboxed app
published the default way (self-contained, many files next to the apphost) aborts at `[NSApplication
sharedApplication]` because the sandbox denies the `launchservicesd` lookup. Publishing as **NativeAOT**
(recommended) or as **self-contained single-file** avoids the crash, needs no temporary-exception entitlement, and
has shipped on the Mac App Store. Everything else (menu bar item, template icon, no Dock icon, config in the
container, `SMAppService` from P/Invoke, universal binary via `lipo`, `productbuild` + `altool`/Transporter) works
without the `macos` workload. Two App behaviours must change on macOS: `TrayIcon.Clicked` is never raised, and
`StatusIcons` must draw a template image.

## 1. The sandbox crash and how to avoid it

- **Verified (issue):** Avalonia issue [#6529 "Mac App Store sandbox crash"](https://github.com/AvaloniaUI/Avalonia/issues/6529)
  shows the abort: `Process unable to create connection because the sandbox denied the right to lookup
  com.apple.coreservices.launchservicesd ... this application requires an ASN, so aborting`, inside
  `+[NSApplication sharedApplication]` called from `libAvaloniaNative`.
- **Verified (issue):** the workaround entitlement `com.apple.security.temporary-exception.mach-lookup.global-name`
  = `com.apple.coreservices.launchservicesd` makes the app start, but App Review **rejected** it under Guideline
  2.4.5(i) ([comment in #6529](https://github.com/AvaloniaUI/Avalonia/issues/6529)). Do not use it.
- **Verified (issue):** Avalonia maintainer maxkatz6 (June 2026, #6529): "Single file is not a solution, it's a
  requirement." The [Avalonia macOS deployment guide](https://docs.avaloniaui.net/docs/deployment/macos) also says to
  try `PublishSingleFile` for sandbox crashes.
- **Verified (issue):** a commenter (August 2026, #6529) showed the crash is not Avalonia-specific (a bare .NET app
  calling `NSApplication` aborts the same way) and that a **NativeAOT** Avalonia 12.1.0 app runs sandboxed with only
  `app-sandbox`, `application-identifier` and `team-identifier` (no `cs.*` entitlements), and that it **passed App
  Review and is live on the Mac App Store**.
- **Verified (local experiment):** a .NET 11 probe that P/Invokes `[NSApplication sharedApplication]`, bundled,
  ad-hoc signed with `app-sandbox`, launched via `open`:

  | Publish mode | Result |
  | --- | --- |
  | self-contained, multi-file | abort in `_RegisterApplication` (same stack as #6529) |
  | self-contained, `PublishSingleFile=true` | runs |
  | `PublishAot=true` | runs |
  | Avalonia 12.1.6 tray app, multi-file | abort in `InitializeAvnApp` |
  | Avalonia 12.1.6 tray app, single-file, `ShowInDock = false` | runs, creates the tray icon, process type `UIElement` |

- **Inference:** why multi-file fails is unknown (the August 2026 commenter ruled out entitlements, bundle layout,
  JIT and library loading). Single-file statically links the host and runtime into the apphost, which is the obvious
  difference. Treat it as a fixed constraint, not something to debug.

## 2. Building the universal `.app` bundle

- **Verified (Microsoft docs):** a self-contained app always has an appHost, a native Mach-O executable
  ([macOS notarization issues](https://learn.microsoft.com/en-us/dotnet/core/install/macos-notarization-issues)).
  There is no universal RID; publish `osx-arm64` and `osx-x64` separately.
- **Verified (local):** on an Apple silicon Mac, `dotnet publish -r osx-x64 -p:PublishAot=true` cross-compiles, and
  `lipo -create` of the arm64 and x64 executables produces a fat binary whose arm64 slice runs sandboxed. This works
  for both NativeAOT (2.7 MB probe) and single-file (161 MB probe: the whole runtime twice).
- **Inference:** the x64 slice was not run (no Rosetta on the test Mac). Test it on an Intel Mac or with Rosetta.
- **Verified (local):** Avalonia 12.1.6's native libraries `libAvaloniaNative.dylib`, `libSkiaSharp.dylib` and
  `libHarfBuzzSharp.dylib` ship as universal (x86_64 + arm64) in their NuGet packages and stay as separate files next
  to a single-file executable. Copy them once; no `lipo` needed.
- **Verified (Avalonia docs):** layout rules: `Contents/MacOS` should hold only Mach-O executables, `.dylib` files
  belong in `Contents/Frameworks`, resources in `Contents/Resources`, provisioning profile at
  `Contents/embedded.provisionprofile` ([deployment guide](https://docs.avaloniaui.net/docs/deployment/macos)).
  Apple explains the same rules in [Embedding nonstandard code structures in a bundle](https://developer.apple.com/documentation/xcode/embedding-nonstandard-code-structures-in-a-bundle).
- **Inference:** .NET probes for native libraries next to the executable, so moving the dylibs to `Frameworks`
  needs a `NativeLibrary.SetDllImportResolver` (or keep them in `MacOS`, which codesign accepted locally).
- **Verified (local):** in multi-file mode the `.dll` files in `Contents/MacOS` make `codesign` fail ("code object is
  not signed at all ... In subcomponent: System.IO.dll"); single-file and NativeAOT have no `.dll` files to place.

### Info.plist

- **Verified (Apple):** `LSUIElement` = true makes "an agent app that runs in the background and doesn't appear in the
  Dock" ([LSUIElement](https://developer.apple.com/documentation/bundleresources/information-property-list/lsuielement)).
  Avalonia's equivalent runtime switch is `MacOSPlatformOptions.ShowInDock = false`
  ([Avalonia macOS guide](https://docs.avaloniaui.net/docs/platform-specific-guides/macos)). Use both. Locally the probe
  with both reported `type="UIElement"` in `lsappinfo`.
- **Verified (Avalonia docs):** required keys: `CFBundleExecutable`, `CFBundleIdentifier`, `CFBundleName`,
  `CFBundlePackageType` = `APPL`, `CFBundleVersion`, `CFBundleShortVersionString`, `NSHighResolutionCapable`,
  `CFBundleIconFile`. Set `LSMinimumSystemVersion` = `13.0`.
- **Inference (search results only, not an Apple page):** App Store Connect rejects macOS uploads with no compiled
  asset catalog (ITMS-90546). Compile an `AppIcon` set with `xcrun actool` into `Contents/Resources/Assets.car` and add
  `CFBundleIconName`. Also add `LSApplicationCategoryType` (for example `public.app-category.productivity`).

### Entitlements (main executable)

```xml
<key>com.apple.security.app-sandbox</key><true/>
<key>com.apple.application-identifier</key><string>TEAMID.bundle.id</string>
<key>com.apple.developer.team-identifier</key><string>TEAMID</string>
<!-- single-file CoreCLR only, harmless otherwise: -->
<key>com.apple.security.cs.allow-jit</key><true/>
```

- **Verified (Apple):** "To distribute a macOS app through the Mac App Store, you must enable the App Sandbox
  capability" ([App Sandbox](https://developer.apple.com/documentation/security/app-sandbox)).
- **Verified (Apple):** the Hardened Runtime blocks `MAP_JIT` memory without `com.apple.security.cs.allow-jit`
  ([allow-jit](https://developer.apple.com/documentation/bundleresources/entitlements/com.apple.security.cs.allow-jit)),
  and Apple only notarizes apps with the Hardened Runtime
  ([Configuring the hardened runtime](https://developer.apple.com/documentation/xcode/configuring-the-hardened-runtime)).
  Notarization is for Developer ID, not the store, so the Hardened Runtime is optional for the Mac App Store.
- **Verified (Avalonia docs):** "The JIT-compiled code exception is required to run Avalonia apps under hardened
  runtime." Microsoft lists four default .NET entitlements (`allow-jit`, `allow-unsigned-executable-memory`,
  `allow-dyld-environment-variables`, `disable-library-validation`)
  ([Microsoft](https://learn.microsoft.com/en-us/dotnet/core/install/macos-notarization-issues)); for the store only
  `allow-jit` is relevant and only with CoreCLR.
- **Verified (issue #6529, August 2026):** a NativeAOT build needs no `cs.*` entitlement; signing with a certificate the
  embedded profile does not list fails with `Unsatisfied entitlements: com.apple.developer.team-identifier`.
- **Out of scope here:** the keyboard event tap and keystroke injection inside the sandbox (other wayfinder tickets).

### Signing and packaging

- **Verified (Apple DTS, Quinn):** sign inside-out, never pass `--deep`, sign bundles by bundle path, use an `Apple
  Distribution` (or `3rd Party Mac Developer Application`) identity and a Mac App Store distribution provisioning
  profile copied to `Contents/embedded.provisionprofile`
  ([Creating Distribution-Signed Code for Mac](https://developer.apple.com/forums/thread/701514)).
  `--timestamp` and `-o runtime` are tied to Developer ID.
- **Verified (Apple):** the store takes an installer package signed with `3rd Party Mac Developer Installer: <TeamID>`:
  `productbuild --sign <Identity> --component <PathToApp> /Applications <PathToPackage>`; submit it "using either the
  `altool` command-line tool or the Transporter app"
  ([Packaging Mac software for distribution](https://developer.apple.com/documentation/xcode/packaging-mac-software-for-distribution)).
- **Verified (Apple, local Xcode 27.2):** `xcrun altool --validate-app`, `--upload-app -f <.pkg>` and
  `--upload-package <file>` exist; Transporter and the App Store Connect API are also supported
  ([Upload builds](https://developer.apple.com/help/app-store-connect/manage-builds/upload-builds)). `notarytool` is
  not used for the store.
- **Verified (local):** an ad-hoc signed universal single-file bundle passed `codesign --verify --strict`.

## 3. `TrayIcon` on macOS

- **Verified (source):** Avalonia 12.1.6 creates the tray icon as
  `[[NSStatusBar systemStatusBar] statusItemWithLength: NSSquareStatusItemLength]`, attaches the `NativeMenu` with
  `setMenu:`, sets the tooltip on the button, and scales the image to the menu bar font height
  ([trayicon.mm](https://github.com/AvaloniaUI/Avalonia/blob/12.1.6/native/Avalonia.Native/src/OSX/trayicon.mm)).
- **Verified (source):** template images are supported: `MacOSProperties.SetIsTemplateIcon(trayIcon, true)` calls
  `[image setTemplate:]` ([trayicon.mm](https://github.com/AvaloniaUI/Avalonia/blob/12.1.6/native/Avalonia.Native/src/OSX/trayicon.mm),
  [MacOSProperties](https://docs.avaloniaui.net/api/avalonia/controls/macosproperties)).
- **Verified (source):** `TrayIcon.Clicked` "is only supported on Win32 and some Linux DEs, on OSX this event is not
  raised" ([TrayIcon.cs](https://github.com/AvaloniaUI/Avalonia/blob/12.1.6/src/Avalonia.Controls/TrayIcon.cs)).
  Pāli Key toggles the mode on tray click today; on macOS the toggle must live in the menu (already present) and on
  the hotkey.
- **Verified (local):** the sandboxed single-file Avalonia probe with no window, `ShutdownMode.OnExplicitShutdown` and
  `ShowInDock = false` created a template tray icon with a menu and kept running. The menu bar item was not inspected
  visually.
- **Inference:** `StatusIcons` draws a coloured rounded square with a "Segoe UI" letter. As a template image only the
  alpha channel counts, so macOS needs a separate rendering: an opaque glyph (`ā` / `a`) on a transparent background,
  with a font present on macOS (for example the system font).

## 4. Configuration inside the sandbox

- **Verified (Microsoft):** since .NET 8, `LocalApplicationData` and `ApplicationData` on macOS both map to
  `NSApplicationSupportDirectory` (`Library/Application Support`)
  ([.NET 8 breaking change](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/8.0/getfolderpath-unix)).
- **Verified (Apple):** a sandboxed app gets a container in `~/Library/Containers` with full read and write access
  ([Accessing files from the macOS App Sandbox](https://developer.apple.com/documentation/security/accessing-files-from-the-macos-app-sandbox)).
- **Verified (local):** in the sandbox `HOME` is `~/Library/Containers/<bundle-id>/Data` and
  `Environment.GetFolderPath(LocalApplicationData)` returns
  `~/Library/Containers/<bundle-id>/Data/Library/Application Support`.
- **Conclusion:** `ConfigStore` needs no change; it already takes the directory from the adapter. The macOS adapter
  passes `Path.Combine(GetFolderPath(LocalApplicationData), "bogopali")`.
- **Inference:** "Open configuration file" should use Avalonia's `ILauncher` (NSWorkspace) rather than spawning
  `/usr/bin/open`; a sandboxed app may open files in its own container with the default editor, but this was not tested.

## 5. Start at login with `SMAppService.mainApp`

- **Verified (Apple):** `SMAppService.mainApp` is "an app service object that corresponds to the main application as
  a login item"; for the main app `register()` means "the application launches on subsequent logins"
  ([mainApp](https://developer.apple.com/documentation/servicemanagement/smappservice/mainapp),
  [register()](https://developer.apple.com/documentation/servicemanagement/smappservice/register())). Available from
  macOS 13.
- **Verified (SDK header `SMAppService.h`):** raw values `NotRegistered = 0`, `Enabled = 1`, `RequiresApproval = 2`,
  `NotFound = 3`.
- **Verified (local):** from .NET 11 without the `macos` workload, `objc_getClass("SMAppService")`,
  `mainAppService` and `status` via `objc_msgSend` P/Invoke into `/usr/lib/libobjc.A.dylib` work inside the sandbox
  (after `dlopen` of `ServiceManagement.framework`). The ad-hoc signed probe run from a scratch folder reported
  `NotFound`; `register` was not tried. Use `[LibraryImport]` so it stays NativeAOT-compatible.

| `SMAppService.Status` | Apple's meaning | `AutostartState` |
| --- | --- | --- |
| `enabled` | "successfully registered and is eligible to run" | `Enabled` |
| `notRegistered` | "hasn't registered" | `Disabled` |
| `requiresApproval` | registered but "the user needs to take action in System Settings"; also returned "if the user revokes consent" | `DisabledByUser` (offer `SMAppService.openSystemSettingsLoginItems()`) |
| `notFound` | "the framework couldn't find this service" | `Disabled` (log it; Inference) |

Sources: [status enum](https://developer.apple.com/documentation/servicemanagement/smappservice/status-swift.enum),
[requiresApproval](https://developer.apple.com/documentation/servicemanagement/smappservice/status-swift.enum/requiresapproval),
[notFound](https://developer.apple.com/documentation/servicemanagement/smappservice/status-swift.enum/notfound).

- **Inference:** `DisabledByPolicy` and `EnabledByPolicy` are never produced on macOS; `SMAppService` exposes no
  policy status. Expect `notFound` for unsigned or misplaced builds, so test login items with a signed build in
  `/Applications`.

## Recipe

1. macOS head project: `net11.0`, Avalonia, `MacOSPlatformOptions { ShowInDock = false }`, menu-only tray with
   `MacOSProperties.SetIsTemplateIcon`, `PublishAot=true` (fallback: `SelfContained` + `PublishSingleFile=true`,
   without `IncludeNativeLibrariesForSelfExtract`).
2. `dotnet publish -c Release -r osx-arm64` and `-r osx-x64` (both work on an Apple silicon Mac).
3. `lipo -create` the two executables into `Pāli Key.app/Contents/MacOS/<exe>`; copy the three universal Avalonia
   dylibs; add `Info.plist` (`LSUIElement`, `LSMinimumSystemVersion` 13.0, category), `Assets.car`, and
   `embedded.provisionprofile`.
4. `codesign --force --sign "Apple Distribution: ..."` each dylib, then the bundle with the entitlements above. No
   `--deep`.
5. `productbuild --sign "3rd Party Mac Developer Installer: ..." --component "Pāli Key.app" /Applications PaliKey.pkg`.
6. `xcrun altool --validate-app` then `--upload-app -f PaliKey.pkg` (or Transporter).
7. Before upload, run the signed app from `/Applications` and check Console for sandbox denials.

## Open risks

- The event tap and key injection under the sandbox are the real Mac App Store risk; they are covered by other tickets.
- NativeAOT needs the App and its dependencies (Tomlyn, compiled bindings) to be trim- and AOT-clean; the build already
  treats warnings as errors, which will surface problems. If AOT is not clean, single-file is the fallback.
- Not reproduced end to end: no store certificate, profile, or App Store Connect upload was available.
