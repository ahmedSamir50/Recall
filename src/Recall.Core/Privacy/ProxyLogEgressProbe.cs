using System.Globalization;
using Recall.Core.Faults;
using Recall.Core.Ports;

namespace Recall.Core.Privacy;

public sealed class ProxyLogEgressProbe : IEgressProbe
{
    public const string DownloadFinishedMarker = "download-finished";

    public static readonly TimeSpan WatchWindow = TimeSpan.FromMinutes(60);

    private readonly string? logPath;

    public ProxyLogEgressProbe(string? logPath)
    {
        this.logPath = logPath;
    }

    public async Task<bool> HasNonLocalByteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
        {
            throw new RecallFaultException(RecallFault.EgressProxyMissing);
        }

        var lines = await File.ReadAllLinesAsync(logPath, cancellationToken);
        var records = new List<(long Timestamp, string Address, int ByteCount)>(lines.Length);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new FormatException("Egress log line is not three fields.");
            }

            var parts = line.Split('\t', StringSplitOptions.None);
            if (parts.Length != 3)
            {
                throw new FormatException("Egress log line is not three fields.");
            }

            if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp))
            {
                throw new FormatException("Egress timestamp is not an integer.");
            }

            if (parts[1].Length == 0)
            {
                throw new FormatException("Egress address is blank.");
            }

            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var byteCount))
            {
                throw new FormatException("Egress byte count is not an integer.");
            }

            if (byteCount < 0)
            {
                throw new FormatException("Egress byte count is negative.");
            }

            records.Add((timestamp, parts[1], byteCount));
        }

        var markerLines = records.Where(r => string.Equals(r.Address, DownloadFinishedMarker, StringComparison.Ordinal)).ToList();
        if (markerLines.Count == 0)
        {
            throw new RecallFaultException(RecallFault.EgressProxyMissing);
        }

        if (markerLines.Count > 1)
        {
            throw new FormatException("Egress log has two download-finished lines.");
        }

        var marker = markerLines[0];
        if (marker.ByteCount != 0)
        {
            throw new FormatException("Egress download-finished byte count is not zero.");
        }

        var start = marker.Timestamp;
        var end = records.Max(r => r.Timestamp);
        if (end - start < 60_000)
        {
            throw new RecallFaultException(RecallFault.EgressWatchIncomplete);
        }

        foreach (var record in records)
        {
            if (record.Timestamp < start)
            {
                continue;
            }

            if (string.Equals(record.Address, DownloadFinishedMarker, StringComparison.Ordinal))
            {
                continue;
            }

            if (IsLocalAddress(record.Address))
            {
                continue;
            }

            if (record.ByteCount > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLocalAddress(string address) =>
        string.Equals(address, "127.0.0.1", StringComparison.Ordinal)
        || string.Equals(address, "::1", StringComparison.Ordinal);
}
