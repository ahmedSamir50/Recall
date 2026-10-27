using Recall.Core.Media;

namespace Recall.IntegrationTests.Infrastructure;

public static class MediaPreviewSeekProof
{
    public static (bool SeekCalled, TimeSpan Position, CancellationToken Token) Plan(double startSeconds, TimeSpan duration)
    {
        if (!MediaPreviewSeekPolicy.ShouldSeek(startSeconds, duration))
        {
            return (false, TimeSpan.Zero, CancellationToken.None);
        }

        return (true, TimeSpan.FromSeconds(startSeconds), CancellationToken.None);
    }
}
