using System.Runtime.CompilerServices;
using Recall.Core.Scope;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public sealed class DirectoryGrantedTextReader : IGrantedTextReader
{
    public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(
        ScopeFolderGrant grant,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Directory.Exists(grant.Path))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(grant.Path, "*.*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!GrantTextFiles.IsTextFile(file))
            {
                continue;
            }

            var text = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            var modified = new DateTimeOffset(File.GetLastWriteTimeUtc(file), TimeSpan.Zero);
            yield return new GrantedTextDocument(file, Path.GetFileName(file), text, modified);
        }
    }
}
