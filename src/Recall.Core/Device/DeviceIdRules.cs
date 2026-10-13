namespace Recall.Core.Device;

public static class DeviceIdRules
{
    public static bool IsKnownBad(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && string.Equals(value.Trim(), DevicePlatformIds.KnownBadAndroidId, StringComparison.OrdinalIgnoreCase);

    public static bool ShouldUseRandomFallback(DeviceIdCandidate candidate) =>
        candidate.IsReady
        && (string.IsNullOrWhiteSpace(candidate.Value) || IsKnownBad(candidate.Value));

    public static string ChooseId(DeviceIdCandidate candidate, Func<string> randomId)
    {
        if (!candidate.IsReady)
        {
            throw new DeviceIdNotReadyException();
        }

        if (ShouldUseRandomFallback(candidate))
        {
            return randomId();
        }

        return candidate.Value!.Trim();
    }
}
