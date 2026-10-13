namespace Recall.Core.Faults;

public class RecallFaultException : Exception
{
    public RecallFaultException()
        : this(RecallFault.TextModelNotReady)
    {
    }

    public RecallFaultException(string message)
        : base(message)
    {
    }

    public RecallFaultException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RecallFaultException(RecallFault fault)
        : base(fault.ToString())
    {
        Fault = fault;
    }

    public RecallFault Fault { get; protected set; }

    public string ResourceKey => "Fault_" + Fault;
}
