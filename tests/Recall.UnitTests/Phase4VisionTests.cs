using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Recall.Adapters.Media;
using Recall.UI.Localization;
using FluentAssertions;
using Recall.Adapters.Indexing;
using Recall.Core.Download;
using Recall.Core.Indexing;
using Recall.Core.Ingest;
using Recall.Core.Ocr;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Text;
using Recall.Core.Vocabulary;
using Recall.UnitTests.Fakes;
using ZVec.NET;

namespace Recall.UnitTests;

public sealed class Phase4VisionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "recall_phase4_" + Guid.NewGuid().ToString("N"));

    public Phase4VisionTests()
    {
        Directory.CreateDirectory(root);
    }

    [Fact]
    public void VisionOptions_FileName_IsQ8Mmproj()
    {
        new VisionModelDownloadOptions().ModelFileName.Should().Be("mmproj-embeddinggemma-2-Q8_0.gguf");
    }

    [Fact]
    public void VisionOptions_Sha256_IsHubOid()
    {
        new VisionModelDownloadOptions().ModelSha256
            .Should().Be("c4a8a52691ecef40618438928bdf9e68379b854e24166f292592353db0aab64f");
    }

    [Fact]
    public void VisionOpen_NullPath_Returns1()
    {
        var library = new LlamaEmbedTests.FakeRecallEmbedLibrary
        {
            EmbedVisionCode = 1,
            OpenVisionCode = 0,
        };
        var opened = library.OpenVision(null!);
        opened.Should().Be(1);
        library.OpenVisionCalls.Should().Be(1);
        library.EmbedVisionCalls.Should().Be(0);
        library.CloseCalls.Should().Be(0);
        library.TextModelOpen.Should().BeTrue();
    }

    [Fact]
    public void VisionOpen_TextNotOpen_Returns6()
    {
        var function = FunctionBody(NativeSource(), "recall_embed_vision_open");
        function.Should().Contain("if (g_model == NULL)");
        function.Should().Contain("return 6;");
        function.Should().Contain("mtmd_init_from_file(mmproj_path, g_model, params)");
    }

    [Fact]
    public void VisionOpen_AlreadyOpen_Returns8()
    {
        var function = FunctionBody(NativeSource(), "recall_embed_vision_open");
        function.Should().Contain("if (g_vision != NULL)");
        function.Should().Contain("return 8;");
    }

    [Fact]
    public void VisionEmbed_WhenClosed_Returns4()
    {
        var function = FunctionBody(NativeSource(), "recall_embed_vision_embed");
        function.Should().Contain("if (g_vision == NULL)");
        function.Should().Contain("return 4;");
    }

    [Fact]
    public void VisionEmbed_WidthNot768_Returns5()
    {
        var function = FunctionBody(NativeSource(), "recall_embed_vision_embed");
        var widthCheck = function.IndexOf("llama_model_n_embd_inp(g_model) != RECALL_EMBED_DIM", StringComparison.Ordinal);
        var firstWrite = function.IndexOf("out768[", StringComparison.Ordinal);
        widthCheck.Should().BeGreaterThanOrEqualTo(0);
        firstWrite.Should().BeGreaterThan(widthCheck);
        function.Should().Contain("return 5;");
    }

    [Fact]
    public void Vision_UnloadedAfterJob()
    {
        var directory = CreateModelDirectory();
        var library = new LlamaEmbedTests.FakeRecallEmbedLibrary();
        using var runtime = new VisionRuntimeHarness(directory, library);
        runtime.Runtime.EmbedJpeg(new byte[] { 1, 2, 3 }, new float[MediaCollections.Width]);
        runtime.Runtime.IsLoaded.Should().BeFalse();
        library.OpenVisionCalls.Should().Be(1);
        library.CloseVisionCalls.Should().Be(1);
    }

    [Fact]
    public void Vision_Throw_StillUnloads()
    {
        var directory = CreateModelDirectory();
        var library = new LlamaEmbedTests.FakeRecallEmbedLibrary
        {
            OnEmbedVision = () => throw new InvalidOperationException("vision failed"),
        };
        using var runtime = new VisionRuntimeHarness(directory, library);
        var act = () => runtime.Runtime.EmbedJpeg(new byte[] { 1 }, new float[MediaCollections.Width]);
        act.Should().Throw<InvalidOperationException>();
        runtime.Runtime.IsLoaded.Should().BeFalse();
        library.CloseVisionCalls.Should().Be(1);
    }

    [Fact]
    public void Create_Width128_Throws()
    {
        using var index = new ZVecMediaVectorIndex(root);
        var act = () => index.Create(MediaCollections.Image, RecallIndexPolicy.BlockedWidth);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ItemsText_Throws()
    {
        using var index = new ZVecMediaVectorIndex(root);
        var act = () => index.Create(ZVecTextVectorIndex.CollectionName, MediaCollections.Width);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_QuantizeType_StaysUndefined()
    {
        AssertUndefined(ZVecMediaVectorIndex.BuildSchema(MediaCollections.Image, MediaCollections.Width));
        AssertUndefined(ZVecMediaVectorIndex.BuildSchema(MediaCollections.Video, MediaCollections.Width));
    }

    [Fact]
    public void PictureRow_NotInTextCollection()
    {
        var databaseRoot = Path.Combine(root, "picture");
        using var index = new ZVecMediaVectorIndex(databaseRoot);
        index.Create(MediaCollections.Image, MediaCollections.Width);
        var bytes = new byte[] { 1, 2, 3 };
        var row = MediaIndexInsert.Picture(bytes, "C:\\grant\\photo.jpg", "photo", Vector(), "hash", true, "C:\\G");
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        row.ContentId.Should().Be(ContentIdBuilder.Build(Modality.Image, hash, StoredWidth.W768, 0));
        var frame = MediaIndexInsert.VideoFrame(bytes, "C:\\grant\\clip.mp4", "frame", Vector(), "hash", true, "C:\\G", 3, "2", "4");
        frame.ContentId.Should().Be(ContentIdBuilder.Build(Modality.Video, hash, StoredWidth.W768, 3));
        index.TryInsert(MediaCollections.Image, row).Should().BeTrue();
        Directory.GetDirectories(databaseRoot).Select(Path.GetFileName).Should().Equal(MediaCollections.Image);
        index.Search(new MediaSearchRequest(MediaCollections.Image, Vector(), string.Empty, 8), TestContext.Current.CancellationToken).Should().ContainSingle();
    }

    [Fact]
    public void PictureRow_StartAndEndUnset()
    {
        var databaseRoot = Path.Combine(root, "picture-unset");
        using var index = new ZVecMediaVectorIndex(databaseRoot);
        index.Create(MediaCollections.Image, MediaCollections.Width);
        var bytes = new byte[] { 4, 5, 6 };
        var row = MediaIndexInsert.Picture(bytes, "C:\\grant\\photo.jpg", "photo", Vector(), "hash", true, "C:\\G");
        index.TryInsert(MediaCollections.Image, row).Should().BeTrue();
        index.Search(new MediaSearchRequest(MediaCollections.Image, Vector(), string.Empty, 8), TestContext.Current.CancellationToken).Should().ContainSingle();
        index.StoredField(MediaCollections.Image, row.ContentId, ZVecMediaVectorIndex.StartSecondsField).Should().BeEmpty();
        index.StoredField(MediaCollections.Image, row.ContentId, ZVecMediaVectorIndex.EndSecondsField).Should().BeEmpty();
    }

    [Fact]
    public void FrameTie_KeepsEarlierStart()
    {
        var best = Collapse.EarliestStart(
        [
            new MediaSearchHit("a", "v", 4, 6, 0.5),
            new MediaSearchHit("b", "v", 2, 4, 0.5),
        ]);
        best.StartSeconds.Should().Be(2);
    }

    [Fact]
    public async Task Frames_OneVideo_OneFileInTop20()
    {
        var index = new FakeMediaVectorIndex
        {
            OnSearch = request => request.CollectionName == MediaCollections.Video
                ?
                [
                    new MediaSearchHit("a", "v", 0, 2, 0.4),
                    new MediaSearchHit("b", "v", 2, 4, 0.9),
                    new MediaSearchHit("c", "v", 4, 6, 0.2),
                ]
                : [],
        };
        var service = new PictureSearchService(new FakeVisionEmbeddingEncoder(), index, new FakeEncoderFileHash());
        var hits = await service.FindAsync(
            new byte[] { 1 },
            [new ScopeFolderGrant("G", "G", GrantFileKinds.Video)],
            TestContext.Current.CancellationToken);
        hits.Should().ContainSingle().Which.ContentId.Should().Be("b");
    }

    [Fact]
    public void Frames_OneVideo_KeepsMaxRrfScore()
    {
        Collapse.EarliestStart(
        [
            new MediaSearchHit("a", "v", 0, 2, 0.2),
            new MediaSearchHit("b", "v", 2, 4, 0.9),
        ]).Score.Should().Be(0.9);
    }

    [Fact]
    public void FrameCadence_FiveSeconds_StartsAtZeroTwoFour()
    {
        FrameCadence.Starts(5).Should().Equal(0d, 2d, 4d);
    }

    [Fact]
    public void FrameCadence_ZeroDuration_IsEmpty()
    {
        FrameCadence.Starts(0).Should().BeEmpty();
    }

    [Fact]
    public void FrameCadence_NullStream_Throws()
    {
        var sampler = new WindowsVideoFrameSampler();
        var act = () => sampler.SampleEveryTwoSeconds(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task PictureQuery_GrantedImage_HitsPictureCollection()
    {
        var encoder = new FakeVisionEmbeddingEncoder();
        var index = new FakeMediaVectorIndex
        {
            OnSearch = request => request.CollectionName == MediaCollections.Image
                ? [new MediaSearchHit("id", "C:\\G\\photo.jpg", 0, 0, 0.8)]
                : [],
        };
        var service = new PictureSearchService(encoder, index, new FakeEncoderFileHash());
        var grants = new[] { new ScopeFolderGrant("C:\\G", "G", GrantFileKinds.Image) };
        var hits = await service.FindAsync(new byte[] { 1, 2 }, grants, TestContext.Current.CancellationToken);
        encoder.Calls.Should().Be(1);
        hits.Should().ContainSingle();
        index.QueriedCollections.Should().Contain(MediaCollections.Image);
        index.QueriedCollections.Should().NotContain(ZVecTextVectorIndex.CollectionName);
    }

    [Fact]
    public async Task PictureQuery_TextQuery_DoesNotEmbedVision()
    {
        var vision = new FakeVisionEmbeddingEncoder();
        var text = new FakeTextEmbeddingEncoder();
        var search = IngestTestKit.CreateSearchService(text, new FakeTextVectorIndex(), vision: vision);
        await search.FindAsync(
            "mars",
            [new ScopeFolderGrant("G", "G", GrantFileKinds.Text)],
            TestContext.Current.CancellationToken);
        text.Calls.Should().BeGreaterThan(0);
        vision.Calls.Should().Be(0);
        search.VisionEncoder.Should().BeSameAs(vision);
    }

    [Fact]
    public async Task PictureFind_OutOfGrant_DoesNotTakeASlot()
    {
        var encoder = new FakeVisionEmbeddingEncoder();
        var index = new FakeMediaVectorIndex
        {
            OnSearch = request =>
            {
                request.Filter.Should().Contain("G");
                request.Filter.Should().NotContain("other");
                request.TopK.Should().Be(RecallIndexCaps.RawListCap);
                return
                [
                    new MediaSearchHit("inside", "G/inside.jpg", 0, 0, 0.8),
                ];
            },
        };
        var service = new PictureSearchService(encoder, index, new FakeEncoderFileHash { TextHash = "h" });
        var grants = new[]
        {
            new ScopeFolderGrant("G", "G", GrantFileKinds.Image),
            new ScopeFolderGrant("other", "other", GrantFileKinds.Text),
        };
        var hits = await service.FindAsync(new byte[] { 1 }, grants, TestContext.Current.CancellationToken);
        hits.Should().ContainSingle().Which.SourcePath.Should().Be("G/inside.jpg");
    }

    [Fact]
    public async Task PictureFind_NoGrants_ReturnsEmpty()
    {
        var encoder = new FakeVisionEmbeddingEncoder();
        var index = new FakeMediaVectorIndex();
        var service = new PictureSearchService(encoder, index, new FakeEncoderFileHash());
        (await service.FindAsync(new byte[] { 1 }, [], TestContext.Current.CancellationToken)).Should().BeEmpty();
        index.Requests.Should().BeEmpty();
        encoder.Calls.Should().Be(0);
    }

    [Fact]
    public void PictureQueryGate_Downloading_Waits()
    {
        PictureQueryGate.Decide(TextModelDownloadPhase.Downloading).Should().Be(PictureQueryDecision.Wait);
        PictureQueryGate.Decide(TextModelDownloadPhase.NotStarted).Should().Be(PictureQueryDecision.Wait);
        PictureQueryGate.Decide(TextModelDownloadPhase.Verifying).Should().Be(PictureQueryDecision.Fail);
        PictureQueryGate.Decide(TextModelDownloadPhase.HashFailed).Should().Be(PictureQueryDecision.Fail);
        PictureQueryGate.Decide(TextModelDownloadPhase.Interrupted).Should().Be(PictureQueryDecision.Fail);
        PictureQueryGate.Decide(TextModelDownloadPhase.Cancelled).Should().Be(PictureQueryDecision.Fail);
    }

    [Fact]
    public void PictureQueryGate_Ready_Searches()
    {
        PictureQueryGate.Decide(TextModelDownloadPhase.Ready).Should().Be(PictureQueryDecision.Search);
    }

    [Fact]
    public void OcrReaders_MissingHash_DoesNotLoad()
    {
        OcrReaderGate.CanLoad(null).Should().BeFalse();
        OcrReaderGate.CanLoad(string.Empty).Should().BeFalse();
        OcrReaderGate.CanLoad(" ").Should().BeFalse();
        OcrReaderGate.CanLoad("not-a-real-hash").Should().BeFalse();
        OcrReaderGate.CanLoad(OcrModelFiles.Detector.Sha256).Should().BeTrue();
        OcrReaderGate.CanLoad(OcrModelFiles.EnglishReader.Sha256).Should().BeTrue();
        OcrReaderGate.CanLoad(OcrModelFiles.ArabicReader.Sha256).Should().BeTrue();
        OcrModelFiles.GraphInputName.Should().Be("x");
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "Recall.Core", "Ocr", "OcrReaderGate.cs"))
            .Should().NotContain("InferenceSession");
    }

    [Fact]
    public void OcrFiles_EnglishRec_IsV6Small()
    {
        OcrModelFiles.EnglishReader.LocalName.Should().Be("ppocrv6-small-rec.onnx");
        OcrModelFiles.EnglishReader.Sha256.Should().Be("5435fd747c9e0efe15a96d0b378d5bd157e9492ed8fd80edf08f30d02fa24634");
    }

    [Fact]
    public void OcrFiles_ArabicRec_IsV5Mobile()
    {
        OcrModelFiles.ArabicReader.LocalName.Should().Be("arabic-ppocrv5-mobile-rec.onnx");
        OcrModelFiles.ArabicReader.Sha256.Should().Be("799113ebf267fbe742deb99eb36e8d42c9ddc5291ceacf92add41b4d52a59110");
    }

    [Fact]
    public void OcrFiles_Detector_IsV6Small()
    {
        OcrModelFiles.Detector.LocalName.Should().Be("ppocrv6-small-det.onnx");
        OcrModelFiles.Detector.Sha256.Should().Be("d73e0058b7a8086bbd57f3d10b8bcd4ff95363f67e06e2762b5e814fe9c9410e");
    }

    [Fact]
    public void OcrFiles_NoFourthFile()
    {
        OcrModelFiles.LocalNames.Should().Equal(
            "ppocrv6-small-det.onnx",
            "ppocrv6-small-rec.onnx",
            "arabic-ppocrv5-mobile-rec.onnx");
    }

    [Fact]
    public void Choose_EnglishHigher_KeepsEnglish()
    {
        OcrLineChoice.Choose("hello", 0.9, "مرحبا", 0.2).Should().Be("hello");
        OcrLineChoice.Choose("hello", 0.9, "", 0.1).Should().Be("hello");
        OcrLineChoice.Choose("", 0.1, "مرحبا", 0.9).Should().Be("مرحبا");
    }

    [Fact]
    public void Choose_ArabicHigher_KeepsArabic()
    {
        OcrLineChoice.Choose("hello", 0.2, "مرحبا", 0.9).Should().Be("مرحبا");
    }

    [Fact]
    public void Choose_Tie_KeepsEnglish()
    {
        OcrLineChoice.Choose("hello", 0.5, "مرحبا", 0.5).Should().Be("hello");
    }

    [Fact]
    public void Choose_BothEmpty_ReturnsEmpty()
    {
        OcrLineChoice.Choose("", 0.5, "", 0.4).Should().BeEmpty();
        OcrLineChoice.Choose("", 0.9, "مرحبا", 0.1).Should().BeEmpty();
    }

    [Fact]
    public void Choose_NullText_BecomesEmpty()
    {
        OcrLineChoice.Choose(null, 0.9, null, 0.1).Should().BeEmpty();
        OcrLineChoice.Choose(null, 0.99, "مرحبا", 0.2).Should().Be("مرحبا");
        OcrLineChoice.Choose("hello", 0.2, null, 0.99).Should().Be("hello");
    }

    [Fact]
    public void EndsSlice_At8192Tokens_IsTrue()
    {
        PdfSlicePolicy.EndsSlice(8192, 1).Should().BeTrue();
    }

    [Fact]
    public void EndsSlice_At10Pages_IsTrue()
    {
        PdfSlicePolicy.EndsSlice(1, 10).Should().BeTrue();
    }

    [Fact]
    public void EndsSlice_NinePagesAnd8191Tokens_IsFalse()
    {
        PdfSlicePolicy.EndsSlice(8191, 9).Should().BeFalse();
    }

    [Fact]
    public void NeedsOcr_ThreeReplacementChars_IsTrue()
    {
        BrokenArabic.NeedsOcr("مر\uFFFD\uFFFD\uFFFD").Should().BeTrue();
        BrokenArabic.NeedsOcr("\uFFFD\uFFFD\uFFFDم").Should().BeTrue();
    }

    [Fact]
    public void NeedsOcr_TwoReplacementChars_IsFalse()
    {
        BrokenArabic.NeedsOcr("مر\uFFFD\uFFFD").Should().BeFalse();
    }

    [Fact]
    public void NeedsOcr_NoArabicLetter_IsFalse()
    {
        BrokenArabic.NeedsOcr("abc\uFFFD\uFFFD\uFFFD").Should().BeFalse();
        BrokenArabic.NeedsOcr(null).Should().BeFalse();
        BrokenArabic.NeedsOcr(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void Resx_PictureWaiting_ExistsInBothLocales()
    {
        var english = ResxValue(Path.Combine(RepoRoot(), "src", "Recall.UI", "Resources", "Strings.resx"), "Search_PictureWaiting");
        var arabic = ResxValue(Path.Combine(RepoRoot(), "src", "Recall.UI", "Resources", "Strings.ar.resx"), "Search_PictureWaiting");
        english.Should().Be("The picture model is still downloading.");
        arabic.Should().Be("نموذج الصور ما زال يُنزَّل.");
        ResxValue(Path.Combine(RepoRoot(), "src", "Recall.UI", "Resources", "Strings.resx"), "Search_PictureFailed")
            .Should().Be("The picture model did not download.");
        ResxValue(Path.Combine(RepoRoot(), "src", "Recall.UI", "Resources", "Strings.ar.resx"), "Search_PictureFailed")
            .Should().Be("لم يكتمل تنزيل نموذج الصور.");
        ResxValue(Path.Combine(RepoRoot(), "src", "Recall.UI", "Resources", "Strings.resx"), "Search_WithPicture")
            .Should().Be("Search with a picture");
        ResxValue(Path.Combine(RepoRoot(), "src", "Recall.UI", "Resources", "Strings.ar.resx"), "Search_WithPicture")
            .Should().Be("ابحث بصورة");
        RecallText.Get("Search_PictureWaiting", CultureInfo.GetCultureInfo("en"))
            .Should().Be("The picture model is still downloading.");
        RecallText.Get("Search_PictureWaiting", CultureInfo.GetCultureInfo("ar"))
            .Should().Be("نموذج الصور ما زال يُنزَّل.");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _ = exception;
        }
    }

    private string CreateModelDirectory()
    {
        var directory = Path.Combine(root, "vision");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, new VisionModelDownloadOptions().ModelFileName), [1]);
        return directory;
    }

    private static ReadOnlyMemory<float> Vector()
    {
        var vector = new float[MediaCollections.Width];
        vector[0] = 1;
        return vector;
    }

    private static void AssertUndefined(ZVecCollectionSchema schema)
    {
        var vector = schema.Vectors.Single(v => v.Name == ZVecMediaVectorIndex.EmbeddingField);
        var hnsw = vector.IndexParam.Should().BeOfType<ZVecHnswIndexParam>().Subject;
        hnsw.QuantizeType.Should().Be(ZVecQuantizeType.Undefined);
        vector.Dimension.Should().Be(MediaCollections.Width);
    }

    private static string ResxValue(string path, string key)
    {
        var document = XDocument.Load(path);
        return document.Root!.Elements("data")
            .Single(element => element.Attribute("name")!.Value == key)
            .Element("value")!.Value;
    }

    private static string NativeSource() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "native", "recall_embed.c"), Encoding.UTF8);

    private static string FunctionBody(string source, string name)
    {
        var marker = "int " + name + "(";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var next = source.IndexOf("\nRECALL_EMBED_EXPORT", start + marker.Length, StringComparison.Ordinal);
        return next < 0 ? source[start..] : source[start..next];
    }

    private static string RepoRoot()
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

    private sealed class VisionRuntimeHarness : IDisposable
    {
        public VisionRuntimeHarness(string directory, LlamaEmbedTests.FakeRecallEmbedLibrary library)
        {
            Runtime = new Recall.Adapters.Embedding.VisionModelRuntime(directory, library);
        }

        public Recall.Adapters.Embedding.VisionModelRuntime Runtime { get; }

        public void Dispose() => Runtime.Dispose();
    }
}
