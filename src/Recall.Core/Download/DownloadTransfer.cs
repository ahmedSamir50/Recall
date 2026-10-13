namespace Recall.Core.Download;

public enum DownloadBodyPlacement
{
    Replace = 0,
    Append = 1,
    UseExistingPartial = 2,
}

public static class DownloadTransfer
{
    public const int StatusPartialContent = 206;

    public const int StatusRangeNotSatisfiable = 416;

    public static DownloadBodyPlacement Place(int statusCode, long existingPartialBytes)
    {
        if (existingPartialBytes <= 0)
        {
            return DownloadBodyPlacement.Replace;
        }

        if (statusCode == StatusPartialContent)
        {
            return DownloadBodyPlacement.Append;
        }

        if (statusCode == StatusRangeNotSatisfiable)
        {
            return DownloadBodyPlacement.UseExistingPartial;
        }

        return DownloadBodyPlacement.Replace;
    }

    public static long? ExpectedTotalBytes(int statusCode, long existingPartialBytes, long? responseContentLength)
    {
        if (statusCode == StatusRangeNotSatisfiable)
        {
            return existingPartialBytes;
        }

        if (statusCode == StatusPartialContent)
        {
            return responseContentLength is long remaining ? existingPartialBytes + remaining : null;
        }

        return responseContentLength;
    }

    public static bool ReceivedFullBody(long bytesOnDisk, long? expectedTotalBytes)
    {
        if (expectedTotalBytes is null)
        {
            return bytesOnDisk > 0;
        }

        return bytesOnDisk == expectedTotalBytes.Value;
    }
}
