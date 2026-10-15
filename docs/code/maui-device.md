## src/Recall.Maui/Platform/Device/MauiDeviceIdCandidateProvider.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceIdCandidateProvider : IDeviceIdCandidateProvider
    public DeviceIdCandidate GetCandidate() => GetCandidatePlatform();
```

No branch.


## src/Recall.Maui/Platform/Device/MauiDeviceIdCandidateProvider.Android.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceIdCandidateProvider
        if (activity is null)
```


## src/Recall.Maui/Platform/Device/MauiDeviceIdCandidateProvider.iOS.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceIdCandidateProvider
        if (vendor is null)
```


## src/Recall.Maui/Platform/Device/MauiDeviceIdCandidateProvider.Windows.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceIdCandidateProvider
```

No branch.


## src/Recall.Maui/Platform/Device/MauiDeviceIdCandidateProvider.Default.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceIdCandidateProvider
```

No branch.


## src/Recall.Maui/Platform/Device/MauiDeviceOsNameProvider.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceOsNameProvider : IDeviceOsNameProvider
    public string GetOsDeviceName() => GetOsDeviceNamePlatform();
```

No branch.


## src/Recall.Maui/Platform/Device/MauiDeviceOsNameProvider.Android.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceOsNameProvider
        if (activity is not null && OperatingSystem.IsAndroidVersionAtLeast(25))
            if (!string.IsNullOrWhiteSpace(deviceName))
```


## src/Recall.Maui/Platform/Device/MauiDeviceOsNameProvider.iOS.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceOsNameProvider
```

No branch.


## src/Recall.Maui/Platform/Device/MauiDeviceOsNameProvider.MacCatalyst.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceOsNameProvider
        if (nameRef == IntPtr.Zero)
            if (copied == 0)
```


## src/Recall.Maui/Platform/Device/MauiDeviceOsNameProvider.Windows.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceOsNameProvider
```

No branch.


## src/Recall.Maui/Platform/Device/MauiDeviceOsNameProvider.Default.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiDeviceOsNameProvider
```

No branch.
