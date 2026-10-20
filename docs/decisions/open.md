Status: open

## Do not pick

- U1 no ship disk number.
- U2 no ship RSS number.
- U3 crash reports stay on the device and an opt-in reporter is unresolved.
- U9 phone audio quality at int8 versus the published 4-bit file is unmeasured.
- The two latency tables on the [performance](../performance.md) page are unresolved against each other.
- Face recognizer weights are not chosen. A stored face vector at 128 numbers stays forbidden.
- The same-face cosine threshold is unmeasured.
- Arabic extraction quality of the v1.5 notes model is unmeasured.
- A net10 iPhone build of the generative library is not a ship claim.
- GitHub Actions for android-arm64, ios-arm64, win-x64, and maccatalyst-arm64. The remote move is accepted in ADR 0013. No workflow file is in the tree. ZVec.NET 1.0.0-beta.6 is not on nuget.org, and this row does not pick a feed.
- A Linux tray, sign-in start, and search shortcut. They stay open on the desktop-presence page and are not in the tree.
- The linux-x64 shell. .NET MAUI has no Linux target framework. Microsoft.Maui.Platforms.Linux.Gtk4 and Platform.Maui.Linux.Gtk4 are not v1 dependencies. linux-arm64 is not a v1 RID.
- Whether hiding the desktop window keeps ScopeWatchCoordinator running. Destroying calls Stop today.
