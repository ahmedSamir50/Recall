Status: accepted, 21 Oct 2026.

Context: v1 listed four RIDs. .NET MAUI has no Linux target framework. The GTK4 backends Microsoft.Maui.Platforms.Linux.Gtk4 and Platform.Maui.Linux.Gtk4 are experimental and unsupported.

Decision: linux-x64 is a v1 RID. linux-arm64 is not a v1 RID. win-arm64 stays not a v1 RID. Recall.Maui keeps net10.0-android, net10.0-ios, net10.0-maccatalyst, and net10.0-windows10.0.19041.0. Do not add Microsoft.Maui.Platforms.Linux.Gtk4 or Platform.Maui.Linux.Gtk4. The Linux shell is open. Plain HNSW applies to linux-x64 when a shell exists. DiskANN stays out, including on Linux.

Consequences: ADR 0003's RID count is superseded by this ADR. Task 7.2.1 still produces four shells. ADR 0012 still names the four shells that link recall_embed. android-x64 does not load a linux-x64 native library.
