using Recall.Core.Ports;

namespace Recall.Core.Device;

public sealed class DeviceRecordService : IDeviceRecordService
{
    private readonly IDeviceRecordStore store;
    private readonly IDeviceOsNameProvider osNameProvider;
    private readonly IDeviceIdCandidateProvider idCandidateProvider;
    private readonly IRandomDeviceIdSource randomDeviceIdSource;
    private DeviceRecordState? cached;

    public DeviceRecordService(
        IDeviceRecordStore store,
        IDeviceOsNameProvider osNameProvider,
        IDeviceIdCandidateProvider idCandidateProvider,
        IRandomDeviceIdSource randomDeviceIdSource)
    {
        this.store = store;
        this.osNameProvider = osNameProvider;
        this.idCandidateProvider = idCandidateProvider;
        this.randomDeviceIdSource = randomDeviceIdSource;
    }

    public string GetDeviceId() => EnsureLoaded().DeviceId;

    public string GetOriginalOsName() => EnsureLoaded().OriginalOsName;

    public string GetDisplayName() => EnsureLoaded().DisplayName;

    public DisplayNameSetResult TrySetDisplayName(string displayName)
    {
        var trimmed = displayName.Trim();
        if (trimmed.Length == 0)
        {
            return DisplayNameSetResult.Empty;
        }

        var current = EnsureLoaded();
        if (string.Equals(current.DisplayName, trimmed, StringComparison.Ordinal))
        {
            return DisplayNameSetResult.Unchanged;
        }

        var updated = current with { DisplayName = trimmed };
        store.Save(updated);
        cached = updated;
        return DisplayNameSetResult.Saved;
    }

    public string GetInitials() => DeviceInitials.FromDisplayName(GetDisplayName());

    public void InvalidateCache() => cached = null;

    private DeviceRecordState EnsureLoaded()
    {
        if (cached is not null)
        {
            return cached;
        }

        var existing = store.Load();
        if (existing is not null)
        {
            cached = existing;
            return cached;
        }

        var candidate = idCandidateProvider.GetCandidate();
        if (!candidate.IsReady)
        {
            throw new DeviceIdNotReadyException();
        }

        var osName = osNameProvider.GetOsDeviceName().Trim();
        var deviceId = DeviceIdRules.ChooseId(candidate, randomDeviceIdSource.NewDeviceId);
        var created = new DeviceRecordState(deviceId, osName, string.IsNullOrEmpty(osName) ? string.Empty : osName);
        store.Save(created);
        cached = created;
        return cached;
    }
}
