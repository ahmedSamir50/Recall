namespace Recall.Core.Age;

public sealed class DiskMediaFileFacts : Ports.IMediaFileFacts
{
    public bool Exists(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public DateTimeOffset? LastWriteUtc(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            return new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
