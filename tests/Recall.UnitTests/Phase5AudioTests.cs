using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using Recall.Adapters.Embedding;
using Recall.Adapters.Indexing;
using Recall.Core.Checkpoints;
using Recall.Core.Download;
using Recall.Core.Indexing;
using Recall.Core.Ingest;
using Recall.Core.Ocr;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.Core.Vocabulary;
using Recall.UI.Pages;
using Recall.UnitTests.Fakes;
using ZVec.NET;

namespace Recall.UnitTests;

public sealed class Phase5AudioTests
{
    [Fact]
    public void AudioModelOptions_NamesMmprojAndHash()
    {
        var options = new AudioModelDownloadOptions();
        AudioModelDownloadOptions.SectionName.Should().Be("AudioModel");
        options.ModelFileName.Should().Be("mmproj-embeddinggemma-2-Q8_0.gguf");
        options.ModelSha256.Should().Be("c4a8a52691ecef40618438928bdf9e68379b854e24166f292592353db0aab64f");
        options.RelativeModelDirectory.Should().Be("models/vision");
        options.ModelBaseUri.ToString().Should().Be("https://huggingface.co/ggml-org/embeddinggemma-2-GGUF/resolve/main/");
    }

    [Fact]
    public void AudioDownload_ReadyFile_DoesNotStart()
    {
        AudioDownloadJoinPolicy.Decide(TextModelDownloadPhase.NotStarted, true).Should().Be(AudioDownloadJoin.UseReady);
        AudioDownloadJoinPolicy.Decide(TextModelDownloadPhase.Ready, false).Should().Be(AudioDownloadJoin.UseReady);
    }

    [Fact]
    public void AudioDownload_InFlight_Waits()
    {
        AudioDownloadJoinPolicy.Decide(TextModelDownloadPhase.Downloading, false).Should().Be(AudioDownloadJoin.WaitForCurrent);
        AudioDownloadJoinPolicy.Decide(TextModelDownloadPhase.Verifying, false).Should().Be(AudioDownloadJoin.WaitForCurrent);
    }

    [Fact]
    public void AudioDownload_Absent_StartsVision()
    {
        AudioDownloadJoinPolicy.Decide(TextModelDownloadPhase.NotStarted, false).Should().Be(AudioDownloadJoin.StartVision);
        AudioDownloadJoinPolicy.Decide(TextModelDownloadPhase.Cancelled, false).Should().Be(AudioDownloadJoin.StartVision);
    }

    [Fact]
    public void FakeAudioDownload_Ready_DoesNotTouchVision()
    {
        var fake = new FakeModelDownloadService
        {
            AudioSnapshot = new TextModelDownloadSnapshot(TextModelDownloadPhase.Downloading, 5, null),
            VisionSnapshot = new TextModelDownloadSnapshot(TextModelDownloadPhase.Ready, 0, null),
        };
        fake.DownloadAudioModelAsync(null, CancellationToken.None).IsCompletedSuccessfully.Should().BeTrue();
        fake.AudioSnapshot.Phase.Should().Be(TextModelDownloadPhase.Ready);
        fake.VisionDownloadCalls.Should().Be(0);
    }

    [Fact]
    public void FakeAudioDownload_InFlight_CopiesVisionSnapshot()
    {
        var fake = new FakeModelDownloadService
        {
            VisionSnapshot = new TextModelDownloadSnapshot(TextModelDownloadPhase.Downloading, 7, null),
        };
        fake.DownloadAudioModelAsync(null, CancellationToken.None).IsCompletedSuccessfully.Should().BeTrue();
        fake.AudioSnapshot.Should().Be(fake.VisionSnapshot);
        fake.VisionDownloadCalls.Should().Be(0);
    }

    [Fact]
    public void FakeAudioDownload_Absent_StartsVisionAndCopies()
    {
        var fake = new FakeModelDownloadService
        {
            VisionSnapshot = new TextModelDownloadSnapshot(TextModelDownloadPhase.NotStarted, 0, null),
        };
        fake.DownloadAudioModelAsync(null, CancellationToken.None).IsCompletedSuccessfully.Should().BeTrue();
        fake.VisionDownloadCalls.Should().Be(1);
        fake.AudioSnapshot.Should().Be(fake.VisionSnapshot);
    }

