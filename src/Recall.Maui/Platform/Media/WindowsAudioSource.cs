#if WINDOWS
using Windows.Media;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Recall.Core.Ingest;
using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed class WindowsAudioSource : IAudioSource
{
    public byte[]? Read(string path) =>
        string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? null : File.ReadAllBytes(path);

    public float[] ReadCompressed(byte[] file)
    {
        var path = WriteTempFile(file);
        var outputPath = Path.Combine(FileSystem.CacheDirectory, $"recall-audio-out-{Guid.NewGuid():N}.wav");
        try
        {
            return TranscodeBlocking(path, outputPath);
        }
        catch (InvalidOperationException ex) when (ex.Message != AudioDecoders.FailureMessage)
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage, ex);
        }
        catch (InvalidDataException)
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }
        finally
        {
            TryDelete(path);
            TryDelete(outputPath);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "The audio source port is synchronous. WinRT work starts on the thread pool so it does not capture the UI synchronization context.")]
    private static float[] TranscodeBlocking(string sourcePath, string outputPath) =>
        Task.Run(() => TranscodeAsync(sourcePath, outputPath)).GetAwaiter().GetResult();

    private static async Task<float[]> TranscodeAsync(string sourcePath, string outputPath)
    {
        Directory.CreateDirectory(FileSystem.CacheDirectory);
        var source = Windows.Storage.StorageFile.GetFileFromPathAsync(sourcePath).AsTask();
        var folder = Windows.Storage.StorageFolder.GetFolderFromPathAsync(FileSystem.CacheDirectory).AsTask();
        await Task.WhenAll(source, folder).ConfigureAwait(false);
        var target = await folder.Result.CreateFileAsync(
            Path.GetFileName(outputPath),
            Windows.Storage.CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);
        var profile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
        profile.Audio = AudioEncodingProperties.CreatePcm(
            AudioWindows.SampleRate,
            1,
            16);
        profile.Video = null;
        using (var sourceStream = await source.Result.OpenAsync(Windows.Storage.FileAccessMode.Read).AsTask().ConfigureAwait(false))
        using (var destination = await target.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite).AsTask().ConfigureAwait(false))
        {
            var prepared = await new MediaTranscoder()
                .PrepareStreamTranscodeAsync(sourceStream, destination, profile)
                .AsTask()
                .ConfigureAwait(false);
            if (prepared.FailureReason != TranscodeFailureReason.None)
            {
                throw new InvalidOperationException(AudioDecoders.FailureMessage);
            }

            await prepared.TranscodeAsync().AsTask().ConfigureAwait(false);
            if (prepared.FailureReason != TranscodeFailureReason.None)
            {
                throw new InvalidOperationException(AudioDecoders.FailureMessage);
            }
        }

        var wav = await File.ReadAllBytesAsync(target.Path).ConfigureAwait(false);
        if (!WavPcm.IsWav(wav))
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }

        return WavPcm.Read(wav);
    }

    private static string WriteTempFile(byte[] file)
    {
        Directory.CreateDirectory(FileSystem.CacheDirectory);
        var path = Path.Combine(FileSystem.CacheDirectory, $"recall-audio-{Guid.NewGuid():N}");
        File.WriteAllBytes(path, file);
        return path;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // The temp file is best-effort cleanup.
        }
    }
}
#endif
