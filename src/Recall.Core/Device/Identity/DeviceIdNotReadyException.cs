using Recall.Core.Faults;

namespace Recall.Core.Device;

#pragma warning disable RCS1194
public sealed class DeviceIdNotReadyException : RecallFaultException
{
    public DeviceIdNotReadyException()
        : base(RecallFault.DeviceIdNotReady)
    {
    }

    public DeviceIdNotReadyException(string message)
        : base(message)
    {
        Fault = RecallFault.DeviceIdNotReady;
    }

    public DeviceIdNotReadyException(string message, Exception innerException)
        : base(message, innerException)
    {
        Fault = RecallFault.DeviceIdNotReady;
    }
}
#pragma warning restore RCS1194
