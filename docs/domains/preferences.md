Status: in-code

## In words

Preferences remembers choices that are not themselves the index. The age cutoff is 2, 5, or 7 years and starts at 2. Storing that number runs `AgeMoveService.Sweep` but does not cut a vector while the recall gate has no corpus. The extra skip names the person adds are also a preference. Built-in skips stay even if this list is empty.

## Owns

Preferences owns the age cutoff of 2, 5, or 7 years. Type: `AgeCutoffPreferenceService`. Store key `recall.preferences.ageCutoff`. `AgeCutoffActions.Set` couples the store to `AgeMoveService`. Preferences also owns `UserSkipService`.

## Does not own

It does not implement the recall gate or alias moves. Those live in `Recall.Core.Age`. It does not store sign-in start or a search shortcut.

## Use cases

Use case: [choose-age](../use-cases/choose-age.md). Pin and shrink behavior: [age-cutoff](../features/age-cutoff.md).
