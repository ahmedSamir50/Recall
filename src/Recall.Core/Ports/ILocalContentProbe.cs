namespace Recall.Core.Ports;

public enum LocalContentState
{
    Local = 0,
    Remote = 1,
    Unknown = 2,
}

public interface ILocalContentProbe
{
    LocalContentState Probe(string path);
}
