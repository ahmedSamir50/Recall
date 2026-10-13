#if !WINDOWS && !ANDROID && !IOS && !MACCATALYST
using Recall.Core.Device;

namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceIdCandidateProvider
{
    private static DeviceIdCandidate GetCandidatePlatform() => new(null, true);
}
#endif
