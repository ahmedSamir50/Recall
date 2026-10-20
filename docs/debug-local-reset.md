Status: debug-only, 15 Oct 2026. Entry: [index.md](index.md).

# Debug local state reset

Status: developer aid for **Debug** builds only. Not part of the v1 user-facing spec.

Use this when you need a **first-run** shell again: empty folder grants, onboarding welcome, no downloaded models, and a fresh device record on the next access.

## Where it lives in the app

1. Run **Recall.Maui** with **Debug** configuration.
2. Open **Preferences** (`/preferences`).
3. At the bottom, section **Debug only** → **Reset all local data**.

The control is hidden in **Release** builds (`IDebugAppReset.IsEnabled` is false).

## What the reset does

`MauiDebugAppReset` (`src/Recall.Maui/Platform/Debug/MauiDebugAppReset.cs`) runs in this order:

| Step | Action |
|------|--------|
| Cancel download | Stops an in-flight text model download. |
| Model files | Clears in-memory download state and deletes files under the configured model directory. |
| Preferences | `Preferences.Default.Clear()` — grants, theme, locale, age cutoff, device record JSON, onboarding completion flag, and every other `recall.*` key. |
| Secure storage | `SecureStorage.RemoveAll()` (v1 uses almost no keys; safe no-op when empty). |
| App data | Deletes and recreates `FileSystem.AppDataDirectory` (vectors, op log, partial downloads, secrets file, etc.). |
| Cache | Same for `FileSystem.CacheDirectory`. |
| Onboarding flag | `IOnboardingCompletionStore.ResetToOnboarding()` — treated as incomplete until **Ready** finishes. |
| Device cache | `DeviceRecordService.InvalidateCache()` so the next read creates a new stored id if needed. |

After reset, the UI:

- Resets theme to **System** and locale to **English** in `ShellUiStore`.
- Reloads grants from the (now empty) scope store.
- Navigates to **`/onboarding/welcome`** with `forceLoad: true`.

## Onboarding without the reset button

Even on a normal **first install**, onboarding is not complete until the user taps **Start** on the **Ready** step (`/onboarding/ready`), which calls `IOnboardingCompletionStore.MarkComplete()`.

`BootstrapRedirect` (`src/Recall.UI/Components/Shell/BootstrapRedirect.razor.cs`) runs on every shell load:

- If onboarding is **not** complete and the current route is **not** under `/onboarding` or `/landing`, navigation is replaced with **`/onboarding/welcome`**.

So a clean profile or a debug reset both land on welcome before the main shell routes (`/`, `/folders`, `/models`, …).

**Phase 1 close evidence:** use **`/onboarding/folders`** with **no grants**, **Continue** disabled, and copy **Pick at least one folder**. That is the onboarding folder step, not library **Folders** (`/folders`), which uses the same `FolderGrantPanel` but without onboarding **Back** / **Continue**.

## Ports and registration

| Type | Location |
|------|----------|
| `IOnboardingCompletionStore` | `src/Recall.Core/Ports/OnboardingPorts.cs` — Maui: `MauiOnboardingCompletionStore`, preference key `recall.onboarding.complete`. |
| `IDebugAppReset` | `src/Recall.Core/Ports/DebugPorts.cs` — Debug: `MauiDebugAppReset`; Release: `DebugAppResetDisabled`. |
| DI | `MauiProgram.cs` (`#if DEBUG`), `AddRecallMauiPlatform` registers the onboarding store on all configurations. |

Unit test fakes: `FakeOnboardingCompletionStore`, `FakeDebugAppReset` in `tests/Recall.UnitTests/Fakes/PortFakes.cs`.

## Blazor WebView devtools (optional)

Debug builds register `AddBlazorWebViewDeveloperTools()`. **F12** in the MAUI window opens WebView2 devtools. You can still navigate manually, for example:

```javascript
location.href = '/onboarding/folders';
```

Prefer **Reset all local data** or the bootstrap redirect for a full ground-zero state.

## Android Debug deploy

The Pixel emulator in Device Manager is **x86_64**. An arm64-only APK is started under **Berberis** (ARM translation). That path loads `libmonosgen`, then the debugger waits on `10.0.2.2`, then the process is dumped while the .NET splash is still up. Log lines that match this:

- `Initialized Berberis (aarch64)`
- `Trying to initialize the debugger with options: ... address=10.0.2.2:<port>`
- `open_from_update_dir: assembly file DOES NOT EXIST`
- `Requested dump for pid`

`Recall.Maui.csproj` therefore builds **both** `android-arm64` (phone) and `android-x64` (this emulator). The emulator uses the same in-code GemmaTextTokenizer as the phone. It does not load Tokenizers.DotNet.runtime.linux-x64. `.so` files stay stored uncompressed (`AndroidStoreUncompressedFileExtensions`).

After pulling this change:

1. Uninstall `com.recall.shell` on the emulator.
2. **Rebuild** Android Debug (do not reuse the previous arm64-only deploy).
3. Deploy again. `adb shell getprop ro.product.cpu.abi` on this AVD should be `x86_64`, and the new log should **not** say `Berberis`.

If you start with the debugger (F5) and the splash still sits for about 30 seconds, run **Start Without Debugging** once. That skips the Mono debugger agent on `10.0.2.2`. The app itself does not need that agent to reach onboarding.

## What reset does **not** do

- It does not uninstall the app or clear Android **scoped storage** URIs held by the OS outside app data; remove folder grants in UI before reset if you need an empty grant list in memory.
- It does not ship in Release and is not a substitute for **Delete everything Recall stored** (product control on a later task).
- It does not fix model download hash mismatches by itself; it only deletes local model files so you can retry download from a clean tree.
