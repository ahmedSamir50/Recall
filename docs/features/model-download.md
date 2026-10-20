Status: in-code

## Files

The text download is embeddinggemma-2-Q8_0.gguf from ggml-org/embeddinggemma-2-GGUF. ModelSha256 is the lowercase SHA-256 in appsettings.json. tokenizer.json and the ONNX pair are not downloaded.

## Placement

`DownloadTransfer.Place`: existing bytes `<= 0` returns `Replace`; status 206 returns `Append`; status 416 returns `UseExistingPartial`; any other status returns `Replace`. `ExpectedTotalBytes`: 416 returns the existing length; 206 returns existing plus content length, or null when content length is null; otherwise the content length. `ReceivedFullBody` is true when expected is null and bytes on disk are `> 0`, or when bytes equal the expected total.

## UI policy

`ShouldAutoStartDownload` is true only for `NotStarted`. `ShouldOfferRetry` is true for `HashFailed`, `Interrupted`, and `Cancelled`. `ShouldPublishProgress` is true when the percent changed. `MarkVerifying` sets phase `Verifying` and clears fault. `MauiModelDownloadService` calls `MarkVerifying`. Percent is finished files plus the current file fraction, floored, clamped 0 to 100. `fileCount <= 0` yields 0. A bad hash fails the download. Float16 and a 4-bit file are not this download. Vision and audio weights are not downloaded here.
