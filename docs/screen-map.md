# Screen map — design vs behavior

Status: 13 Oct 2026. Visual layout and copy follow `InitDocs/DesignConcepts/Bible/RECALL_DESIGN_BIBLE.md` and `InitDocs/DesignConcepts/Prototypes/`. Runtime behavior follows `docs/plan.md` only. Prototype demo payloads are not loaded.

Legend: **Wired** = implemented against a Core port or navigation defined in the plan. **Unwired** = control is drawn; no handler or no backend yet. **Visual-only** = static chrome matching the prototype without claiming demo facts.

| Surface | Route | Wired | Unwired / visual-only |
|---|---|---|---|
| Landing (marketing) | `/landing` | Theme toggle, language toggle, **Get Recall** / download CTAs → Welcome onboarding. | Nav anchors, fact row numbers, demo query card, privacy claims, model size, download URLs. No WebView of external HTML. |
| Welcome | `/onboarding/welcome` | Primary **Choose folders** → Folders onboarding. Theme toggle, language toggle. | Ghost **How it stays private** (use `/landing` for privacy story). Fact grid is copy only. |
| Folders (step 1/3) | `/onboarding/folders` | List grants, add via folder picker, remove grant, overlap warning, **Continue** enabled only when `IScopeManagerService.CanContinue()`. Back → Welcome. | Per-row item counts when not indexed. Preset demo paths. iPhone foreground copy from plan 1.2.2 when on iOS shell. |
| Models (step 2/3) | `/onboarding/models` | Rows from `ModelCatalog` only (text, vision, audio embeddings). **Continue** disabled until download epic (empty / not started). Back → Folders. | Progress, verified, hash failure, retry (phase 2). No language-model or speech-to-text rows. |
| Ready (step 3/3) | `/onboarding/ready` | **Start searching** → Home. | Indexed counts and “background indexing” lines (no indexer in phase 1). |
| Home / Search | `/` | Command bar shell (modes, query line, latency pill, Ctrl+K chrome). Theme, language, privacy pill. Nav to Lattice, Folders, Models, Preferences. Empty search behavior per plan 1.2.3 is not wired to retrieval. | Find / Group / Tag mode switches, context chips, demo query text, bento cards, match %, inspector actions, sort, playback, open/show file. |
| Lattice | `/lattice` | Sync stub copy. | Painted node graph (v1.5 cluster browser not in plan). |
| Folders (shell) | `/folders` | Same folder grant UI as onboarding. | — |
| Models (shell) | `/models` | Same model list as onboarding empty state. | — |
| Preferences | `/preferences` | Theme System/Dark/Light via `ThemeService`. Language ar/en via `LocaleService`. Links to Folders and Models. | Delete index confirm (no index yet). Age 2/5/7, device display name, reduce motion (later). |

## Component behavior

| Component | Wired | Unwired |
|---|---|---|
| `PrivacyPill` | Static copy “On this device · 0 KB sent” / Arabic equivalent. | Live egress metering (open U3). |
| `ThemeToggle` | Cycles preference; persists; sets `data-theme` on document. | — |
| `LanguageToggle` | Toggles `UiLocale`; persists; sets `lang`/`dir`. | — |
| `Avatar` | Shows initials from device display name when `IDeviceRecordService` supplies one. | Fixed “AS” demo initials. |
| `CommandBar` | Placeholder and segments rendered. | Search execution, latency measurement, keyboard shortcut. |
| `ResultGrid` | Empty state only. | Photo/voice/document/video/code/unavailable demo cards. |
| `ModelRow` | Catalog name and modality label. | Download state machine. |
| `StatusBar` | Ready / empty counts. | Item count, model names, indexing paused. |
| `LatticeBackground` | Static frame when reduced motion; optional shader interop. | 60 fps continuous loop default off on phone per performance push-back (static frame). |

## Core ports used on these screens

| Port | Screens |
|---|---|
| `IScopeManagerService` | Folders (list, add, remove, continue gate, overlap) |
| `IFolderPickerService` | Folders **Add folder** |
| `ShellUiStore` | All shell and onboarding screens (snapshot of theme, locale, route, grants; commands write Core services then publish) |
| `ThemeService` + `IThemePreferenceStore` + `ISystemThemeProvider` | All (chrome; projected via `ShellUiStore`) |
| `LocaleService` + `IUiLocaleStore` | All (copy, digits, RTL; projected via `ShellUiStore`) |
| `ModelCatalog` | Models |
| `IModelDownloadService` | Models (no methods until phase 2; UI shows empty state) |
