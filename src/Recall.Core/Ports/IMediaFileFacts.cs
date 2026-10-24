namespace Recall.Core.Ports;

public interface IMediaFileFacts
{
    bool Exists(string path);

    DateTimeOffset? LastWriteUtc(string path);
}
