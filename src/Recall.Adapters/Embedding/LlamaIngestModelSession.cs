using Recall.Adapters.Embedding.Llama;
using Recall.Core.Ingest;

namespace Recall.Adapters.Embedding;

public sealed class LlamaIngestModelSession : IIngestModelSession
{
    private readonly TextModelRuntime text;
    private readonly VisionModelRuntime vision;
    private readonly AudioModelRuntime audio;
    private readonly string textModelPath;
    private readonly string mmprojPath;

    public LlamaIngestModelSession(
        TextModelRuntime text,
        VisionModelRuntime vision,
        AudioModelRuntime audio,
        string textModelPath,
        string mmprojPath)
    {
        this.text = text;
        this.vision = vision;
        this.audio = audio;
        this.textModelPath = textModelPath;
        this.mmprojPath = mmprojPath;
    }

    public void OpenText() => text.OpenSession();

    public void CloseText() => text.CloseSession();

    public void OpenVision() => vision.OpenSession();

    public void CloseVision() => vision.CloseSession();

    public void OpenAudio() => audio.OpenSession();

    public void CloseAudio() => audio.CloseAudio();

    public void EnsureProbe()
    {
        LlamaNativeGate.Enter();
        try
        {
            MediaContextProbe.EnsureApplied(textModelPath, mmprojPath);
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }
}
