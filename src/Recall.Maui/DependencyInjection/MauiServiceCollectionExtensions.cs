using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Recall.Adapters.DependencyInjection;
using Recall.Core.Age;
using Recall.Core.Checkpoints;
using Recall.Core.Download;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.Maui.Platform;
using Recall.Maui.Platform.Files;
using Recall.Core.Ocr;
using Recall.Maui.Platform.Media;
using Recall.Maui.Platform.Ocr;
using Recall.Maui.Platform.Resources;
using Recall.Core.Resources;

namespace Recall.Maui.DependencyInjection;

public static class MauiServiceCollectionExtensions
{
    [SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "The OCR factory runs on the ingest Task.Run loop, not the UI thread.")]
    public static IServiceCollection AddRecallMauiPlatform(
        this IServiceCollection services,
        TextModelDownloadOptions textModel,
        VisionModelDownloadOptions? visionModel = null,
        AudioModelDownloadOptions? audioModel = null)
    {
        services.AddSingleton(textModel);
        services.AddSingleton(visionModel ?? new VisionModelDownloadOptions());
        services.AddSingleton(audioModel ?? new AudioModelDownloadOptions());
        services.AddSingleton<IEncoderFileHash>(_ => new DefaultEncoderFileHash
        {
            TextHash = textModel.ModelSha256 + ChunkerStamp.Suffix,
            VisionHash = (visionModel ?? new VisionModelDownloadOptions()).ModelSha256,
            AudioHash = (audioModel ?? new AudioModelDownloadOptions()).ModelSha256,
        });
        services.AddHttpClient();
        services.AddSingleton<IThemePreferenceStore, MauiThemePreferenceStore>();
        services.AddSingleton<IUiLocaleStore, MauiUiLocaleStore>();
        services.AddSingleton<IScopeGrantStore, MauiScopeGrantStore>();
        services.AddSingleton<ISystemThemeProvider, MauiSystemThemeProvider>();
        services.AddSingleton<IFolderPickerService, MauiFolderPickerService>();
        services.AddSingleton<IDeviceRecordStore, MauiDeviceRecordStore>();
        services.AddSingleton<IDeviceOsNameProvider, MauiDeviceOsNameProvider>();
        services.AddSingleton<IDeviceIdCandidateProvider, MauiDeviceIdCandidateProvider>();
        services.AddSingleton<IAgeCutoffPreferenceStore, MauiAgeCutoffPreferenceStore>();
        services.AddSingleton<IUserSkipStore, MauiUserSkipStore>();
        services.AddSingleton<IOnboardingCompletionStore, MauiOnboardingCompletionStore>();
        services.AddSingleton<IRecallShellKind, MauiRecallShellKind>();
        services.AddSingleton<IGrantedFolderAccessService, MauiGrantedFolderAccessService>();
        services.AddSingleton<IGrantedTextReader, MauiGrantedTextReader>();
        services.AddSingleton<ISiblingFiles, MauiSiblingFiles>();
#if WINDOWS
        services.AddSingleton<IAudioSource, WindowsAudioSource>();
#elif ANDROID
        services.AddSingleton<IAudioSource, AndroidAudioSource>();
#elif IOS || MACCATALYST
        services.AddSingleton<IAudioSource, AppleAudioSource>();
#endif
        services.Replace(ServiceDescriptor.Singleton<IAudioFileIngest>(sp => new AudioFileIngest(
            sp.GetRequiredService<IAudioSource>(),
            sp.GetRequiredService<ISiblingFiles>(),
            sp.GetRequiredService<IAudioEmbeddingEncoder>(),
            sp.GetRequiredService<IMediaVectorIndex>(),
            sp.GetRequiredService<IAudioOffReport>(),
            sp.GetRequiredService<IEncoderFileHash>(),
            sp.GetRequiredService<IGrantedTextReader>(),
            sp.GetRequiredService<IIngestCursorStore>(),
            sp.GetService<AgeMoveService>())));
        services.AddSingleton(new LocalOpLogWriter(Path.Combine(FileSystem.AppDataDirectory, LocalOpLogWriter.DirectoryName)));
        services.AddSingleton<IModelDownloadService, MauiModelDownloadService>();
        services.AddSingleton<ILocalFileOpener, MauiLocalFileOpener>();
        services.AddSingleton<IMediaPreview, MauiMediaPreview>();
        services.AddSingleton<ILocalContentProbe, MauiLocalContentProbe>();
        services.AddSingleton<IIdleDetectorService, MauiIdleDetector>();
        services.AddSingleton<IIngestCursorStore>(_ => new SqliteIngestCursorStore(Path.Combine(FileSystem.AppDataDirectory, CheckpointSql.FileName)));
        services.Replace(ServiceDescriptor.Singleton<IResourceMeter, ProcessResourceMeter>());
        services.AddSingleton<WindowsFileScopeWatcher>();
        services.AddSingleton<IFileScopeWatcherService>(sp =>
        {
            var kind = ScopeWatcherKindPolicy.Choose(sp.GetRequiredService<IRecallShellKind>().Kind);
            if (kind == ScopeWatcherKind.WindowsFileSystem)
            {
                return sp.GetRequiredService<WindowsFileScopeWatcher>();
            }

            return sp.GetRequiredService<DisabledScopeWatcher>();
        });

#if WINDOWS
        services.AddSingleton<IVideoFrameSamplerService, WindowsVideoFrameSampler>();
#elif ANDROID
        services.AddSingleton<IVideoFrameSamplerService, AndroidVideoFrameSampler>();
#elif IOS || MACCATALYST
        services.AddSingleton<IVideoFrameSamplerService, AppleVideoFrameSampler>();
#endif

        var modelDir = Path.Combine(FileSystem.AppDataDirectory, textModel.RelativeModelDirectory);
        var vectorRoot = Path.Combine(FileSystem.AppDataDirectory, textModel.RelativeVectorDirectory);
        var visionDir = Path.Combine(FileSystem.AppDataDirectory, (visionModel ?? new VisionModelDownloadOptions()).RelativeModelDirectory);
        services.AddRecallAdapters(modelDir, vectorRoot, visionDir);
        var ocrModelRoot = FileSystem.AppDataDirectory;
        services.Replace(ServiceDescriptor.Singleton(sp => new IngestSchedulerService(
            sp.GetRequiredService<IngestScheduler>(),
            sp.GetRequiredService<IngestSchedulerRunner>(),
            sp.GetRequiredService<ScheduledIngestDispatcher>(),
            sp.GetService<IIngestModelSession>(),
            sp.GetService<AgeMoveService>(),
            sp.GetService<PinService>(),
            paths =>
            {
                if (!OcrModelFiles.IsComplete(ocrModelRoot))
                {
                    sp.GetRequiredService<IModelDownloadService>()
                        .DownloadOcrModelsAsync(null, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }

                return new OnnxOcrEngine(
                    ocrModelRoot,
                    sp.GetRequiredService<IIngestCursorStore>(),
                    sp.GetRequiredService<IMediaVectorIndex>(),
                    sp.GetRequiredService<TextIngestEngine>());
            })));
        return services;
    }
}
