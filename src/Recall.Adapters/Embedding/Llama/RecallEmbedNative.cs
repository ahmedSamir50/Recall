using System.Runtime.InteropServices;
using System.Text;

namespace Recall.Adapters.Embedding.Llama;

internal interface IRecallEmbedLibrary
{
    int Open(string modelPath);

    void Close();

    int CountTokens(string text);

    int Embed(string text, float[] destination768);
}

internal static class RecallEmbedResults
{
    internal static int RequireCount(int code, int count)
    {
        if (code != 0)
        {
            throw new InvalidOperationException($"recall_embed count failed with code {code}.");
        }

        return count;
    }
}

internal sealed class RecallEmbedNative : IRecallEmbedLibrary
{
    public int Open(string modelPath)
    {
        try
        {
            return recall_embed_open(Utf8NullTerminated(modelPath));
        }
        catch (DllNotFoundException)
        {
            throw new InvalidOperationException("recall_embed library is missing.");
        }
        catch (EntryPointNotFoundException)
        {
            throw new InvalidOperationException("recall_embed library is missing.");
        }
    }

    public void Close() => recall_embed_close();

    public int CountTokens(string text)
    {
        try
        {
            var code = recall_embed_count_tokens(Utf8NullTerminated(text), out var count);
            return RecallEmbedResults.RequireCount(code, count);
        }
        catch (DllNotFoundException)
        {
            throw new InvalidOperationException("recall_embed library is missing.");
        }
        catch (EntryPointNotFoundException)
        {
            throw new InvalidOperationException("recall_embed library is missing.");
        }
    }

    public int Embed(string text, float[] destination768)
    {
        try
        {
            return recall_embed_embed(Utf8NullTerminated(text), destination768);
        }
        catch (DllNotFoundException)
        {
            throw new InvalidOperationException("recall_embed library is missing.");
        }
        catch (EntryPointNotFoundException)
        {
            throw new InvalidOperationException("recall_embed library is missing.");
        }
    }

    private static byte[] Utf8NullTerminated(string value) =>
        Encoding.UTF8.GetBytes(value + "\0");

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_open(byte[] model_path);

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_close();

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_count_tokens(byte[] text, out int token_count);

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_embed(byte[] text, float[] destination_768);
}
