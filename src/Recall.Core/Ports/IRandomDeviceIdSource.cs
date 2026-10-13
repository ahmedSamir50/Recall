namespace Recall.Core.Ports;

public interface IRandomDeviceIdSource
{
    string NewDeviceId();
}
