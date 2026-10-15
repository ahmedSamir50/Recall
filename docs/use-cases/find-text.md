Status: in-code

## Actor

The person.

## Trigger

Enter on Home.

## Path

Enter calls `SubmitAsync`. `TextFindSession.ShouldQuery` is false for null or white space, and then Home does not call `FindAsync`. It sets the view to `Empty`, clears the fault, and sets `hasSearched` false. A query that passes calls `FindAsync` with the trimmed text and `Ui.Current.Grants`. `FindAsync` still throws on a blank query if something else calls it. No text grant returns an empty list. Otherwise wrap the search prefix once, embed once, cut to 512, fuse with RRF rank constant 60, drop a path outside the grant, and return up to 20 hits. `TextFindSession.Build` shows the first 5 immediately and the rest after. Group and Tag do not run.

## Failure

Any exception from `FindAsync` sets fault key `Search_Fault` and the view to `Empty`. A rank other than 60 throws inside the index.

## Domains

Shell, Search, Text, Vector index, Scope. Feature page: [text-search](../features/text-search.md).
