using FluentAssertions;
using Recall.Adapters.Embedding;
using Recall.Adapters.Embedding.Llama;
using Recall.Core.Download;
using Recall.Core.Faults;

namespace Recall.UnitTests;

public sealed class LlamaEmbedTests
{
    [Fact]
    public async Task Embed_Blank_Throws()
    {
        var library = new FakeRecallEmbedLibrary();
        using var encoder = new LlamaTextEmbeddingEncoder(library);
        var act = () => encoder.EmbedPrefixedTextAsync(" ", TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<ArgumentException>();
        library.EmbedCalls.Should().Be(0);
    }

    [Fact]
    public async Task Embed_Cancelled_Throws()
    {
        var library = new FakeRecallEmbedLibrary();
        using var encoder = new LlamaTextEmbeddingEncoder(library);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var act = () => encoder.EmbedPrefixedTextAsync("task: search result | query: mars", cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        library.EmbedCalls.Should().Be(0);
    }

    [Fact]
    public async Task Embed_NativeCode_Throws()
    {
        var library = new FakeRecallEmbedLibrary { EmbedCode = 4 };
        using var encoder = new LlamaTextEmbeddingEncoder(library);
        var act = () => encoder.EmbedPrefixedTextAsync("task: search result | query: mars", TestContext.Current.CancellationToken);
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Be("recall_embed embed failed with code 4.");
    }

    [Fact]
    public async Task Embed_Success_IsUnit768()
    {
        var library = new FakeRecallEmbedLibrary
        {
            EmbedCode = 0,
            EmbedFill = 2f,
        };
        using var encoder = new LlamaTextEmbeddingEncoder(library);
        var vector = await encoder.EmbedPrefixedTextAsync("task: search result | query: mars", TestContext.Current.CancellationToken);
        vector.Length.Should().Be(768);
        L2Norm(vector.Span).Should().BeApproximately(1d, 0.00001);
    }

    [Fact]
    public void Count_Blank_Throws()
    {
        var library = new FakeRecallEmbedLibrary();
        var counter = new LlamaTextTokenCounter(library);
        var act = () => counter.CountTokens(" ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Count_ReturnsValue()
    {
        var library = new FakeRecallEmbedLibrary { CountResult = 12 };
        var counter = new LlamaTextTokenCounter(library);
        counter.CountTokens("red planet", TestContext.Current.CancellationToken).Should().Be(12);
    }

    [Fact]
    public void RequireCount_Code_Throws()
    {
        var act = () => RecallEmbedResults.RequireCount(9, 0);
        var ex = act.Should().Throw<InvalidOperationException>();
        ex.Which.Message.Should().Be("recall_embed count failed with code 9.");
    }

    [Fact]
    public void RequireCount_Zero_ReturnsCount()
    {
        RecallEmbedResults.RequireCount(0, 9).Should().Be(9);
    }

    [Fact]
    public void Ensure_MissingFile_Throws()
    {
        var library = new FakeRecallEmbedLibrary();
        using var runtime = new TextModelRuntime(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), library);
        var act = () => runtime.GetEncoder();
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.TextModelNotReady);
        library.OpenCalls.Should().Be(0);
    }

    [Fact]
    public async Task Ensure_OpenFails_Throws()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        await File.WriteAllTextAsync(Path.Combine(directory, new TextModelDownloadOptions().ModelFileName), "x", TestContext.Current.CancellationToken);
        var library = new FakeRecallEmbedLibrary { OpenCode = 2 };
        using var runtime = new TextModelRuntime(directory, library);
        var act = () => runtime.GetEncoder();
        var ex = act.Should().Throw<InvalidOperationException>();
        ex.Which.Message.Should().Be("recall_embed open failed with code 2.");
    }

    [Fact]
    public async Task Ensure_SecondCall_OpensOnce()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        await File.WriteAllTextAsync(Path.Combine(directory, new TextModelDownloadOptions().ModelFileName), "x", TestContext.Current.CancellationToken);
        var library = new FakeRecallEmbedLibrary { OpenCode = 0 };
        using var runtime = new TextModelRuntime(directory, library);
        runtime.GetEncoder();
        runtime.GetEncoder();
        library.OpenCalls.Should().Be(1);
    }

    [Fact]
    public async Task Count_Cancelled_Throws()
    {
        var library = new FakeRecallEmbedLibrary();
        var counter = new LlamaTextTokenCounter(library);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var act = () => counter.CountTokens("red planet", cts.Token);
        act.Should().Throw<OperationCanceledException>();
        library.CountCalls.Should().Be(0);
    }

