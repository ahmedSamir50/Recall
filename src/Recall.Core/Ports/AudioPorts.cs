using Recall.Core.Scope;

namespace Recall.Core.Ports;

/// <summary>
/// Reads the raw bytes of an audio or video file and decodes compressed audio
/// plus video soundtracks to 16 kHz mono PCM. Implemented per platform shell.
/// </summary>
public interface IAudioSource
{
    byte[]? Read(string path);

    float[] ReadCompressed(byte[] file);
}

public interface ISiblingFiles
{
    IReadOnlyList<SiblingFile> List(string mediaPath);
}

public interface IAudioOffReport
{
    bool Shown { get; }

    void Report();

    void Clear();
}

public interface IAudioFileIngest
{
    Task IndexAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default);

    Task IndexGrantAsync(ScopeFolderGrant grant, CancellationToken cancellationToken = default);
}

public sealed class DisabledAudioFileIngest : IAudioFileIngest
{
    public Task IndexAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task IndexGrantAsync(ScopeFolderGrant grant, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public readonly record struct SiblingFile(string Name, string Text);
