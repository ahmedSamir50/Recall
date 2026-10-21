using Recall.Core.Ingest;
using Recall.Core.Ports;

namespace Recall.Adapters.Media;

public sealed class WindowsVideoFrameSampler : IVideoFrameSamplerService
{
    public IReadOnlyList<VideoFrame> SampleEveryTwoSeconds(Stream media)
    {
        ArgumentNullException.ThrowIfNull(media);
        throw new NotSupportedException(FrameCadence.WindowsPackageMessage);
    }
}
