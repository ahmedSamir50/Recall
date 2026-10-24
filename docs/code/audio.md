Status: in-code

These headings exist so the catalog stays complete. The explanation is [What each piece is for](../architecture/pieces.md) and [Audio and transcripts](../features/audio-and-transcripts.md).

## src/Recall.Core/Ingest/AudioDecoders.cs

`AudioDecoders` is the shared failure sentence the platform decoders use when a compressed file cannot become PCM.

```csharp
public static class AudioDecoders
```

No branch.

## src/Recall.Core/Ingest/AudioOffReport.cs

`AudioOffReport` remembers that a media file was skipped because the grant did not have the audio kind on. Home can read `Shown`.

```csharp
public sealed class AudioOffReport
```

No branch.

## src/Recall.Core/Ingest/AudioFileIngest.cs

`AudioFileIngest` is the real audio walk. A caption file is ignored. Audio off is reported and the file is not embedded. A WAV is decoded in process. Other containers use the platform source. Each window is stored in `items_audio` at its window ordinal, an `ingest_file` row and one `ingest_chunk` row per window are written through `FinishPath` with the alias, a file already finished with the same SHA-256 content hash is skipped, and changed bytes retire and `TryDelete` their old window ids. The native audio context is closed after the file.

```csharp
public sealed class AudioFileIngest
if (SiblingTranscript.IsCaptionFile(fileName))
if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Audio))
if (fileBytes is null)
if (IngestCursorPolicy.Decide(row, contentHash) == IngestCursorDecision.Skip)
if (WavPcm.IsWav(fileBytes))
catch (InvalidDataException)
catch (NotSupportedException)
if (windows.Count == 0)
if (startSample >= pcm.LongLength)
if (cursors.AnyLivePath(retiredId))
if (index.TryDelete(retiredId))
```

```csharp
if (!sessionOwned)
```

## src/Recall.Core/Ports/AudioPorts.cs

These are the promises the MAUI host keeps. `DisabledAudioFileIngest` is the headless registration and does not read. `SiblingFile` is one caption found next to a media file.

```csharp
public interface IAudioSource
public interface ISiblingFiles
public interface IAudioOffReport
public interface IAudioFileIngest
public sealed class DisabledAudioFileIngest
public readonly record struct SiblingFile
```

No branch.

## src/Recall.Core/Search/AudioQueryGate.cs

`AudioQueryGate` reuses the picture download decision for the audio snapshot, so Home waits, fails, or searches with the same three outcomes.

```csharp
public static class AudioQueryGate
```

No branch.

## src/Recall.Core/Search/AudioSearchService.cs

`AudioSearchService` embeds one PCM clip and searches `items_audio` inside grants that have audio turned on. Windows of one file collapse to the earliest start. At most 20 files return.

```csharp
public sealed class AudioSearchService
if (mono16k.IsEmpty || grants is null)
if (grantedPaths.Count == 0)
if (filter.Length == 0)
if (ScopeGrantPolicy.IsInsideGrant(grantPath, path))
```

## src/Recall.Maui/Platform/Media/WindowsAudioSource.cs

`WindowsAudioSource` reads the file bytes and transcodes a compressed container to WAV, then to PCM. A transcode that fails for a reason other than the shared decoder sentence is wrapped as `Audio decode failed.`

```csharp
public sealed class WindowsAudioSource
catch (InvalidOperationException ex) when (ex.Message != AudioDecoders.FailureMessage)
if (!WavPcm.IsWav(wav))
catch (IOException)
```

```csharp
catch (InvalidDataException)
catch (System.Runtime.InteropServices.COMException)
if (prepared.FailureReason != TranscodeFailureReason.None)
```

## src/Recall.Maui/Platform/Media/AndroidAudioSource.cs

`AndroidAudioSource` finds an audio track and decodes it with `MediaCodec` to 16 kHz mono PCM.

```csharp
public sealed class AndroidAudioSource
catch (IOException)
if (trackIndex < 0)
if (candidateMime.StartsWith("audio/", StringComparison.Ordinal))
if (!inputDone)
if (inputIndex >= 0)
if (size < 0)
if (status < 0)
if (bufferInfo.Flags.HasFlag(MediaCodecBufferFlags.EndOfStream))
```

```csharp
catch (Java.Lang.Exception)
if (iterations > MaxDrainIterations)
if (BitConverter.IsLittleEndian)
if (!format.ContainsKey(MediaFormat.KeyPcmEncoding))
if (encoding != (int)Android.Media.Encoding.Pcm16bit)
```

## src/Recall.Maui/Platform/Media/AppleAudioSource.cs

`AppleAudioSource` opens the file with `AVAudioFile` and converts it to 16 kHz mono PCM.

```csharp
public sealed class AppleAudioSource
if (openError is not null)
if (channels == 0 || channels > 2)
if (rate <= 0)
if (readError is not null)
if (!converter.ConvertToBuffer(input, output, out var convertError)
if (convertError is not null)
if (output.FrameLength > 0)
if (wantsFlush)
if (channel == IntPtr.Zero)
catch (IOException)
```

```csharp
catch (InvalidOperationException)
catch (Exception)
if (audioTracks is null || audioTracks.Length == 0)
if (reader is null || readerError is not null)
if (!reader.StartReading())
if (sampleBuffer is null)
if (reader.Status == AVAssetReaderStatus.Failed || samples.Count == 0)
if (blockBuffer is null)
if (length <= 0)
if (copyError != CMBlockBufferError.None || bytes is null)
if (file.Length >= 12 && file[4] == (byte)'f' && file[5] == (byte)'t' && file[6] == (byte)'y' && file[7] == (byte)'p')
if (file.Length >= 3 && file[0] == (byte)'I' && file[1] == (byte)'D' && file[2] == (byte)'3')
if (file.Length >= 2 && file[0] == 0xFF && (file[1] & 0xE0) == 0xE0)
```

## src/Recall.Maui/Platform/Media/MauiSiblingFiles.cs

`MauiSiblingFiles` lists caption files in the same folder whose names match the media file, and reads them as UTF-8. A file that cannot be read is skipped.

```csharp
public sealed class MauiSiblingFiles
if (string.IsNullOrWhiteSpace(mediaPath))
if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
if (string.Equals(candidateName, mediaName, StringComparison.OrdinalIgnoreCase) ||
catch (IOException)
catch (UnauthorizedAccessException)
```
