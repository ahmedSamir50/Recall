Status: in-code

## Keys

Theme key stores `ThemePreference` via `ThemeResolution.ParseStored`, default `System`. Locale key stores `UiLocale`, default `En`. Age key stores the int of `AgeCutoffYears`, default `Two` (2). Grants key stores the grant list JSON. Onboarding key stores a bool, default false. Empty age other than 2, 5, or 7 is rejected by `AgeCutoffPreferenceService` (task 6.1.1). The stored age does not move vectors. Key `recall.skips.user` stores user-added extensions, folder names, and file names. Built-ins are not stored there. No key stores sign-in start or a search shortcut.
