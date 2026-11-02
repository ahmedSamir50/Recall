namespace Recall.Core.Runtime;

public interface IIngestResourceReclaimer
{
    void ReleaseOptionalEncoders();
}

public sealed class IngestResourceReclaimer : IIngestResourceReclaimer
{
    private readonly Ports.IAudioEmbeddingEncoder? audio;

    public IngestResourceReclaimer(Ports.IAudioEmbeddingEncoder? audio = null)
    {
        this.audio = audio;
    }

    public void ReleaseOptionalEncoders()
    {
        // The text encoder stays resident by canon. Vision closes its own context per image.
        if (audio is { IsLoaded: true })
        {
            audio.CloseAudio();
        }
    }
}
