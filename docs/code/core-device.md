## src/Recall.Core/Device/Record/DeviceRecordService.cs

```csharp
namespace Recall.Core.Device;
public sealed class DeviceRecordService : IDeviceRecordService
    public DeviceRecordService(
        IDeviceRecordStore store,
        IDeviceOsNameProvider osNameProvider,
        IDeviceIdCandidateProvider idCandidateProvider,
        IRandomDeviceIdSource randomDeviceIdSource)
    public string GetDeviceId() => EnsureLoaded().DeviceId;
    public string GetOriginalOsName() => EnsureLoaded().OriginalOsName;
    public string GetDisplayName() => EnsureLoaded().DisplayName;
    public DisplayNameSetResult TrySetDisplayName(string displayName)
    public string GetInitials() => DeviceInitials.FromDisplayName(GetDisplayName());
    public void InvalidateCache() => cached = null;
        if (trimmed.Length == 0)
        if (string.Equals(current.DisplayName, trimmed, StringComparison.Ordinal))
        if (cached is not null)
        if (existing is not null)
        if (!candidate.IsReady)
            throw new DeviceIdNotReadyException();
```


## src/Recall.Core/Device/Record/DeviceRecordState.cs

```csharp
namespace Recall.Core.Device;
public sealed record DeviceRecordState(string DeviceId, string OriginalOsName, string DisplayName);
```

No branch.


## src/Recall.Core/Device/Identity/DeviceIdRules.cs

```csharp
namespace Recall.Core.Device;
public static class DeviceIdRules
    public static bool IsKnownBad(string? value) =>
    public static bool ShouldUseRandomFallback(DeviceIdCandidate candidate) =>
    public static string ChooseId(DeviceIdCandidate candidate, Func<string> randomId)
        if (!candidate.IsReady)
            throw new DeviceIdNotReadyException();
        if (ShouldUseRandomFallback(candidate))
```


## src/Recall.Core/Device/Identity/DeviceIdCandidate.cs

```csharp
namespace Recall.Core.Device;
public readonly record struct DeviceIdCandidate(string? Value, bool IsReady);
```

No branch.


## src/Recall.Core/Device/Identity/DeviceIdNotReadyException.cs

```csharp
namespace Recall.Core.Device;
public sealed class DeviceIdNotReadyException : RecallFaultException
    public DeviceIdNotReadyException()
    public DeviceIdNotReadyException(string message)
    public DeviceIdNotReadyException(string message, Exception innerException)
```

No branch.


## src/Recall.Core/Device/Identity/DevicePlatformIds.cs

```csharp
namespace Recall.Core.Device;
public static class DevicePlatformIds
```

No branch.


## src/Recall.Core/Device/Identity/GuidRandomDeviceIdSource.cs

```csharp
namespace Recall.Core.Device;
public sealed class GuidRandomDeviceIdSource : IRandomDeviceIdSource
    public string NewDeviceId() => Guid.NewGuid().ToString("N");
```

No branch.


## src/Recall.Core/Device/Naming/DeviceInitials.cs

```csharp
namespace Recall.Core.Device;
public static class DeviceInitials
    public static string FromDisplayName(string displayName)
        if (name.Length == 0)
        if (parts.Length >= 2)
        if (!enumerator.MoveNext())
```


## src/Recall.Core/Device/Naming/DisplayNameSetResult.cs

```csharp
namespace Recall.Core.Device;
public enum DisplayNameSetResult
```

No branch.


## src/Recall.Core/Ports/DevicePorts.cs

```csharp
namespace Recall.Core.Ports;
public interface IDeviceOsNameProvider
public interface IDeviceRecordStore
public interface IDeviceRecordService
    string GetOsDeviceName();
    DeviceRecordState? Load();
    void Save(DeviceRecordState state);
    string GetDeviceId();
    string GetOriginalOsName();
    string GetDisplayName();
    DisplayNameSetResult TrySetDisplayName(string displayName);
    string GetInitials();
```

No branch.


## src/Recall.Core/Ports/IDeviceIdCandidateProvider.cs

```csharp
namespace Recall.Core.Ports;
public interface IDeviceIdCandidateProvider
    DeviceIdCandidate GetCandidate();
```

No branch.


## src/Recall.Core/Ports/IRandomDeviceIdSource.cs

```csharp
namespace Recall.Core.Ports;
public interface IRandomDeviceIdSource
    string NewDeviceId();
```

No branch.
