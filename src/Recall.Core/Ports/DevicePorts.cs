using Recall.Core.Device;

namespace Recall.Core.Ports;

public interface IDeviceOsNameProvider
{
    string GetOsDeviceName();
}

public interface IDeviceRecordStore
{
    DeviceRecordState? Load();

    void Save(DeviceRecordState state);
}

public interface IDeviceRecordService
{
    string GetDeviceId();

    string GetOriginalOsName();

    string GetDisplayName();

    DisplayNameSetResult TrySetDisplayName(string displayName);

    string GetInitials();
}
