# Screen map — design vs behavior

Status: 13 Oct 2026 (gate). Visual layout and copy follow `InitDocs/DesignConcepts/Bible/RECALL_DESIGN_BIBLE.md` and `InitDocs/DesignConcepts/Prototypes/` where they do not conflict with `docs/plan.md`. Runtime behavior follows `docs/plan.md` only.

**Phase progress:** Phase 1 shell is **partial** — platform device id and native/display name rules are in Core and Maui; reopenable grants, per-grant file types, Find-only empty search, pin/age/preferences, and localized model rows exist; phone/Windows open builds not verified. Phase 2 is **in progress (not done)** — download/embed/query adapters exist; models progress UI and live search cards are not wired. Demo result cards were removed from Home; they were never Phase 2 results.

Legend: **Wired** = implemented against a Core port or navigation defined in the plan. **Unwired** = control is drawn; no handler or no backend yet. **Visual-only** = static chrome matching the prototype without claiming demo facts.

| Surface | Route | Wired | Unwired / visual-only |
|---|---|---|---|
| Landing (marketing) | `/landing` | Theme toggle, language toggle, **Get Recall** / download CTAs → Welcome onboarding. | Nav anchors, fact row numbers, demo query card, privacy claims, model size, download URLs. No WebView of external HTML. |
| Welcome | `/onboarding/welcome` | Primary **Choose folders** → Folders onboarding. Theme toggle, language toggle. | Ghost **How it stays private** (use `/landing` for privacy story). Fact grid is copy only. |
| Folders (step 1/3) | `/onboarding/folders` | List grants, add via folder picker, remove grant, overlap warning, per-grant file types, **Continue** enabled only when `IScopeManagerService.CanContinue()`. Back → Welcome. | Per-row item counts when not indexed. Preset demo paths. |
| Models (step 2/3) | `/onboarding/models` | Rows from `ModelCatalog` with localized modality labels. **Continue** disabled until download epic (phase 2). Back → Folders. | Progress, verified, hash failure, retry (phase 2). No language-model or speech-to-text rows. |
| Ready (step 3/3) | `/onboarding/ready` | **Start searching** → Home. | Indexed counts and “background indexing” lines (no indexer in phase 1). |
| Home / Search | `/` | Find-only command bar shell; empty search state (`Search_EmptyTitle` / `Search_EmptyLead`). Theme, language, privacy pill. Nav to Lattice, Folders, Models, Preferences. | Query execution, latency measurement, keyboard shortcut, Group/Tag modes. |
| Pin | `/pin` | Empty state copy (“keep this easy to find”). | Pin actions until phase 6. |
| This device | `/device` | `IDeviceRecordService` display name edit; read-only original OS name and device id. | Network send (none in v1). |
| Lattice | `/lattice` | Sync stub copy. | Painted node graph (v1.5 cluster browser not in plan). |
| Folders (shell) | `/folders` | Same folder grant UI as onboarding; iPhone foreground sentence when `IRecallShellKind` is iOS. | — |
| Models (shell) | `/models` | Same model list as onboarding empty state. | Download state machine (phase 2). |
| Preferences | `/preferences` | Theme System/Dark/Light via `ThemeService`. Language ar/en via `LocaleService`. Age 2/5/7 via `AgeCutoffPreferenceService` (stored only). Links to Folders, Models, Pin, This device. | Delete index confirm (no index yet). Reduce motion (later). |

## Component behavior

| Component | Wired | Unwired |
|---|---|---|
| `PrivacyPill` | Static copy “On this device · 0 KB sent” / Arabic equivalent. | Live egress metering (open U3). |
| `ThemeToggle` | Cycles preference; persists; sets `data-theme` on document. | — |
| `LanguageToggle` | Toggles `UiLocale`; persists; sets `lang`/`dir`. | — |
| `Avatar` | Shows initials from device display name when `IDeviceRecordService` supplies one. | Fixed “AS” demo initials. |
| `CommandBar` | Find mode and placeholder on Home. | Search execution, latency measurement, keyboard shortcut. |
| `ResultGrid` | Empty state on Home. | Demo photo/voice/document/video cards (removed from Home). |
| `ModelRow` | Catalog modality label from resources. | Download state machine. |
| `StatusBar` | Ready; “no items indexed yet” copy. | Live item count, indexing paused truth. |
| `LatticeBackground` | Static frame when reduced motion; optional shader interop. | 60 fps continuous loop default off on phone per performance push-back (static frame). |
| `FolderGrantPanel` | Grants list, picker, overlap, per-grant file types, iOS-only foreground note. | Index-now and per-row counts (phase 3). |

## Core ports used on these screens

| Port | Screens |
|---|---|
| `IScopeManagerService` | Folders (list, add, remove, file types, continue gate, overlap) |
| `IFolderPickerService` | Folders **Add folder** |
| `IGrantedFolderAccessService` | Maui startup — restore SAF / security-scoped grants |
| `IRecallShellKind` | Folders — iPhone copy |
| `IDeviceRecordService` | App shell avatar, This device |
| `IAgeCutoffPreferenceStore` / `AgeCutoffPreferenceService` | Preferences — age choice |
| `ShellUiStore` | All shell and onboarding screens (snapshot of theme, locale, route, grants; commands write Core services then publish) |
| `ThemeService` + `IThemePreferenceStore` + `ISystemThemeProvider` | All (chrome; projected via `ShellUiStore`) |
| `LocaleService` + `IUiLocaleStore` | All (copy, digits, RTL; projected via `ShellUiStore`) |
| `ModelCatalog` | Models (ids only; display names in `ar` / `en` resources) |
| `IModelDownloadService` | Models (no methods until phase 2; UI shows empty state) |
