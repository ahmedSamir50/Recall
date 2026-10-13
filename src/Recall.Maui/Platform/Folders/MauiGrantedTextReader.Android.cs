#if ANDROID
using Android.Provider;
using Recall.Core.Ingest;
using Recall.Core.Scope;
using Recall.Core.Text;

namespace Recall.Maui.Platform;

public sealed partial class MauiGrantedTextReader
{
    private partial bool TryPlatform(
        ScopeFolderGrant grant,
        CancellationToken cancellationToken,
        out IAsyncEnumerable<GrantedTextDocument> documents)
    {
        if (!grant.Path.StartsWith(ContentUri.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            documents = NoDocuments;
            return false;
        }

        documents = ReadContentTreeAsync(grant, cancellationToken);
        return true;
    }

    private static async IAsyncEnumerable<GrantedTextDocument> ReadContentTreeAsync(
        ScopeFolderGrant grant,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var resolver = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.ContentResolver;
        var tree = Android.Net.Uri.Parse(grant.Path);
        if (resolver is null || tree is null)
        {
            yield break;
        }

        var rootId = DocumentsContract.GetTreeDocumentId(tree);
        if (string.IsNullOrEmpty(rootId))
        {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(rootId);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parentId = pending.Pop();
            var children = DocumentsContract.BuildChildDocumentsUriUsingTree(tree, parentId);
            if (children is null)
            {
                continue;
            }

            using var cursor = resolver.Query(
                children,
                [
                    DocumentsContract.Document.ColumnDocumentId,
                    DocumentsContract.Document.ColumnDisplayName,
                    DocumentsContract.Document.ColumnMimeType,
                    DocumentsContract.Document.ColumnLastModified,
                ],
                null,
                null,
                null);
            if (cursor is null)
            {
                continue;
            }

            var idColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDocumentId);
            var nameColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDisplayName);
            var mimeColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnMimeType);
            var modifiedColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnLastModified);
            while (cursor.MoveToNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var id = idColumn >= 0 ? cursor.GetString(idColumn) ?? string.Empty : string.Empty;
                var name = nameColumn >= 0 ? cursor.GetString(nameColumn) ?? string.Empty : string.Empty;
                var mime = mimeColumn >= 0 ? cursor.GetString(mimeColumn) ?? string.Empty : string.Empty;
                if (string.Equals(mime, DocumentsContract.Document.MimeTypeDir, StringComparison.Ordinal))
                {
                    if (id.Length > 0)
                    {
                        pending.Push(id);
                    }

                    continue;
                }

                if (!GrantTextFiles.IsTextFile(name) || id.Length == 0)
                {
                    continue;
                }

                var documentUri = DocumentsContract.BuildDocumentUriUsingTree(tree, id);
                if (documentUri is null)
                {
                    continue;
                }

                using var stream = resolver.OpenInputStream(documentUri);
                if (stream is null)
                {
                    continue;
                }

                var sample = new byte[TextFileSignature.SampleBytes];
                var sampleCount = await stream.ReadAsync(sample, cancellationToken).ConfigureAwait(false);
                if (!TextFileSignature.IsSafeText(sample.AsSpan(0, sampleCount)))
                {
                    continue;
                }

                using var buffered = new MemoryStream(sampleCount + 1024);
                buffered.Write(sample, 0, sampleCount);
                await stream.CopyToAsync(buffered, cancellationToken).ConfigureAwait(false);
                buffered.Position = 0;
                using var reader = new StreamReader(buffered, detectEncodingFromByteOrderMarks: true);
                var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                var modified = DateTimeOffset.UnixEpoch;
                if (modifiedColumn >= 0 && !cursor.IsNull(modifiedColumn))
                {
                    var milliseconds = cursor.GetLong(modifiedColumn);
                    if (milliseconds > 0)
                    {
                        modified = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
                    }
                }

                yield return new GrantedTextDocument(documentUri.ToString()!, name, text, modified);
            }
        }
    }
}
#endif
