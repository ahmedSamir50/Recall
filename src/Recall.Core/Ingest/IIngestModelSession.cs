namespace Recall.Core.Ingest;

public interface IIngestModelSession
{
    void OpenText();

    void CloseText();

    void OpenVision();

    void CloseVision();

    void OpenAudio();

    void CloseAudio();

    void EnsureProbe();
}
