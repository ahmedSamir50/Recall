Status: in-code for grant ingest on the MAUI host, the audio search on Home, the window plan, the WAV reader, and the sibling-caption parser. Speech-to-text is forbidden. The audio phase follows the vision download and does not fetch a separate weight.

## What the app does

`AddRecallCore` registers `DisabledAudioFileIngest`, which returns without reading. `AddRecallMauiPlatform` replaces that with `AudioFileIngest`. A grant walk that has audio or video turned on calls `IndexGrantAsync` after the text pass. `IndexAsync` ignores a caption file. If the audio kind is off, `AudioOffReport` records that and the file is not embedded, including a video on a video-only grant. If the audio kind is on, the platform `IAudioSource` reads the bytes. `WavPcm` decodes a WAV. `WindowsAudioSource`, `AndroidAudioSource`, and `AppleAudioSource` decode other containers to 16 kHz mono PCM. `AudioWindows` plans overlapping windows of 11200 milliseconds. The hop is half that length. A short tail extends the last window. Each window is embedded to 768 numbers and stored in `items_audio`. When the ingest scheduler owns the media pass, the audio encoder stays open across every window in that pass and `CloseAudio` runs in the pass `finally`. A lone `IndexAsync` still closes audio after that file when it opened the session.

The ingest writes one `ingest_file` row and one `ingest_chunk` row per window into `checkpoints.db` through the existing alias path, so the same SHA-256 bytes are not embedded twice. Deleting the media file walks those chunk rows and sets `path_live` to 0, and the vector then stops being a hit. Changed bytes retire the window ids and `TryDelete` the windows no other path still holds; the id scheme keeps one copy of the same bytes.

`MauiSiblingFiles` reads a caption in the same folder when `SiblingTranscript` says the names match: `.srt`, `.vtt`, `.sbv`, `.ass`, `.ssa`, `.ttml`. Those words are stored on the audio row. The parser does not open a media container. `ReadsEmbeddedTrack` is false.

Home can pick an audio clip. `AudioQueryGate` uses the same wait, ready, and fail decisions as the picture gate, read from the audio download snapshot. A waiting phase starts `DownloadAudioModelAsync`, which follows the vision file `mmproj-embeddinggemma-2-Q8_0.gguf` and does not request a second URL. `AudioSearchService` embeds the clip once and searches `items_audio` inside grants that have audio turned on. Windows of one file collapse to the earliest start.

Opening a hit with a start above zero plays the media file in the app: `MediaHitStart.Seconds` reads the start off a text or media hit and `Seeks` is true only above zero, so Home calls `IMediaPreview.PlayAsync`, which pushes a `MediaPreviewPage` and plays the file in `CommunityToolkit.Maui.MediaElement` at that second. A transcript hit carries `StartSeconds` on `TextFindCard`, so the same page seeks into the matching window. A start of 0 still uses the launcher.

## Forbidden

No speech-to-text package is in the solution. Phone audio quality is U9 and has no number.
