using Microsoft.AspNetCore.Components;
using Recall.Core.Device;
using Recall.Core.Ports;

namespace Recall.UI.Pages;

public partial class Device
{
    [Inject]
    private IDeviceRecordService DeviceRecord { get; set; } = null!;

    private string displayName = string.Empty;
    private string originalName = string.Empty;
    private string deviceId = string.Empty;
    private string? displayNameError;
    private string? deviceIdError;

    protected override void OnInitialized()
    {
        try
        {
            displayName = DeviceRecord.GetDisplayName();
            originalName = DeviceRecord.GetOriginalOsName();
            deviceId = DeviceRecord.GetDeviceId();
        }
        catch (DeviceIdNotReadyException ex)
        {
            deviceId = string.Empty;
            deviceIdError = T(ex.ResourceKey);
        }
    }

    private void SaveDisplayName()
    {
        displayNameError = null;
        var result = DeviceRecord.TrySetDisplayName(displayName);
        if (result == DisplayNameSetResult.Empty)
        {
            displayNameError = T("Device_DisplayNameRequired");
            return;
        }

        displayName = DeviceRecord.GetDisplayName();
    }
}
