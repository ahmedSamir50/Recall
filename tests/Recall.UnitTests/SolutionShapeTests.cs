using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recall.Core.DependencyInjection;
using Recall.Core.Ports;
using Recall.Core.Sync;
using Recall.Core.Vocabulary;
using System.Reflection;
using System.Xml.Linq;

namespace Recall.UnitTests;

public sealed class SolutionShapeTests
{
    private static string RepoRoot => FindRepoRoot();

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

        throw new InvalidOperationException("Recall.slnx was not found above the test output directory.");
    }

    [Fact]
    public void RequiredPorts_WhenCoreAssemblyIsLoaded_ExposesTheSevenPorts()
    {
        // Arrange
        var expected = new[]
        {
            nameof(IFileScopeWatcherService),
            nameof(IScopeManagerService),
            nameof(IVideoFrameSamplerService),
            nameof(ISecureStorageService),
            nameof(IIdleDetectorService),
            nameof(IModelDownloadService),
            nameof(IIndexPolicy),
        };

        // Act
        var typeNames = typeof(IFileScopeWatcherService).Assembly.GetTypes().Select(t => t.Name).ToHashSet();

        // Assert
        typeNames.Should().Contain(expected);
    }

    [Fact]
    public void ForbiddenCapturePorts_WhenSourceIsRead_DoNotExist()
    {
        // Arrange
        var forbidden = new[]
        {
            "IClipboardService",
            "ICameraRollService",
            "IScreenCaptureService",
            "IAudioRecorderService",
        };
        var srcRoot = Path.Combine(RepoRoot, "src");

        // Act
        var hits = Directory
            .EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => (path, text: File.ReadAllText(path)))
            .Where(pair => forbidden.Any(name => pair.text.Contains(name, StringComparison.Ordinal)))
            .Select(pair => pair.path)
            .ToList();

        // Assert
        hits.Should().BeEmpty();
    }

    [Fact]
    public void EachPort_HasAFakeInUnitTests()
    {
        // Arrange
        var testTypes = Assembly.GetExecutingAssembly().GetTypes();
        var portTypes = new[]
        {
            typeof(IFileScopeWatcherService),
            typeof(IScopeManagerService),
            typeof(IVideoFrameSamplerService),
            typeof(ISecureStorageService),
            typeof(IIdleDetectorService),
            typeof(IModelDownloadService),
            typeof(IIndexPolicy),
            typeof(ISyncAdapter),
            typeof(IThemePreferenceStore),
            typeof(IUiLocaleStore),
            typeof(ISystemThemeProvider),
            typeof(IScopeGrantStore),
            typeof(IFolderPickerService),
            typeof(IDeviceRecordService),
            typeof(IDeviceRecordStore),
            typeof(IDeviceOsNameProvider),
            typeof(IAgeCutoffPreferenceStore),
            typeof(IRecallShellKind),
            typeof(IGrantedFolderAccessService),
            typeof(IDeviceIdCandidateProvider),
            typeof(IRandomDeviceIdSource),
            typeof(ITextTokenCounter),
            typeof(ITextEmbeddingEncoder),
            typeof(ITextVectorIndex),
            typeof(IGrantedTextIngestService),
            typeof(ITextSearchService),
            typeof(IOnboardingCompletionStore),
            typeof(IDebugAppReset),
            typeof(IUserSkipStore),
            typeof(IMediaVectorIndex),
            typeof(IVisionEmbeddingEncoder),
            typeof(IPictureSearchService),
        };

        // Act
        var missing = portTypes
            .Where(port => testTypes.All(t => !port.IsAssignableFrom(t)))
            .Select(port => port.Name)
            .ToList();

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void SyncFeature_WhenDefault_IsOff()
    {
        // Act
        var enabled = SyncFeature.Enabled;

        // Assert
        enabled.Should().BeFalse();
    }

    [Fact]
    public void Modality_Members_AreTextImageVideoAudio()
    {
        // Act
        var names = Enum.GetNames<Modality>();

        // Assert
        names.Should().Equal("Text", "Image", "Video", "Audio");
        ((int)Modality.Text).Should().Be(0);
        ((int)Modality.Image).Should().Be(1);
        ((int)Modality.Video).Should().Be(2);
        ((int)Modality.Audio).Should().Be(3);
    }

    [Fact]
    public void StoredWidth_Members_Are768_512_256()
    {
        // Act
        var names = typeof(StoredWidth).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => field.Name)
            .ToArray();

        // Assert
        names.Should().Equal("W768", "W512", "W256");
        ((int)StoredWidth.W768).Should().Be(768);
        ((int)StoredWidth.W512).Should().Be(512);
        ((int)StoredWidth.W256).Should().Be(256);
    }

    [Fact]
    public void AgeCutoff_Members_Are2_5_7()
    {
        // Act
        var names = Enum.GetNames<AgeCutoffYears>();

        // Assert
        names.Should().Equal("Two", "Five", "Seven");
        ((int)AgeCutoffYears.Two).Should().Be(2);
        ((int)AgeCutoffYears.Five).Should().Be(5);
        ((int)AgeCutoffYears.Seven).Should().Be(7);
    }

    [Fact]
    public void OpLogOperation_Members_AreInsertDeletePinUnpin()
    {
        // Act
        var names = Enum.GetNames<OpLogOperation>();

        // Assert
        names.Should().Equal("Insert", "Delete", "Pin", "Unpin");
        ((int)OpLogOperation.Insert).Should().Be(0);
        ((int)OpLogOperation.Delete).Should().Be(1);
        ((int)OpLogOperation.Pin).Should().Be(2);
        ((int)OpLogOperation.Unpin).Should().Be(3);
    }

    [Fact]
    public void BannedSymbols_ListsHttpClientSocketTcpListenerUdpClient()
    {
        // Arrange
        var corePath = Path.Combine(RepoRoot, "src", "Recall.Core", "BannedSymbols.txt");
        var uiPath = Path.Combine(RepoRoot, "src", "Recall.UI", "BannedSymbols.txt");
        var mauiPath = Path.Combine(RepoRoot, "src", "Recall.Maui", "BannedSymbols.txt");
        var coreAndUiRequired = new[] { "HttpClient", "Socket", "TcpListener", "UdpClient" };
        var mauiRequired = new[] { "Socket", "TcpListener", "UdpClient" };

        // Act
        var coreText = File.ReadAllText(corePath);
        var uiText = File.ReadAllText(uiPath);
        var mauiText = File.ReadAllText(mauiPath);

        // Assert
        foreach (var symbol in coreAndUiRequired)
        {
            coreText.Should().Contain(symbol);
            uiText.Should().Contain(symbol);
        }

        foreach (var symbol in mauiRequired)
        {
            mauiText.Should().Contain(symbol);
        }

        mauiText.Should().NotContain("HttpClient");
    }

    [Fact]
    public void MauiTfms_IncludeTheFourShells_AndNotWinArm64()
    {
        // Arrange
        var csprojPath = Path.Combine(RepoRoot, "src", "Recall.Maui", "Recall.Maui.csproj");

        // Act
        var text = File.ReadAllText(csprojPath);

        // Assert
        text.Should().Contain("net10.0-android");
        text.Should().Contain("net10.0-ios");
        text.Should().Contain("net10.0-maccatalyst");
        text.Should().Contain("net10.0-windows10.0.19041.0");
        text.Should().Contain("android-arm64");
        text.Should().Contain("ios-arm64");
        text.Should().Contain("win-x64");
        text.Should().Contain("maccatalyst-arm64");
        text.Should().NotContain("win-arm64");
    }

    [Fact]
    public void MauiCsproj_LinuxHostAbsent_DoesNotReferenceGtkOrLinuxRid()
    {
        // Arrange
        var csprojPath = Path.Combine(RepoRoot, "src", "Recall.Maui", "Recall.Maui.csproj");

        // Act
        var text = File.ReadAllText(csprojPath);

        // Assert
        text.Should().NotContain("linux-x64");
        text.Should().NotContain("linux-arm64");
        text.Should().NotContain("Microsoft.Maui.Platforms.Linux.Gtk4");
        text.Should().NotContain("Platform.Maui.Linux.Gtk4");
        text.Should().NotContain("net10.0-linux");
    }

    [Fact]
    public void Readme_LogoAndLinuxRid_NamesUnbuiltShell()
    {
        // Arrange
        var readmePath = Path.Combine(RepoRoot, "README.md");

        // Act
        var text = File.ReadAllText(readmePath);

        // Assert
        text.Should().Contain("src/Recall.UI/wwwroot/brand/infinity-icon-small-dark.svg");
        text.Should().Contain("alt=\"Recall\"");
        text.Should().Contain("width=\"96\"");
        text.Should().Contain("## What it can do");
        text.Should().Contain("## What v1 will do");
        text.Should().Contain("## The future");
        text.Should().Contain("Not all of this is in the tree yet.");
        text.Should().Contain("A scored recall report is not in this tree yet.");
        text.Should().Contain("v1 has no language model, no chat, and no generated summary.");
        text.Should().Contain("linux-x64");
        text.Should().Contain("The Linux shell is not in the tree.");
        text.Should().Contain("`linux-arm64` and `win-arm64` are not v1 RIDs.");
    }

    [Fact]
    public void CoreCsproj_DoesNotReferenceMauiOnnxOrZVec()
    {
        // Arrange
        var csprojPath = Path.Combine(RepoRoot, "src", "Recall.Core", "Recall.Core.csproj");

        // Act
        var text = File.ReadAllText(csprojPath);

        // Assert
        text.Should().NotContain("UseMaui");
        text.Should().NotContain("OnnxRuntime");
        text.Should().NotContain("ZVec");
        text.Should().NotContain("Http");
    }

    [Fact]
    public void Solution_DoesNotReferenceSpeechOrLanguageModel()
    {
        // Arrange
        var forbidden = new[] { "OnnxRuntimeGenAI", "Whisper", "Vosk" };
        var csprojFiles = Directory.EnumerateFiles(RepoRoot, "*.csproj", SearchOption.AllDirectories).ToList();

        // Act
        var hits = csprojFiles
            .Where(path => forbidden.Any(token => File.ReadAllText(path).Contains(token, StringComparison.Ordinal)))
            .ToList();

        // Assert
        hits.Should().BeEmpty();
    }

    [Fact]
    public void Shell_HasNoHardcodedUserVisibleSentence()
    {
        // Arrange
        var forbidden = new[] { "Hello", "Counter", "Weather", "Click me", "Welcome", "Loading" };
        var srcRoot = Path.Combine(RepoRoot, "src");
        var patterns = new[] { "*.razor", "*.html", "*.xaml" };

        // Act
        var hits = patterns
            .SelectMany(pattern => Directory.EnumerateFiles(srcRoot, pattern, SearchOption.AllDirectories))
            .Where(path => forbidden.Any(word => File.ReadAllText(path).Contains(word, StringComparison.Ordinal)))
            .ToList();

        // Assert
        hits.Should().BeEmpty();
    }

    [Fact]
    public void Manifest_HasNoCapturePermission()
    {
        // Arrange
        var androidManifest = Path.Combine(RepoRoot, "src", "Recall.Maui", "Platforms", "Android", "AndroidManifest.xml");
        var iosPlist = Path.Combine(RepoRoot, "src", "Recall.Maui", "Platforms", "iOS", "Info.plist");
        var catalystPlist = Path.Combine(RepoRoot, "src", "Recall.Maui", "Platforms", "MacCatalyst", "Info.plist");
        var androidForbidden = new[] { "CAMERA", "RECORD_AUDIO", "READ_MEDIA", "READ_EXTERNAL_STORAGE" };
        var plistForbidden = new[]
        {
            "UIBackgroundModes",
            "NSCameraUsageDescription",
            "NSMicrophoneUsageDescription",
            "NSPhotoLibraryUsageDescription",
        };

        // Act
        var androidText = File.ReadAllText(androidManifest);
        var iosText = File.ReadAllText(iosPlist);
        var catalystText = File.ReadAllText(catalystPlist);

        // Assert
        foreach (var token in androidForbidden)
        {
            androidText.Should().NotContain(token);
        }

        foreach (var token in plistForbidden)
        {
            iosText.Should().NotContain(token);
            catalystText.Should().NotContain(token);
        }
    }

    [Fact]
    public void RecallSlnx_WhenRead_ContainsRequiredProjects()
    {
        // Arrange
        var slnx = Path.Combine(RepoRoot, "Recall.slnx");
        var text = File.ReadAllText(slnx);

        // Assert
        text.Should().Contain("Recall.Core.csproj");
        text.Should().Contain("Recall.UI.csproj");
        text.Should().Contain("Recall.Maui.csproj");
        text.Should().Contain("Recall.Benchmarks.csproj");
        text.Should().Contain("Recall.UnitTests.csproj");
        text.Should().Contain("Recall.CorpusTests.csproj");
        text.Should().Contain("Recall.IntegrationTests.csproj");
        text.Should().Contain("mrl-eval.csproj");
    }

    [Fact]
    public void RepoRoot_UsesSlnxOnly_NotLegacySln()
    {
        // Arrange
        var slnx = Path.Combine(RepoRoot, "Recall.slnx");
        var sln = Path.Combine(RepoRoot, "Recall.sln");

        // Assert
        File.Exists(slnx).Should().BeTrue();
        File.Exists(sln).Should().BeFalse();
    }

    [Fact]
    public void RecallUi_WhenScanned_DoesNotInjectThemeLocaleOrScopeServices()
    {
        // Arrange
        var uiRoot = Path.Combine(RepoRoot, "src", "Recall.UI");
        var forbidden = new[]
        {
            "ThemeService",
            "LocaleService",
            "IScopeManagerService",
        };

        // Act
        var hits = Directory
            .EnumerateFiles(uiRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".razor.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains("\\obj\\", StringComparison.Ordinal))
            .Where(path => !path.Contains("\\State\\", StringComparison.Ordinal))
            .Select(path => (path, text: File.ReadAllText(path)))
            .Where(pair => forbidden.Any(token => pair.text.Contains(token, StringComparison.Ordinal)))
            .Select(pair => pair.path)
            .ToList();

        // Assert
        hits.Should().BeEmpty();
    }

    [Fact]
    public void RazorMarkup_WhenScanned_HasNoCodeBlocks()
    {
        // Arrange
        var uiRoot = Path.Combine(RepoRoot, "src", "Recall.UI");
        var razorFiles = Directory
            .EnumerateFiles(uiRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\obj\\", StringComparison.Ordinal))
            .ToList();

        // Act
        var withCode = razorFiles
            .Where(path => File.ReadAllText(path).Contains("@code", StringComparison.Ordinal))
            .ToList();

        // Assert
        withCode.Should().BeEmpty();
    }

    [Fact]
    public void AddRecallCore_CanBeCalledWithoutMaui()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returned = services.AddRecallCore();

        // Assert
        returned.Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(Recall.Core.Theme.ThemeService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(Recall.Core.Locale.LocaleService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IScopeManagerService));
    }

    [Fact]
    public void CrashPackage_IsNotReferenced()
    {
        // Arrange
        var forbidden = new[]
        {
            "Sentry",
            "Sentry.Maui",
            "Sentry.AspNetCore",
            "Microsoft.AppCenter",
            "Microsoft.AppCenter.Crashes",
            "Microsoft.AppCenter.Analytics",
        };
        var projectFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(RepoRoot, "tests"), "*.csproj", SearchOption.AllDirectories))
            .Append(Path.Combine(RepoRoot, "Directory.Packages.props"))
            .ToList();

        // Act
        var hits = new List<string>();
        foreach (var projectFile in projectFiles)
        {
            var document = XDocument.Load(projectFile);
            foreach (var element in document.Descendants())
            {
                var include = element.Attribute("Include")?.Value;
                if (include is null)
                {
                    continue;
                }

                if (forbidden.Any(name => string.Equals(include, name, StringComparison.Ordinal)))
                {
                    hits.Add($"{projectFile}: {include}");
                }
            }
        }

        // Assert
        hits.Should().BeEmpty();
    }

    [Fact]
    public void WinX64Publish_ContainsNoWeight()
    {
        // Arrange
        var publishRoot = Path.Combine(
            RepoRoot,
            "src",
            "Recall.Maui",
            "bin",
            "Release",
            "net10.0-windows10.0.19041.0",
            "win-x64",
            "publish");
        if (!Directory.Exists(publishRoot))
        {
            throw new InvalidOperationException("Windows publish folder is missing.");
        }

        // Act
        var weights = Directory
            .EnumerateFiles(publishRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert
        weights.Should().BeEmpty();
    }

    [Fact]
    public void AndroidApk_ContainsNoWeight()
    {
        // Arrange
        var publishRoot = Path.Combine(
            RepoRoot,
            "src",
            "Recall.Maui",
            "bin",
            "Release",
            "net10.0-android",
            "android-arm64",
            "publish");
        if (!Directory.Exists(publishRoot))
        {
            throw new InvalidOperationException("Android publish folder is missing.");
        }

        var apk = Directory.EnumerateFiles(publishRoot, "*.apk", SearchOption.AllDirectories).FirstOrDefault();
        if (apk is null)
        {
            throw new InvalidOperationException("Android publish folder is missing.");
        }

        // Act
        using var archive = System.IO.Compression.ZipFile.OpenRead(apk);
        var weights = archive.Entries
            .Where(entry => entry.FullName.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)
                || entry.FullName.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.FullName)
            .ToList();

        // Assert
        weights.Should().BeEmpty();
    }

    [Fact]
    public void Libzvec_LoadAlign_Is4096()
    {
        // Arrange
        var publishRoot = Path.Combine(
            RepoRoot,
            "src",
            "Recall.Maui",
            "bin",
            "Release",
            "net10.0-android",
            "android-arm64",
            "publish");
        if (!Directory.Exists(publishRoot))
        {
            throw new InvalidOperationException("Android publish folder is missing.");
        }

        var extracted = false;
        var library = Directory
            .EnumerateFiles(publishRoot, "libzvec_c_api.so", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (library is null)
        {
            library = ExtractPublishedLibzvec(publishRoot);
            extracted = true;
        }

        try
        {
            // Act
            var aligns = ReadElf64PtLoadAlignments(library);

            // Assert
            aligns.Should().Equal(4096, 4096, 4096, 4096);
        }
        finally
        {
            if (extracted)
            {
                File.Delete(library);
            }
        }
    }

    private static List<long> ReadElf64PtLoadAlignments(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        var ident = reader.ReadBytes(16);
        if (ident.Length < 16 || ident[0] != 0x7f || ident[1] != (byte)'E' || ident[2] != (byte)'L' || ident[3] != (byte)'F')
        {
            throw new InvalidOperationException("Android publish folder is missing.");
        }

        if (ident[4] != 2)
        {
            throw new InvalidOperationException("Android publish folder is missing.");
        }

        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt32();
        reader.ReadUInt64();
        var programHeaderOffset = reader.ReadUInt64();
        reader.ReadUInt64();
        reader.ReadUInt32();
        reader.ReadUInt16();
        var programHeaderEntrySize = reader.ReadUInt16();
        var programHeaderCount = reader.ReadUInt16();

        var aligns = new List<long>();
        for (var index = 0; index < programHeaderCount; index++)
        {
            stream.Position = (long)programHeaderOffset + index * programHeaderEntrySize;
            var segmentType = reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt64();
            reader.ReadUInt64();
            reader.ReadUInt64();
            reader.ReadUInt64();
            reader.ReadUInt64();
            var alignment = reader.ReadUInt64();
            if (segmentType == 1)
            {
                aligns.Add((long)alignment);
            }
        }

        return aligns;
    }

    private static string ExtractPublishedLibzvec(string publishRoot)
    {
        var apk = Directory.EnumerateFiles(publishRoot, "*.apk", SearchOption.AllDirectories).FirstOrDefault();
        if (apk is null)
        {
            throw new InvalidOperationException("Android publish folder is missing.");
        }

        using var archive = System.IO.Compression.ZipFile.OpenRead(apk);
        var entry = archive.Entries.FirstOrDefault(item =>
            item.FullName.Replace('\\', '/').EndsWith("lib/arm64-v8a/libzvec_c_api.so", StringComparison.Ordinal));
        if (entry is null)
        {
            throw new InvalidOperationException("Android publish folder is missing.");
        }

        var extracted = Path.Combine(Path.GetTempPath(), "recall-libzvec-" + Guid.NewGuid().ToString("N") + ".so");
        using (var source = entry.Open())
        using (var destination = File.Create(extracted))
        {
            source.CopyTo(destination);
        }

        return extracted;
    }

    [Fact]
    public void HttpClient_IsAbsentFromCoreAndUiSource()
    {
        // Arrange
        var roots = new[]
        {
            Path.Combine(RepoRoot, "src", "Recall.Core"),
            Path.Combine(RepoRoot, "src", "Recall.UI"),
        };

        // Act
        var hits = roots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => File.ReadAllText(path).Contains("HttpClient", StringComparison.Ordinal))
            .ToList();

        // Assert
        hits.Should().BeEmpty();
    }
}