    [Fact]
    public async Task LazyEncoder_Forwards()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        await File.WriteAllTextAsync(Path.Combine(directory, new TextModelDownloadOptions().ModelFileName), "x", TestContext.Current.CancellationToken);
        var library = new FakeRecallEmbedLibrary { OpenCode = 0, EmbedFill = 1f };
        using var runtime = new TextModelRuntime(directory, library);
        var lazy = new LlamaTextEmbeddingEncoderLazy(runtime);
        var vector = await lazy.EmbedPrefixedTextAsync("task: search result | query: red planet", TestContext.Current.CancellationToken);
        vector.Length.Should().Be(768);
    }

    [Fact]
    public async Task Dispose_ClosesOnce()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        await File.WriteAllTextAsync(Path.Combine(directory, new TextModelDownloadOptions().ModelFileName), "x", TestContext.Current.CancellationToken);
        var library = new FakeRecallEmbedLibrary { OpenCode = 0, EmbedFill = 1f };
        var runtime = new TextModelRuntime(directory, library);
        var encoder = runtime.GetEncoder();
        runtime.Dispose();
        library.CloseCalls.Should().Be(1);
        var act = () => encoder.EmbedPrefixedTextAsync("task: search result | query: mars", TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Defaults_AreGguF()
    {
        var options = new TextModelDownloadOptions();
        options.ModelFileName.Should().Be("embeddinggemma-2-Q8_0.gguf");
        options.ModelBaseUrl.Should().Be("https://huggingface.co/ggml-org/embeddinggemma-2-GGUF/resolve/main/");
    }

    [Fact]
    public void AppSettings_TextModel_IsGguF()
    {
        var repoRoot = FindRepoRoot();
        var json = File.ReadAllText(Path.Combine(repoRoot, "src", "Recall.Maui", "appsettings.json"));
        json.Should().Contain("embeddinggemma-2-Q8_0.gguf");
        json.Should().NotContain("model_quantized.onnx");
        var options = new TextModelDownloadOptions();
        json.Should().Contain(options.ModelSha256);
        options.ModelSha256.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    private static double L2Norm(ReadOnlySpan<float> vector)
    {
        double sum = 0;
        foreach (var value in vector)
        {
            sum += value * value;
        }

        return Math.Sqrt(sum);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Recall.slnx was not found.");
    }

    internal sealed class FakeRecallEmbedLibrary : IRecallEmbedLibrary
    {
        public int OpenCode { get; set; }

        public int EmbedCode { get; set; }

        public float EmbedFill { get; set; } = 1f;

        public int CountResult { get; set; }

        public int OpenCalls { get; private set; }

        public int CloseCalls { get; private set; }

        public int EmbedCalls { get; private set; }

        public int CountCalls { get; private set; }

        public int OpenVisionCode { get; set; }

        public int EmbedVisionCode { get; set; }

        public Action? OnEmbedVision { get; set; }

        public int OpenVisionCalls { get; private set; }

        public int EmbedVisionCalls { get; private set; }

        public int CloseVisionCalls { get; private set; }

        public string? LastMmprojPath { get; private set; }

        public int OpenAudioCode { get; set; }

        public int EmbedAudioCode { get; set; }

        public Action? OnEmbedAudio { get; set; }

        public int OpenAudioCalls { get; private set; }

        public int EmbedAudioCalls { get; private set; }

        public int CloseAudioCalls { get; private set; }

        public string? LastAudioMmprojPath { get; private set; }

        public bool TextModelOpen { get; private set; } = true;

        public int Open(string modelPath)
        {
            OpenCalls++;
            return OpenCode;
        }

        public void Close()
        {
            CloseCalls++;
            TextModelOpen = false;
        }

        public int CountTokens(string text)
        {
            CountCalls++;
            return CountResult;
        }

        public int TokenEndsCode { get; set; }

        public int[] TokenEndResult { get; set; } = [];

        public int TokenEndsCalls { get; private set; }

        public int[] TokenEnds(string text)
        {
            TokenEndsCalls++;
            if (TokenEndsCode != 0)
            {
                throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture, RecallEmbedResults.TokenEndsFailedFormat, TokenEndsCode));
            }

            return TokenEndResult;
        }

        public int Embed(string text, float[] destination768)
        {
            EmbedCalls++;
            for (var i = 0; i < destination768.Length; i++)
            {
                destination768[i] = EmbedFill;
            }

            return EmbedCode;
        }

        public int OpenVision(string mmprojPath)
        {
            OpenVisionCalls++;
            LastMmprojPath = mmprojPath;
            if (mmprojPath is null)
            {
                return 1;
            }

            return OpenVisionCode;
        }

        public int EmbedVision(byte[] jpeg, float[] destination768)
        {
            EmbedVisionCalls++;
            OnEmbedVision?.Invoke();
            for (var i = 0; i < destination768.Length; i++)
            {
                destination768[i] = EmbedFill;
            }

            return EmbedVisionCode;
        }

        public void CloseVision() => CloseVisionCalls++;

        public int OpenAudio(string mmprojPath)
        {
            OpenAudioCalls++;
            LastAudioMmprojPath = mmprojPath;
            if (mmprojPath is null)
            {
                return 1;
            }

            return OpenAudioCode;
        }

        public int EmbedAudio(float[] samples, float[] destination768)
        {
            EmbedAudioCalls++;
            OnEmbedAudio?.Invoke();
            for (var i = 0; i < destination768.Length; i++)
            {
                destination768[i] = EmbedFill;
            }

            return EmbedAudioCode;
        }

        public void CloseAudio() => CloseAudioCalls++;
    }
}
