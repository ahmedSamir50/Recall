using Recall.Core.Ingest;
using Recall.Core.Ports;
using System.Text;

namespace Recall.Maui.Platform;

public sealed class MauiSiblingFiles : ISiblingFiles
{
    public IReadOnlyList<SiblingFile> List(string mediaPath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            return [];
        }

        var directory = Path.GetDirectoryName(mediaPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return [];
        }

        var mediaName = Path.GetFileName(mediaPath);
        var results = new List<SiblingFile>();
        try
        {
            foreach (var candidate in Directory.EnumerateFiles(directory))
            {
                var candidateName = Path.GetFileName(candidate);
                if (string.Equals(candidateName, mediaName, StringComparison.OrdinalIgnoreCase) ||
                    !SiblingTranscript.Matches(mediaName, candidateName))
                {
                    continue;
                }

                try
                {
                    var text = File.ReadAllText(candidate, Encoding.UTF8);
                    results.Add(new SiblingFile(candidateName, text));
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
            }
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }

        return results;
    }
}