    [Fact]
    public void AudioWindow_SameBytes_MatchesRawSha()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var insert = MediaIndexInsert.AudioWindow(
            bytes, "C:\\music\\talk.wav", "hello", new float[768], "mmproj-hash", true, "grant", 1, "0", "11.2");
        insert.ContentId.Should().Be(ContentIdBuilder.Build(Modality.Audio, sha, StoredWidth.W768, 1));
        insert.EncoderHash.Should().Be("mmproj-hash");
        insert.SourcePath.Should().Be("C:\\music\\talk.wav");
        insert.StartSeconds.Should().Be("0");
    }

    [Fact]
    public void Create_Audio_Accepts768()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase5_zvec_" + Guid.NewGuid().ToString("N"));
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            var act = () => index.Create(MediaCollections.Audio, MediaCollections.Width);
            act.Should().NotThrow();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Create_Width128_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase5_zvec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            var act = () => index.Create(MediaCollections.Audio, 128);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Create_TextName_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase5_zvec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var index = new ZVecMediaVectorIndex(root);
            var act = () => index.Create(ZVecTextVectorIndex.CollectionName, MediaCollections.Width);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Only picture, video, and audio collections are media collections. (Parameter 'collectionName')");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SearchWords_EmptyQuery_DoesNotQuery()
    {
        var fake = new FakeMediaVectorIndex();
        var hits = fake.SearchWords(MediaCollections.Audio, "   ", "filter", 64, CancellationToken.None);
        hits.Should().BeEmpty();
        fake.WordsQueries.Should().BeEmpty();
        fake.QueriedCollections.Should().BeEmpty();
    }

    [Fact]
    public void AudioNative_RefusesWhileVisionOpen()
    {
        var open = FunctionBody(NativeSource(), "recall_embed_audio_open");
        open.Should().Contain("g_vision");
        open.Should().Contain("return 10;");
        open.Should().Contain("mtmd_init_from_file(mmproj_path, g_model, params)");
        var embed = FunctionBody(NativeSource(), "recall_embed_audio_embed");
        embed.Should().Contain("mtmd_get_marker");
        embed.Should().Contain("mtmd_bitmap_init_from_audio");
        embed.Should().Contain("MTMD_INPUT_CHUNK_TYPE_AUDIO");
        NativeSource().Should().Contain("void recall_embed_audio_close(void)");
        NativeSource().Should().Contain("mtmd_free(g_audio)");
        NativeSource().Should().Contain("#define RECALL_EMBED_MAX_TOKENS 7680");
    }

    [Fact]
    public void IsAudio_M4a_IsTrue()
    {
        MediaFileKinds.IsAudio("talk.m4a").Should().BeTrue();
    }

    [Fact]
    public void IsAudio_Mp4_IsFalse()
    {
        MediaFileKinds.IsAudio("talk.mp4").Should().BeFalse();
    }

    [Fact]
    public void IsAudio_Null_IsFalse()
    {
        MediaFileKinds.IsAudio(null!).Should().BeFalse();
    }

    [Fact]
    public void IsVideo_Mp4_IsTrue()
    {
        MediaFileKinds.IsVideo("talk.mp4").Should().BeTrue();
    }

    [Fact]
    public void IsImage_Jpg_IsTrue()
    {
        MediaFileKinds.IsImage("shot.JPG").Should().BeTrue();
    }

    [Fact]
    public void MediaFileKinds_Blank_IsFalse()
    {
        MediaFileKinds.IsAudio("").Should().BeFalse();
        MediaFileKinds.IsVideo("").Should().BeFalse();
        MediaFileKinds.IsImage("").Should().BeFalse();
    }

    [Fact]
    public void Hop_Native11200_Is5600()
    {
        AudioWindows.HopMilliseconds(11200).Should().Be(5600);
    }

    [Fact]
    public void Hop_NativeOne_IsOne()
    {
        AudioWindows.HopMilliseconds(1).Should().Be(1);
    }

    [Fact]
    public void Windows_HopIsHalfNative_SecondStartsAtHalf()
    {
        var windows = AudioWindows.Plan(22400);
        windows[0].Should().Be(new AudioWindow(0, 11200));
        windows[1].StartMilliseconds.Should().Be(5600);
    }

    [Fact]
    public void Windows_ShortTail_ExtendsLastWindow()
    {
        var windows = AudioWindows.Plan(16900);
        windows.Should().HaveCount(2);
        windows[^1].EndMilliseconds.Should().Be(16900);
        windows.Select(static window => window.StartMilliseconds).Should().NotContain(11200);
    }

    [Fact]
    public void Windows_ZeroDuration_IsEmpty()
    {
        AudioWindows.Plan(0).Should().BeEmpty();
        AudioWindows.Plan(-1).Should().BeEmpty();
    }

    [Fact]
    public void Windows_ShorterThanNative_IsOneWindow()
    {
        var windows = AudioWindows.Plan(1000);
        windows.Should().ContainSingle().Which.EndMilliseconds.Should().Be(1000);
    }

    [Fact]
    public void Windows_OneFile_KeepsMaxRrfScore()
    {
        var best = Collapse.EarliestStart(
        [
            new("a", "talk.wav", 0, 11.2, 0.2),
            new("b", "talk.wav", 5.6, 11.2, 0.9),
        ]);
        best.Score.Should().Be(0.9);

        var tie = Collapse.EarliestStart(
        [
            new("a", "talk.wav", 4, 11.2, 0.5),
            new("b", "talk.wav", 2, 11.2, 0.5),
        ]);
        tie.StartSeconds.Should().Be(2);
    }

    [Fact]
    public void Seconds_5600_IsFivePointSix()
    {
        AudioWindows.Seconds(5600).Should().Be("5.6");
        AudioWindows.Seconds(11200).Should().Be("11.2");
    }

    [Fact]
    public void Wav_HeaderShort_Throws()
    {
        var file = ""u8.ToArray();
        var action = () => WavPcm.Read(file);
        action.Should().Throw<InvalidDataException>().WithMessage("WAV header is short.");
    }

    [Fact]
    public void Wav_NotRiff_Throws()
    {
        var file = BuildWav(1, 16000, 1, 16, []);
        file[0] = (byte)'X';
        var action = () => WavPcm.Read(file);
        action.Should().Throw<InvalidDataException>().WithMessage("WAV form is not RIFF.");
    }

    [Fact]
    public void Wav_NotPcm_Throws()
    {
        var file = BuildWav(7, 16000, 1, 16, [], includeData: false);
        var action = () => WavPcm.Read(file);
        action.Should().Throw<InvalidDataException>().WithMessage("WAV encoding is not 16-bit PCM.");
    }

    [Fact]
    public void Wav_NoData_Throws()
    {
        var file = BuildWav(1, 16000, 1, 16, [], includeData: false);
        var action = () => WavPcm.Read(file);
        action.Should().Throw<InvalidDataException>().WithMessage("WAV data chunk is missing.");
    }

    [Fact]
    public void Wav_Mono16_IsUnitRange()
    {
        var file = BuildWav(1, 16000, 1, 16, [0x00, 0x40]);
        var samples = WavPcm.Read(file);
        samples.Should().HaveCount(1);
        samples[0].Should().Be(0.5f);
        WavPcm.IsWav(file).Should().BeTrue();
    }

    [Fact]
    public void Wav_ExtensiblePcm_IsAccepted()
    {
        var samples = new byte[]
        {
            0x01, 0x00, 0x02, 0x00, 0x03, 0x00,
        };
        var tag1 = WavPcm.Read(BuildWav(1, 16000, 2, 16, samples));
        var extensible = WavPcm.Read(BuildWav(0xFFFE, 16000, 2, 16, samples));
        extensible.Should().Equal(tag1);
    }

    [Fact]
    public void Wav_ExtensibleNonPcm_Throws()
    {
        var file = BuildWav(0xFFFE, 16000, 1, 16, [], includeData: false, subFormatFloat: true);
        var action = () => WavPcm.Read(file);
        action.Should().Throw<InvalidDataException>().WithMessage("WAV encoding is not 16-bit PCM.");
    }

    [Fact]
    public void Wav_NotWav_IsWavFalse()
    {
        WavPcm.IsWav(new byte[] { 1, 2, 3 }).Should().BeFalse();
    }

    [Fact]
    public void Resample_SameRate_ReturnsSameReference()
    {
        var source = new float[] { 0.1f, 0.2f, 0.3f };
        AudioResample.To16k(source, AudioWindows.SampleRate).Should().BeSameAs(source);
    }

    [Fact]
    public void Resample_Null_Throws()
    {
        var action = () => AudioResample.To16k(null!, 16000);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Resample_ZeroRate_Throws()
    {
        var action = () => AudioResample.To16k([0.1f], 0);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Resample_Empty_IsEmpty()
    {
        AudioResample.To16k([], 16000).Should().BeEmpty();
    }

    [Fact]
    public void Resample_Long44100_MappedSample_Is0625()
    {
        var source = new float[60000];
        source[49998] = 1f;
        var output = AudioResample.To16k(source, 44100);
        output.Should().HaveCount(21768);
        output[18140].Should().BeApproximately(0.625f, 1e-4f);
    }

    [Fact]
    public void Extract_Srt_DropsTimestamp()
    {
        var raw = "1\n00:00:01,000 --> 00:00:02,000\nHello\n\n2\n00:00:03,000 --> 00:00:04,000\nBye\n";
        SiblingTranscript.ExtractCues(".srt", raw).Should().Be("Hello\nBye");
    }

    [Fact]
    public void Extract_Vtt_DropsHeader()
    {
        var raw = "WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHello\n\nNOTE until blank\nstill note\n\n00:00:03.000 --> 00:00:04.000\nAgain\n";
        var cues = SiblingTranscript.ExtractCues(".vtt", raw);
        cues.Should().NotContain("WEBVTT");
        cues.Should().Contain("Hello");
        cues.Should().Contain("Again");
    }

    [Fact]
    public void Extract_Sbv_DropsTimestamp()
    {
        var raw = "0:00:01.000,0:00:02.000\nHello\n\n0:00:03.000,0:00:04.000\nAgain\n";
        SiblingTranscript.ExtractCues(".sbv", raw).Should().Be("Hello\nAgain");
    }

    [Fact]
    public void Extract_Ass_KeepsDialogueText()
    {
        var raw = "[Script Info]\n[V4+ Styles]\nDialogue: 0,0:00:01.18,0:00:02.20,Default,,0000,0000,0000,,{\\k10}Hello, world\n";
        SiblingTranscript.ExtractCues(".ass", raw).Should().Be("Hello, world");
        SiblingTranscript.ExtractCues(".ssa", raw).Should().Be("Hello, world");
    }

    [Fact]
    public void Extract_Ttml_UsesParagraph()
    {
        var raw = "<tt xmlns=\"http://www.w3.org/ns/ttml\"><body><p>Hello</p><p> Again </p></body></tt>";
        SiblingTranscript.ExtractCues(".ttml", raw).Should().Be("Hello\nAgain");
    }

    [Fact]
    public void Extract_Ttml_BadXml_IsEmpty()
    {
        SiblingTranscript.ExtractCues(".ttml", "<tt><p>Broken").Should().BeEmpty();
    }

    [Fact]
    public void Extract_Null_Throws()
    {
        var action = () => SiblingTranscript.ExtractCues(".srt", null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Extract_UnknownExtension_Throws()
    {
        var action = () => SiblingTranscript.ExtractCues(".txt", "text");
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Matches_LanguageTag_IsTrue()
    {
        SiblingTranscript.Matches("talk.mp4", "talk.en.vtt").Should().BeTrue();
        SiblingTranscript.Matches("talk.mp4", "talk.srt").Should().BeTrue();
        SiblingTranscript.IsCaptionFile("talk.ar.vtt").Should().BeTrue();
    }

    [Fact]
    public void Matches_TwoExtraDots_IsFalse()
    {
        SiblingTranscript.Matches("talk.mp4", "talk.extra.notes.vtt").Should().BeFalse();
    }

    [Fact]
    public void Matches_Null_IsFalse()
    {
        SiblingTranscript.Matches(null!, "talk.en.vtt").Should().BeFalse();
        SiblingTranscript.Matches("talk.mp4", null!).Should().BeFalse();
        SiblingTranscript.IsCaptionFile(null!).Should().BeFalse();
    }

    [Fact]
    public void EmbeddedCaption_NotRead()
    {
        SiblingTranscript.ReadsEmbeddedTrack.Should().BeFalse();
    }

    [Fact]
    public void Caption_EnumsSortByUtf8()
    {
        var ordered = SiblingTranscript.Order(["talk2.en.vtt", "talk1.en.vtt"]);
        ordered[0].Should().Be("talk1.en.vtt");
        var prefix = SiblingTranscript.Order(["talk.en.vtt", "talk"]);
        prefix[0].Should().Be("talk");
    }

    [Fact]
    public void Join_Empty_IsEmpty()
    {
        SiblingTranscript.Join([]).Should().BeEmpty();
    }

    [Fact]
    public void AudioEmbed_OpensSessionOnce()
    {
        var library = new LlamaEmbedTests.FakeRecallEmbedLibrary();
        using var runtime = new AudioModelRuntime("models/audio", library, new AudioModelDownloadOptions());
        runtime.EmbedPcm(new float[16], new float[768]);
        runtime.EmbedPcm(new float[16], []);
        library.OpenAudioCalls.Should().Be(1);
        library.EmbedAudioCalls.Should().Be(2);
        library.CloseAudioCalls.Should().Be(0);
        runtime.IsLoaded.Should().BeTrue();
        library.LastAudioMmprojPath.Should().Be(Path.Combine("models/audio", "mmproj-embeddinggemma-2-Q8_0.gguf"));
    }

    [Fact]
    public void AudioEmbed_Throw_LeavesSessionOpen()
    {
        var library = new LlamaEmbedTests.FakeRecallEmbedLibrary { EmbedAudioCode = 2 };
        using var runtime = new AudioModelRuntime("models/audio", library, new AudioModelDownloadOptions());
        var act = () => runtime.EmbedPcm(new float[16], new float[768]);
        act.Should().Throw<InvalidOperationException>().WithMessage("recall_embed audio embed failed with code 2.");
        runtime.IsLoaded.Should().BeTrue();
        library.CloseAudioCalls.Should().Be(0);
        runtime.CloseAudio();
        library.CloseAudioCalls.Should().Be(1);
        runtime.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public void AudioEmbed_ClosesVisionFirst()
    {
        var wasOneContext = MediaContextPolicy.OneContext;
        try
        {
            var library = new LlamaEmbedTests.FakeRecallEmbedLibrary { OpenAudioCode = 10 };
            using var closed = new AudioModelRuntime("models/audio", library, new AudioModelDownloadOptions());
            MediaContextPolicy.OneContext = false;
            var act = () => closed.EmbedPcm(new float[16], new float[768]);
            act.Should().Throw<InvalidOperationException>().WithMessage("recall_embed audio open failed with code 10.");
            library.OpenAudioCalls.Should().Be(1);
            library.EmbedAudioCalls.Should().Be(0);
            library.CloseAudioCalls.Should().Be(0);
            library.CloseVisionCalls.Should().Be(0);

            var shared = new LlamaEmbedTests.FakeRecallEmbedLibrary { OpenAudioCode = 10 };
            using var runtime = new AudioModelRuntime("models/audio", shared, new AudioModelDownloadOptions());
            MediaContextPolicy.OneContext = true;
            runtime.EmbedPcm(new float[16], new float[768]);
            shared.OpenAudioCalls.Should().Be(0);
            shared.EmbedAudioCalls.Should().Be(1);
            shared.CloseVisionCalls.Should().Be(0);
            runtime.IsLoaded.Should().BeTrue();
        }
        finally
        {
            MediaContextPolicy.OneContext = wasOneContext;
        }
    }

    [Fact]
    public void AudioEmbed_NullDestination_Throws()
    {
        var library = new LlamaEmbedTests.FakeRecallEmbedLibrary();
        using var runtime = new AudioModelRuntime("models/audio", library, new AudioModelDownloadOptions());
        var act = () => runtime.EmbedPcm(new float[16], null!);
        act.Should().Throw<ArgumentNullException>();
        library.OpenAudioCalls.Should().Be(0);
    }

    [Fact]
    public void AudioEmbed_Disposed_Throws()
    {
        var library = new LlamaEmbedTests.FakeRecallEmbedLibrary();
        var runtime = new AudioModelRuntime("models/audio", library, new AudioModelDownloadOptions());
        runtime.Dispose();
        var act = () => runtime.EmbedPcm(new float[16], new float[768]);
        act.Should().Throw<ObjectDisposedException>();
        library.CloseAudioCalls.Should().Be(0);
    }

    [Fact]
    public void AudioQueryGate_Ready_Searches()
    {
        AudioQueryGate.Decide(TextModelDownloadPhase.Ready).Should().Be(PictureQueryDecision.Search);
    }

    [Fact]
    public void AudioQueryGate_Downloading_Waits()
    {
        AudioQueryGate.Decide(TextModelDownloadPhase.NotStarted).Should().Be(PictureQueryDecision.Wait);
        AudioQueryGate.Decide(TextModelDownloadPhase.Downloading).Should().Be(PictureQueryDecision.Wait);
    }

    [Fact]
    public void AudioQueryGate_Cancelled_Fails()
    {
        AudioQueryGate.Decide(TextModelDownloadPhase.Verifying).Should().Be(PictureQueryDecision.Fail);
        AudioQueryGate.Decide(TextModelDownloadPhase.HashFailed).Should().Be(PictureQueryDecision.Fail);
        AudioQueryGate.Decide(TextModelDownloadPhase.Interrupted).Should().Be(PictureQueryDecision.Fail);
        AudioQueryGate.Decide(TextModelDownloadPhase.Cancelled).Should().Be(PictureQueryDecision.Fail);
    }

    [Fact]
    public async Task AudioQuery_EmptyPcm_ReturnsEmpty()
    {
        var encoder = new FakeAudioEmbeddingEncoder();
        var service = new AudioSearchService(encoder, new FakeMediaVectorIndex(), new FakeEncoderFileHash { AudioHash = "audio-hash" });
        var grants = new List<ScopeFolderGrant> { new(@"C:\med", "med", GrantFileKinds.Audio) };
        var hits = await service.FindAsync(ReadOnlyMemory<float>.Empty, grants, TestContext.Current.CancellationToken);
        hits.Should().BeEmpty();
        encoder.EmbedCalls.Should().Be(0);
    }

    [Fact]
    public async Task AudioQuery_NoAudioGrant_ReturnsEmpty()
    {
        var encoder = new FakeAudioEmbeddingEncoder();
        var service = new AudioSearchService(encoder, new FakeMediaVectorIndex(), new FakeEncoderFileHash { AudioHash = "audio-hash" });
        var grants = new List<ScopeFolderGrant> { new(@"C:\notes", "notes", GrantFileKinds.Text) };
        var hits = await service.FindAsync(IngestTestKit.SixteenKFloats, grants, TestContext.Current.CancellationToken);
        hits.Should().BeEmpty();
        encoder.EmbedCalls.Should().Be(0);
    }

    [Fact]
    public async Task AudioQuery_StaysInsideGrant()
    {
        var encoder = new FakeAudioEmbeddingEncoder();
        var mediaIndex = new FakeMediaVectorIndex
        {
            OnSearch = _ =>
            [
                new MediaSearchHit("inside", @"C:\med\talk.wav", 0, 11.2, 3),
                new MediaSearchHit("outside", @"D:\elsewhere\song.wav", 1, 3, 9),
            ],
        };
        var service = new AudioSearchService(encoder, mediaIndex, new FakeEncoderFileHash { AudioHash = "audio-hash" });
        var grants = new List<ScopeFolderGrant> { new(@"C:\med", "med", GrantFileKinds.Audio) };
        var hits = await service.FindAsync(IngestTestKit.SixteenKFloats, grants, TestContext.Current.CancellationToken);
        hits.Should().ContainSingle();
        hits[0].ContentId.Should().Be("inside");
        hits[0].SourcePath.Should().Be(@"C:\med\talk.wav");
        var request = mediaIndex.Requests.Should().ContainSingle().Subject;
        request.CollectionName.Should().Be(MediaCollections.Audio);
        request.Filter.Should().Contain(@"C:\med");
        request.Filter.Should().Contain("audio-hash");
        request.QueryVector768.Span[0].Should().Be(1);
    }

    [Fact]
    public async Task AudioQuery_Cancels_ThrowsBeforeEmbed()
    {
        var encoder = new FakeAudioEmbeddingEncoder();
        var service = new AudioSearchService(encoder, new FakeMediaVectorIndex(), new FakeEncoderFileHash { AudioHash = "audio-hash" });
        var grants = new List<ScopeFolderGrant> { new(@"C:\med", "med", GrantFileKinds.Audio) };
        var act = () => service.FindAsync(
            IngestTestKit.SixteenKFloats,
            grants,
            new CancellationToken(canceled: true));
        await act.Should().ThrowAsync<OperationCanceledException>();
        encoder.EmbedCalls.Should().Be(0);
    }

    [Fact]
    public async Task AudioQuery_CollapsesToEarliestWindow()
    {
        var encoder = new FakeAudioEmbeddingEncoder();
        var mediaIndex = new FakeMediaVectorIndex
        {
            OnSearch = _ =>
            [
                new MediaSearchHit("later", @"C:\med\talk.wav", 5.6, 11.2, 0.9),
                new MediaSearchHit("earlier", @"C:\med\talk.wav", 0, 5.6, 0.4),
            ],
        };
        var service = new AudioSearchService(encoder, mediaIndex, new FakeEncoderFileHash { AudioHash = "audio-hash" });
        var grants = new List<ScopeFolderGrant> { new(@"C:\med", "med", GrantFileKinds.Audio) };
        var hits = await service.FindAsync(IngestTestKit.SixteenKFloats, grants, TestContext.Current.CancellationToken);
        hits.Should().ContainSingle();
        hits[0].StartSeconds.Should().Be(5.6);
    }

    [Fact]
    public async Task AudioQuery_RanksByScoreThenStart()
    {
        var encoder = new FakeAudioEmbeddingEncoder();
        var mediaIndex = new FakeMediaVectorIndex
        {
            OnSearch = _ =>
            [
                new MediaSearchHit("weak", @"C:\med\talk.wav", 8, 11.2, 3),
                new MediaSearchHit("strong", @"C:\med\song.wav", 2, 5.6, 9),
            ],
        };
        var service = new AudioSearchService(encoder, mediaIndex, new FakeEncoderFileHash { AudioHash = "audio-hash" });
        var grants = new List<ScopeFolderGrant> { new(@"C:\med", "med", GrantFileKinds.Audio) };
        var hits = await service.FindAsync(IngestTestKit.SixteenKFloats, grants, TestContext.Current.CancellationToken);
        hits.Select(h => h.ContentId).Should().Equal("strong", "weak");
    }

    [Fact]
    public void OpenHit_SeeksToWindowStart()
    {
        var hit = new MediaSearchHit("aud1", @"C:\med\talk.wav", 4, 11.2, 2);
        MediaHitStart.Seconds(hit).Should().Be(4);
        MediaHitStart.Seeks(MediaHitStart.Seconds(hit)).Should().BeTrue();
    }

    [Fact]
    public void OpenHit_UsesWinningRowStart()
    {
        var start4 = new MediaSearchHit("at4", @"C:\med\talk.wav", 4, 11.2, 0.5);
        var start2 = new MediaSearchHit("at2", @"C:\med\talk.wav", 2, 5.6, 0.5);
        Collapse.EarliestStart([start4, start2]).Should().Be(start2);
        Collapse.EarliestStart([start2, start4]).Should().Be(start2);
        var strong = new MediaSearchHit("at4", @"C:\med\talk.wav", 4, 11.2, 0.9);
        Collapse.EarliestStart([strong, start2]).Should().Be(strong);
        Collapse.EarliestStart([start2, strong]).Should().Be(strong);
    }

    [Fact]
    public void OpenHit_StartFour_PlaysAtFour()
    {
        var hit = new TextSearchHit(
            "aud1",
            @"C:\med\talk.wav",
            "talk.wav",
            DateTimeOffset.UnixEpoch,
            TextMatchReason.FullText,
            2,
            0,
            "",
            "",
            false,
            4);
        var view = TextFindSession.Build([hit], _ => true);
        view.Immediate.Should().ContainSingle();
        view.Immediate[0].StartSeconds.Should().Be(4);
        MediaHitStart.Seeks(view.Immediate[0].StartSeconds).Should().BeTrue();
    }

    [Fact]
    public void OpenHit_TextCard_DoesNotSeek()
    {
        MediaHitStart.Seeks(0).Should().BeFalse();
        MediaHitStart.Seeks(-0.5).Should().BeFalse();
    }

    [Fact]
    public async Task TranscriptWords_OpenMediaFile()
    {
        var encoder = new FakeTextEmbeddingEncoder();
        var index = new FakeTextVectorIndex();
        var mediaIndex = new FakeMediaVectorIndex();
        mediaIndex.OnSearchWords = (collection, query, topK) =>
            collection == MediaCollections.Audio
                ? [new MediaSearchHit("aud1", @"C:\med\talk.wav", 4, 5.6, 2)]
                : [];
        var service = IngestTestKit.CreateSearchService(
            encoder,
            index,
            mediaIndex: mediaIndex,
            encoderFileHash: new FakeEncoderFileHash { AudioHash = "audio-hash" });
        var grants = new List<ScopeFolderGrant> { new(@"C:\med", "med", GrantFileKinds.Audio) };
        var hits = await service.FindAsync("booth", grants, TestContext.Current.CancellationToken);
        hits.Should().ContainSingle();
        hits[0].ContentId.Should().Be("aud1");
        hits[0].SourcePath.Should().Be(@"C:\med\talk.wav");
        hits[0].FileName.Should().Be("talk.wav");
        hits[0].MatchReason.Should().Be(TextMatchReason.FullText);
        hits[0].StartSeconds.Should().Be(4);
        hits[0].Score.Should().Be(2);
        hits[0].Ordinal.Should().Be(0);
        encoder.Calls.Should().Be(0);
        mediaIndex.WordsQueries.Should().HaveCount(2);
        mediaIndex.WordsRequests[0].Collection.Should().Be(MediaCollections.Audio);
        mediaIndex.WordsRequests[0].Query.Should().Be("booth");
        mediaIndex.WordsRequests[0].Filter.Should().Contain(@"C:\med");
        mediaIndex.WordsRequests[0].Filter.Should().Contain("audio-hash");
        mediaIndex.WordsRequests[1].Collection.Should().Be(MediaCollections.Video);
    }

    [Fact]
    public async Task MediaQuery_ImageOnlyGrant_IsEmpty()
    {
        var encoder = new FakeTextEmbeddingEncoder();
        var mediaIndex = new FakeMediaVectorIndex();
        var service = IngestTestKit.CreateSearchService(
            encoder,
            new FakeTextVectorIndex(),
            mediaIndex: mediaIndex,
            encoderFileHash: new FakeEncoderFileHash { AudioHash = "audio-hash" });
        var grants = new List<ScopeFolderGrant> { new(@"C:\pics", "pics", GrantFileKinds.Image) };
        var hits = await service.FindAsync("booth", grants, TestContext.Current.CancellationToken);
        hits.Should().BeEmpty();
        encoder.Calls.Should().Be(0);
        mediaIndex.WordsQueries.Should().BeEmpty();
    }

    [Fact]
    public async Task AudioOn_Wav_InsertsWindowsAndUnloads()
    {
        var kit = CreateAudioIngest(TalkWav());
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        kit.Index.Inserts.Should().ContainSingle();
        var insert = kit.Index.Inserts[0];
        insert.FileName.Should().Be("talk.wav");
        insert.SourcePath.Should().Be(@"C:\med\talk.wav");
        insert.StartSeconds.Should().Be("0");
        insert.EndSeconds.Should().Be("11.2");
        insert.EncoderHash.Should().Be("audio-hash");
        insert.GrantPath.Should().Be(@"C:\med");
        insert.Words.Should().BeEmpty();
        insert.Vector768.Length.Should().Be(MediaCollections.Width);
        kit.Encoder.EmbedCalls.Should().Be(1);
        kit.Encoder.CloseCalls.Should().Be(1);
        kit.Encoder.IsLoaded.Should().BeFalse();
        kit.Report.ReportCalls.Should().Be(0);
        kit.Index.CreatedCollections.Should().Contain(MediaCollections.Audio);
    }

    [Fact]
    public async Task TranscriptWords_AreCarriedIntoInsert()
    {
        var wav = TalkWav();
        var kit = CreateAudioIngest(wav);
        kit.Siblings.Next = [new SiblingFile("talk.srt", "1\n00:00:01,000 --> 00:00:03,000\nbooth boots\n")];
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        kit.Siblings.LastMediaPath.Should().Be(@"C:\med\talk.wav");
        kit.Index.Inserts.Should().ContainSingle().Which.Words.Should().Be("booth boots");
    }

    [Fact]
    public async Task TranscriptWords_MultipleLanguages_AppendInNameOrder()
    {
        var wav = TalkWav();
        var kit = CreateAudioIngest(wav);
        kit.Siblings.Next =
        [
            new SiblingFile("talk.en.srt", "1\n00:00:01,000 --> 00:00:02,000\nbooth\n"),
            new SiblingFile("talk.ar.srt", "1\n00:00:01,000 --> 00:00:02,000\nأحمد\n"),
        ];
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        kit.Index.Inserts.Should().ContainSingle().Which.Words.Should().Be("أحمد\nbooth");
    }

    [Fact]
    public async Task Caption_Srt_IsSkippedBeforeDecoding()
    {
        var kit = CreateAudioIngest(TalkWav());
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.srt", TestContext.Current.CancellationToken);
        kit.Source.ReadCalls.Should().Be(0);
        kit.Source.ReadCompressedCalls.Should().Be(0);
        kit.Encoder.EmbedCalls.Should().Be(0);
        kit.Index.Inserts.Should().BeEmpty();
    }

    [Fact]
    public async Task AudioOff_ReportsAndSkips()
    {
        var kit = CreateAudioIngest(TalkWav());
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Text);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        kit.Report.ReportCalls.Should().Be(1);
        kit.Report.Shown.Should().BeTrue();
        kit.Source.ReadCalls.Should().Be(0);
        kit.Source.ReadCompressedCalls.Should().Be(0);
        kit.Encoder.EmbedCalls.Should().Be(0);
        kit.Encoder.CloseCalls.Should().Be(0);
        kit.Index.Inserts.Should().BeEmpty();
    }

    [Fact]
    public async Task CompressedAudio_NotDecoded_DoesNotEmbed()
    {
        var kit = CreateAudioIngest([1, 2, 3]);
        kit.Source.OnReadCompressed = new NotSupportedException();
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.m4a", TestContext.Current.CancellationToken);
        kit.Source.ReadCompressedCalls.Should().Be(1);
        kit.Encoder.EmbedCalls.Should().Be(0);
        kit.Encoder.CloseCalls.Should().Be(0);
        kit.Index.Inserts.Should().BeEmpty();
    }

    [Fact]
    public async Task AudioIngest_ThrowDuringEmbed_LeavesSessionOpen()
    {
        var kit = CreateAudioIngest(TalkWav());
        kit.Encoder.OnEmbed = (_, _) => throw new InvalidOperationException("engine");
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        var act = () => kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(act);
        kit.Encoder.CloseCalls.Should().Be(0);
        kit.Encoder.EmbedCalls.Should().Be(1);
    }

    [Fact]
    public async Task AudioOn_Wav_WritesFileRowAndWindowChunkRows()
    {
        var kit = CreateAudioIngest(TalkWav());
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        var fileRow = kit.Cursors.GetFile(@"C:\med\talk.wav");
        fileRow.Should().NotBeNull();
        fileRow.State.Should().Be(IngestCursorState.Finished);
        fileRow.EncoderHash.Should().Be("audio-hash");
        fileRow.GrantPath.Should().Be(@"C:\med");
        var chunks = kit.Cursors.ListChunks(@"C:\med\talk.wav");
        chunks.Should().ContainSingle();
        chunks[0].Ordinal.Should().Be(0);
        chunks[0].Retired.Should().BeFalse();
        chunks[0].ContentId.Should().Be(kit.Index.Inserts[0].ContentId);
        kit.Cursors.AnyLivePath(kit.Index.Inserts[0].ContentId).Should().BeTrue();
    }

    [Fact]
    public async Task AudioIngest_SameBytes_SkipsReembed()
    {
        var kit = CreateAudioIngest(TalkWav());
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        kit.Encoder.EmbedCalls.Should().Be(1);
        kit.Encoder.CloseCalls.Should().Be(1);
        kit.Index.Inserts.Should().ContainSingle();
    }

    [Fact]
    public async Task AudioIngest_ChangedBytes_TryDeletesRetiredWindows()
    {
        var kit = CreateAudioIngest(TalkWav());
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        var oldId = kit.Index.Inserts[0].ContentId;
        var changed = TalkWav();
        changed[200] = (byte)(changed[200] + 1);
        kit.Source.NextBytes = changed;
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        kit.Encoder.EmbedCalls.Should().Be(2);
        kit.Index.Inserts.Should().ContainSingle(i => i.ContentId != oldId);
        kit.Index.Inserts.Should().NotContain(i => i.ContentId == oldId);
        kit.Cursors.PendingCount().Should().Be(1);
    }

    [Fact]
    public async Task Apply_DeletedAudio_EnqueuesWindowIdsThroughAliasPath()
    {
        var kit = CreateAudioIngest(TalkWav());
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
        var windowId = kit.Index.Inserts[0].ContentId;
        var opLogDir = Path.Combine(Path.GetTempPath(), "recall-audio-oplog-" + Guid.NewGuid().ToString("N"));
        var opLog = new LocalOpLogWriter(opLogDir);
        var applier = new ScopeChangeApplier(kit.Cursors, new FakeGrantedTextIngestService(), opLog);
        try
        {
            await applier.ApplyAsync(
                new ScopeChange(ScopeChangeKind.Deleted, @"C:\med\talk.wav", null),
                grant,
                null,
                TestContext.Current.CancellationToken);
            kit.Cursors.GetFile(@"C:\med\talk.wav")!.State.Should().Be(IngestCursorState.Missing);
            kit.Cursors.AnyLivePath(windowId).Should().BeFalse();
            kit.Cursors.ListPending(10).Should().Contain(windowId);
            opLog.Entries.Should().Contain(e => e.Operation == OpLogOperation.Delete && e.ContentId == windowId);
        }
        finally
        {
            Directory.Delete(opLogDir, recursive: true);
        }
    }

    [Fact]
    public async Task Find_AfterAudioDelete_ReconcilesMediaPathLiveToZero()
    {
        var kit = CreateAudioIngest(TalkWav());
        var grant = new ScopeFolderGrant(@"C:\med", "med", GrantFileKinds.Audio);
        await kit.Ingest.IndexAsync(grant, @"C:\med\talk.wav", TestContext.Current.CancellationToken);
            var opLogDir = Path.Combine(Path.GetTempPath(), "recall-audio-oplog-" + Guid.NewGuid().ToString("N"));
            var applier = new ScopeChangeApplier(kit.Cursors, new FakeGrantedTextIngestService(), new LocalOpLogWriter(opLogDir));

        try
        {
            await applier.ApplyAsync(
                new ScopeChange(ScopeChangeKind.Deleted, @"C:\med\talk.wav", null),
                grant,
                null,
                TestContext.Current.CancellationToken);
            var service = IngestTestKit.CreateSearchService(
                new FakeTextEmbeddingEncoder(),
                new FakeTextVectorIndex(),
                cursors: kit.Cursors,
                mediaIndex: kit.Index);
            var hits = await service.FindAsync("booth", [grant], TestContext.Current.CancellationToken);
            hits.Should().BeEmpty();
            kit.Cursors.PendingCount().Should().Be(0);
            kit.Index.ReconcileCalls.Should().Be(1);
            kit.Index.Inserts.Should().ContainSingle().Which.PathLive.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(opLogDir, recursive: true);
        }
    }

    [Fact]
    public void ZVecMedia_Reconcile_FlipsPathLiveOnDeletedAudioRow()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall_phase5_zvec_" + Guid.NewGuid().ToString("N"));
        try
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var vector = new float[MediaCollections.Width];
            vector[0] = 0.5f;
            var insert = MediaIndexInsert.AudioWindow(
                bytes, @"C:\med\talk.wav", "booth boots", vector, "mmproj-hash", true, @"C:\med", 0, "0", "11.2");
            using var index = new ZVecMediaVectorIndex(root);
            index.Create(MediaCollections.Audio, MediaCollections.Width);
            index.TryInsert(MediaCollections.Audio, insert).Should().BeTrue();
            index.StoredField(MediaCollections.Audio, insert.ContentId, ZVecMediaVectorIndex.PathLiveField).Should().Be("1");
            index.Reconcile([new VectorLiveState(insert.ContentId, false, "mmproj-hash", @"C:\med")]);
            index.StoredField(MediaCollections.Audio, insert.ContentId, ZVecMediaVectorIndex.PathLiveField).Should().Be("0");
            index.StoredField(MediaCollections.Audio, insert.ContentId, ZVecMediaVectorIndex.EncoderHashField).Should().Be("mmproj-hash");
            index.Reconcile([new VectorLiveState(insert.ContentId, true, "other", @"C:\med")]);
            index.StoredField(MediaCollections.Audio, insert.ContentId, ZVecMediaVectorIndex.PathLiveField).Should().Be("1");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task IngestGrant_AudioOnly_ListsWav()
    {
        var grantPath = Path.Combine(Path.GetTempPath(), "recall-phase5-grant-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(grantPath);
        try
        {
            var store = new FakeScopeGrantStore();
            var scope = new ScopeGrantService(store);
            scope.TryAddGrant(new ScopeFolderGrant(grantPath, "media", GrantFileKinds.Audio));
            var reader = new ReadCountingReader();
            var index = new FakeTextVectorIndex();
            var (engine, opLog, cursors) = IngestTestKit.CreateEngineParts(new FakeTextEmbeddingEncoder(), index, null);
            var audio = new FakeAudioFileIngest();
            var service = IngestTestKit.CreateGrantedService(scope, reader, engine, cursors, index, opLog, null, audio);
            await service.IngestGrantAsync(grantPath, TestContext.Current.CancellationToken);
            reader.ReadCalls.Should().Be(0);
            audio.IndexGrantCalls.Should().Be(1);
            audio.IndexAsyncCalls.Should().Be(0);
            audio.IndexedPaths.Should().BeEmpty();
            index.Inserts.Should().BeEmpty();
            cursors.IsGrantFinished(grantPath).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(grantPath, recursive: true);
        }
    }

    [Fact]
    public void MediaFileKinds_AudioAccept_IncludesSafeTypes()
    {
        MediaFileKinds.AudioAccept.Should().Be(".mp3,.m4a,.wav,.flac,.ogg,.aac");
    }

    [Fact]
    public void HomeRazor_HasAudioControlWithoutMicrophone()
    {
        var razor = File.ReadAllText(
            Path.Combine(
                RepoRoot(),
                "src",
                "Recall.UI",
                "Pages",
                "Find",
                "Home.razor"),
            Encoding.UTF8);
        razor.Should().Contain("Search_WithAudio");
        razor.Should().Contain("accept=\"@MediaFileKinds.AudioAccept\"");
        razor.Should().Contain("Search_AudioOff");
        razor.Should().NotContain("microphone");
        razor.Should().NotContain("Microphone");
    }

    [Fact]
    public void StringsResx_AudioKeys_AreLocalized()
    {
        var root = RepoRoot();
        var en = ResxMap(Path.Combine(root, "src", "Recall.UI", "Resources", "Strings.resx"));
        var ar = ResxMap(Path.Combine(root, "src", "Recall.UI", "Resources", "Strings.ar.resx"));
        en["Search_WithAudio"].Should().Be("Search with an audio file");
        en["Search_AudioWaiting"].Should().Be("The audio model is still downloading.");
        en["Search_AudioFailed"].Should().Be("The audio model did not download.");
        en["Search_AudioOff"].Should().Be("Audio files are off for this folder.");
        ar["Search_WithAudio"].Should().NotBeNullOrEmpty();
        ar["Search_AudioWaiting"].Should().NotBeNullOrEmpty();
        ar["Search_AudioFailed"].Should().NotBeNullOrEmpty();
        ar["Search_AudioOff"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PlatformAudio_DecodersAreSourceLocked()
    {
        var mediaDir = Path.Combine(RepoRoot(), "src", "Recall.Maui", "Platform", "Media");
        var windows = File.ReadAllText(Path.Combine(mediaDir, "WindowsAudioSource.cs"), Encoding.UTF8);
        windows.Should().Contain("MediaTranscoder");
        windows.Should().Contain("Task.Run");
        windows.Should().Contain("CreateWav");
        windows.Should().Contain("AudioDecoders.FailureMessage");
        var android = File.ReadAllText(Path.Combine(mediaDir, "AndroidAudioSource.cs"), Encoding.UTF8);
        android.Should().Contain("MediaExtractor");
        android.Should().Contain("MediaCodec");
        android.Should().Contain("MonoDownMix");
        android.Should().Contain("AudioResample.To16k");
        var apple = File.ReadAllText(Path.Combine(mediaDir, "AppleAudioSource.cs"), Encoding.UTF8);
        apple.Should().Contain("AVAssetReader");
        apple.Should().Contain("AVAssetReaderTrackOutput");
        apple.Should().Contain("16000");
    }

    private static byte[] TalkWav()
    {
        const int SampleCount = 179_200;
        var samples = new byte[SampleCount * 2];
        for (var i = 0; i < SampleCount; i++)
        {
            var amplitude = (short)(i % 1000 * 4);
            samples[2 * i] = (byte)amplitude;
            samples[(2 * i) + 1] = (byte)(amplitude >> 8);
        }

        return BuildWav(1, 16000, 1, 16, samples);
    }

    private static (AudioFileIngest Ingest, FakeAudioSource Source, FakeSiblingFiles Siblings, FakeAudioEmbeddingEncoder Encoder, FakeMediaVectorIndex Index, FakeAudioOffReport Report, FakeIngestCursorStore Cursors) CreateAudioIngest(byte[]? sourceBytes)
    {
        var source = new FakeAudioSource { NextBytes = sourceBytes };
        var siblings = new FakeSiblingFiles();
        var encoder = new FakeAudioEmbeddingEncoder();
        var index = new FakeMediaVectorIndex();
        var report = new FakeAudioOffReport();
        var cursors = new FakeIngestCursorStore();
        var ingest = new AudioFileIngest(
            source,
            siblings,
            encoder,
            index,
            report,
            new FakeEncoderFileHash { AudioHash = "audio-hash" },
            new EmptyAudioListReader(),
            cursors);
        return (ingest, source, siblings, encoder, index, report, cursors);
    }

    private static Dictionary<string, string> ResxMap(string path)
    {
        var document = XDocument.Load(path);
        return document.Root!
            .Elements("data")
            .ToDictionary(
                element => element.Attribute("name")!.Value,
                element => element.Element("value")!.Value,
                StringComparer.Ordinal);
    }

    private sealed class EmptyAudioListReader : IGrantedTextReader
    {
        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<string> ListAudioAndVideoAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class ReadCountingReader : IGrantedTextReader
    {
        public int ReadCalls { get; private set; }

        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            ReadCalls++;
            yield break;
        }

        public async IAsyncEnumerable<string> ListAudioAndVideoAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public async Task Scheduler_SwitchesModelOncePerPass()
    {
        OcrPassStub.Reset();
        var wasOneContext = MediaContextPolicy.OneContext;
        try
        {
            MediaContextPolicy.OneContext = true;
            var sharedOpens = await RunSchedulerPassAndCollectOpensAsync(TestContext.Current.CancellationToken);
            sharedOpens.Should().Equal("text", "vision");
            OcrPassStub.OpenCount.Should().Be(1);

            OcrPassStub.Reset();
            MediaContextPolicy.OneContext = false;
            var splitOpens = await RunSchedulerPassAndCollectOpensAsync(TestContext.Current.CancellationToken);
            splitOpens.Should().Equal("text", "vision", "audio");
            OcrPassStub.OpenCount.Should().Be(1);
        }
        finally
        {
            MediaContextPolicy.OneContext = wasOneContext;
        }
    }

    [Fact]
    public async Task Scheduler_BurstOf50Events_OneOpen()
    {
        var scheduler = new IngestScheduler();
        var runner = new IngestSchedulerRunner(scheduler);
        for (var i = 0; i < 50; i++)
        {
            scheduler.Enqueue(new IngestJob(IngestModel.Text, $"note{i}.md"));
        }

        var textOpens = 0;
        var hooks = new IngestPassHooks
        {
            OpenText = () => textOpens++,
            ProcessTextJobAsync = (_, _) => Task.CompletedTask,
        };
        await runner.RunReadyPassesAsync(DateTimeOffset.UtcNow, hooks, TestContext.Current.CancellationToken);
        textOpens.Should().Be(1);
        scheduler.QueueCount(IngestModel.Text).Should().Be(0);
    }

    private static async Task<List<string>> RunSchedulerPassAndCollectOpensAsync(CancellationToken cancellationToken)
    {
        var scheduler = new IngestScheduler();
        var runner = new IngestSchedulerRunner(scheduler);
        for (var i = 0; i < 5; i++)
        {
            scheduler.Enqueue(new IngestJob(IngestModel.Text, $"note{i}.md"));
            scheduler.Enqueue(new IngestJob(IngestModel.Media, $"pic{i}.jpg"));
            scheduler.Enqueue(new IngestJob(IngestModel.Media, $"aud{i}.wav"));
            scheduler.Enqueue(new IngestJob(IngestModel.Ocr, $"scan{i}.pdf"));
        }

        var opens = new List<string>();
        var hooks = new IngestPassHooks
        {
            OpenText = () => opens.Add("text"),
            OpenVision = () => opens.Add("vision"),
            OpenAudio = () => opens.Add("audio"),
            ProcessTextJobAsync = (_, _) => Task.CompletedTask,
            ProcessMediaJobAsync = (_, _) => Task.CompletedTask,
            ProcessOcrJob = _ => { },
        };
        await runner.RunReadyPassesAsync(DateTimeOffset.UtcNow.AddSeconds(31), hooks, cancellationToken);
        return opens;
    }

    private static byte[] BuildWav(ushort format, int rate, short channels, short bits, byte[] samples, bool includeData = true, bool subFormatFloat = false)
    {
        var fmt = new List<byte>();
        Put16(fmt, format);
        Put16(fmt, channels);
        Put32(fmt, rate);
        Put32(fmt, rate * channels * 2);
        Put16(fmt, (short)(channels * 2));
        Put16(fmt, bits);
        if (format == 0xFFFEu)
        {
            Put16(fmt, (short)22);
            Put16(fmt, bits);
            Put32(fmt, 0);
            fmt.AddRange(subFormatFloat
                ?
                [
                // KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = 00000003-0000-0010-8000-00aa00389b71
                    0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71,
                ]
                : (IEnumerable<byte>)
                [
                    0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71,
                ]);
        }

        var bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes("RIFF"));
        var total = 4 + 8 + fmt.Count;
        if (includeData)
        {
            total += 8 + samples.Length;
        }

        Put32(bytes, total);
        bytes.AddRange(Encoding.ASCII.GetBytes("WAVE"));
        bytes.AddRange(Encoding.ASCII.GetBytes("fmt "));
        Put32(bytes, fmt.Count);
        bytes.AddRange(fmt);
        if (includeData)
        {
            bytes.AddRange(Encoding.ASCII.GetBytes("data"));
            Put32(bytes, samples.Length);
            bytes.AddRange(samples);
        }

        return bytes.ToArray();
    }

    private static void Put16(ICollection<byte> target, short value) => Put16(target, (ushort)value);

    private static void Put16(ICollection<byte> target, ushort value)
    {
        target.Add((byte)value);
        target.Add((byte)(value >> 8));
    }

    private static void Put32(ICollection<byte> target, int value)
    {
        target.Add((byte)value);
        target.Add((byte)(value >> 8));
        target.Add((byte)(value >> 16));
        target.Add((byte)(value >> 24));
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
}
