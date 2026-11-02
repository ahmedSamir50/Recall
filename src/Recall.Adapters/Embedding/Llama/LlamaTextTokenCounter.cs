using Recall.Core.Ports;
using System.Text;

namespace Recall.Adapters.Embedding.Llama;

public sealed class LlamaTextTokenCounter : ITextTokenCounter
{
    public const int WindowChars = 65_536;

    private readonly IRecallEmbedLibrary library;

    internal LlamaTextTokenCounter(IRecallEmbedLibrary library)
    {
        this.library = library;
    }

    public int CountTokens(string text) => CountTokens(text, default);

    public int CountTokens(string text, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        cancellationToken.ThrowIfCancellationRequested();
        LlamaNativeGate.Enter(cancellationToken);
        try
        {
            return library.CountTokens(text);
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    public IReadOnlyList<int> TokenEnds(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        LlamaNativeGate.Enter();
        try
        {
            return Collect(text);
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    private List<int> Collect(string text)
    {
        var ends = new List<int>();
        var offset = 0;
        while (offset < text.Length)
        {
            var length = Math.Min(text.Length - offset, WindowChars);
            var window = text.Substring(offset, length);
            var local = ToCharEnds(window, library.TokenEnds(window));
            if (local.Count == 0)
            {
                break;
            }

            foreach (var end in local)
            {
                ends.Add(offset + end);
            }

            var consumed = local[^1];
            if (consumed <= 0)
            {
                break;
            }

            offset += consumed;
        }

        return ends;
    }

    private static IReadOnlyList<int> ToCharEnds(string text, int[] byteEnds)
    {
        if (byteEnds.Length == 0)
        {
            return byteEnds;
        }

        var utf8 = Encoding.UTF8.GetBytes(text);
        var ends = new List<int>(byteEnds.Length);
        var byteCursor = 0;
        var charCursor = 0;
        foreach (var raw in byteEnds)
        {
            var target = SnapUp(utf8, Math.Min(Math.Max(raw, 0), utf8.Length));
            while (byteCursor < target)
            {
                var length = Utf8SequenceLength(utf8, byteCursor);
                byteCursor += length;
                charCursor += length == 4 ? 2 : 1;
            }

            if (ends.Count > 0 && charCursor < ends[ends.Count - 1])
            {
                charCursor = ends[ends.Count - 1];
            }

            ends.Add(Math.Min(charCursor, text.Length));
        }

        return ends;
    }

    private static int SnapUp(byte[] utf8, int position)
    {
        while (position < utf8.Length && utf8[position] is >= 0x80 and <= 0xBF)
        {
            position++;
        }

        return position;
    }

    private static int Utf8SequenceLength(byte[] utf8, int position)
    {
        var first = utf8[position];
        if (first is >= 0xC0 and <= 0xDF)
        {
            return Math.Max(1, Math.Min(2, utf8.Length - position));
        }

        if (first is >= 0xE0 and <= 0xEF)
        {
            return Math.Max(1, Math.Min(3, utf8.Length - position));
        }

        if (first is >= 0xF0 and <= 0xF7)
        {
            return Math.Max(1, Math.Min(4, utf8.Length - position));
        }

        return 1;
    }
}
