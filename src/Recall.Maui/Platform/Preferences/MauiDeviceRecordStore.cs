using Recall.Core.Device;
using Recall.Core.Ports;
using System.Text.Json;

namespace Recall.Maui.Platform;

public sealed class MauiDeviceRecordStore : IDeviceRecordStore
{
    private const string Key = "recall.device.record";

    public DeviceRecordState? Load()
    {
        var json = Preferences.Default.Get(Key, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<DeviceRecordState>(json);
    }

    public void Save(DeviceRecordState state)
    {
        Preferences.Default.Set(Key, JsonSerializer.Serialize(state));
    }
}
