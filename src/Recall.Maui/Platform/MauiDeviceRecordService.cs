using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed class MauiDeviceRecordService : IDeviceRecordService
{
    private const string IdKey = "recall.device.id";
    private const string DisplayNameKey = "recall.device.displayName";

    public MauiDeviceRecordService()
    {
        EnsureCreated();
    }

    public string GetDisplayName()
    {
        EnsureCreated();
        return Preferences.Default.Get(DisplayNameKey, DeviceInfo.Name);
    }

    public string GetInitials()
    {
        var name = GetDisplayName().Trim();
        if (name.Length == 0)
        {
            return "?";
        }

        var parts = name.Split([' ', '.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
        }

        return char.ToUpperInvariant(name[0]).ToString();
    }

    private static void EnsureCreated()
    {
        if (!Preferences.Default.ContainsKey(IdKey))
        {
            Preferences.Default.Set(IdKey, Guid.NewGuid().ToString("N"));
            Preferences.Default.Set(DisplayNameKey, DeviceInfo.Name);
        }
    }
}
