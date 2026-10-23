Status: in-code for the stored choice.

## In words

Preferences remembers choices that are not themselves the index. The age cutoff is 2, 5, or 7 years and starts at 2. Storing that number does not shorten a vector. The extra skip names the person adds are also a preference. Built-in skips stay even if this list is empty.

## Owns

Preferences owns the age cutoff of 2, 5, or 7 years. Type: `AgeCutoffPreferenceService`. Store key `recall.preferences.ageCutoff`. Preferences also owns `UserSkipService`.

## Does not own

It does not move a vector. It does not store sign-in start or a search shortcut.

## Use cases

Use case: [choose-age](../use-cases/choose-age.md). Shrink is on the [absent](../use-cases/absent.md) use-case page.
