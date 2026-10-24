Status: in-code

## Actor

The person.

## Trigger

The age control on Preferences.

## Path

Store only 2, 5, or 7. The default is 2. `AgeCutoffActions.Set` saves the choice and runs `AgeMoveService.Sweep` with the current UTC time.

## Failure

Any other number throws. While `AgeCutCorpus` is empty, sweep does not cut vectors; changing years only updates the stored cutoff.

## Domains

Shell, Preferences. Feature page: [age-cutoff](../features/age-cutoff.md).
