using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Recall.Adapters.Embedding.Llama;

internal interface IRecallEmbedLibrary
{
    int Open(string modelPath);

    void Close();

    int CountTokens(string text);

    int[] TokenEnds(string text);

    int Embed(string text, float[] destination768);

    int OpenVision(string mmprojPath);

    int EmbedVision(byte[] jpeg, float[] destination768);

    void CloseVision();

    int OpenAudio(string mmprojPath);

    int EmbedAudio(float[] samples, float[] destination768);

    void CloseAudio();
}

internal static class RecallEmbedResults
{
    internal const string MissingLibraryMessage = "recall_embed library is missing.";

    internal const string VisionFailedFormat = "recall_embed vision failed with code {0}.";

    internal const string TokenEndsFailedFormat = "recall_embed token ends failed with code {0}.";

    internal static InvalidOperationException MissingLibrary() => new(MissingLibraryMessage);

    internal static int RequireCount(int code, int count)
    {
        if (code != 0)
        {
            throw new InvalidOperationException($"recall_embed count failed with code {code}.");
        }

        return count;
    }

    internal static int RequireVision(int code)
    {
        if (code != 0)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, VisionFailedFormat, code));
        }

        return code;
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
            throw RecallEmbedResults.MissingLibrary();
        }
        catch (EntryPointNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
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
            throw RecallEmbedResults.MissingLibrary();
        }
        catch (EntryPointNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }
    }

    public int[] TokenEnds(string text)
    {
        try
        {
            var ends = new int[CountTokens(text) + 8];
            var code = recall_embed_token_ends(Utf8NullTerminated(text), ends, ends.Length, out var endsCount);
            if (code != 0)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, RecallEmbedResults.TokenEndsFailedFormat, code));
            }

            return ends[..endsCount];
        }
        catch (DllNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }
        catch (EntryPointNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
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
            throw RecallEmbedResults.MissingLibrary();
        }
        catch (EntryPointNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }
    }

    public int OpenVision(string mmprojPath)
    {
        try
        {
            return recall_embed_vision_open(Utf8NullTerminated(mmprojPath));
        }
        catch (DllNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }
        catch (EntryPointNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }
    }

    public int EmbedVision(byte[] jpeg, float[] destination768)
    {
        int code;
        try
        {
            code = recall_embed_vision_embed(jpeg, jpeg.Length, destination768);
        }
        catch (DllNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }
        catch (EntryPointNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }

        return RecallEmbedResults.RequireVision(code);
    }

    public void CloseVision() => recall_embed_vision_close();

    public int OpenAudio(string mmprojPath)
    {
        try
        {
            return recall_embed_audio_open(Utf8NullTerminated(mmprojPath));
        }
        catch (DllNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }
        catch (EntryPointNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }
    }

    public int EmbedAudio(float[] samples, float[] destination768)
    {
        try
        {
            return recall_embed_audio_embed(samples, samples.Length, destination768);
        }
        catch (DllNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }
        catch (EntryPointNotFoundException)
        {
            throw RecallEmbedResults.MissingLibrary();
        }
    }

    public void CloseAudio() => recall_embed_audio_close();

    private static byte[] Utf8NullTerminated(string value) =>
        Encoding.UTF8.GetBytes(value + "\0");

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_open(byte[] model_path);

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_close();

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_count_tokens(byte[] text, out int token_count);

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_token_ends(byte[] text, int[] ends, int ends_capacity, out int ends_len);

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_embed(byte[] text, float[] destination_768);

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_vision_open(byte[] mmproj_path);

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_vision_embed(byte[] jpeg, int jpeg_len, float[] destination_768);

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_vision_close();

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_audio_open(byte[] mmproj_path);

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern int recall_embed_audio_embed(float[] samples, int n_samples, float[] destination_768);

    [DllImport("recall_embed", CallingConvention = CallingConvention.Cdecl)]
    private static extern void recall_embed_audio_close();
}
