using Recall.Adapters.DependencyInjection;
using Recall.Core.Checkpoints;
using Recall.Core.Download;
using Recall.Core.Ports;
using Recall.Core.Ingest;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.Maui.Platform;
using Recall.Maui.Platform.Debug;
using Recall.Maui.Platform.Files;

namespace Recall.Maui.DependencyInjection;

public static class MauiServiceCollectionExtensions
{
    public static IServiceCollection AddRecallMauiPlatform(this IServiceCollection services, TextModelDownloadOptions textModel)
    {
        services.AddSingleton(textModel);
        services.AddSingleton<IEncoderFileHash>(_ => new DefaultEncoderFileHash { TextHash = textModel.OnnxSha256 });
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
        services.AddSingleton(new LocalOpLogWriter(Path.Combine(FileSystem.AppDataDirectory, LocalOpLogWriter.DirectoryName)));
        services.AddSingleton<IModelDownloadService, MauiModelDownloadService>();
        services.AddSingleton<ILocalFileOpener, MauiLocalFileOpener>();
        services.AddSingleton<ILocalContentProbe, MauiLocalContentProbe>();
        services.AddSingleton<IIdleDetectorService, MauiIdleDetector>();
        services.AddSingleton<IIngestCursorStore>(_ => new SqliteIngestCursorStore(Path.Combine(FileSystem.AppDataDirectory, CheckpointSql.FileName)));
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

        var modelDir = Path.Combine(FileSystem.AppDataDirectory, textModel.RelativeModelDirectory);
        var vectorRoot = Path.Combine(FileSystem.AppDataDirectory, textModel.RelativeVectorDirectory);
        services.AddRecallAdapters(modelDir, vectorRoot);
        return services;
    }
}
