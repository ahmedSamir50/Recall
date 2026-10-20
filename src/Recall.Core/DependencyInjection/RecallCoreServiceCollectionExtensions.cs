using Microsoft.Extensions.DependencyInjection;
using Recall.Core.Device;
using Recall.Core.Download;
using Recall.Core.Ingest;
using Recall.Core.Text;
using Recall.Core.Indexing;
using Recall.Core.Locale;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Theme;

namespace Recall.Core.DependencyInjection;

public static class RecallCoreServiceCollectionExtensions
{
    public static IServiceCollection AddRecallCore(this IServiceCollection services)
    {
        services.AddSingleton<ThemeService>();
        services.AddSingleton<LocaleService>();
        services.AddSingleton<AgeCutoffPreferenceService>();
        services.AddSingleton<IUserSkipStore, MemoryUserSkipStore>();
        services.AddSingleton<UserSkipService>();
        services.AddSingleton<IScopeManagerService, ScopeGrantService>();
        services.AddSingleton<IIndexPolicy, RecallIndexPolicy>();
        services.AddSingleton<LocalOpLogWriter>();
        services.AddSingleton<IRandomDeviceIdSource, GuidRandomDeviceIdSource>();
        services.AddSingleton<IDeviceRecordService, DeviceRecordService>();
        services.AddSingleton<ILocalContentProbe, LocalOnlyContentProbe>();
        services.AddSingleton<IGrantedTextReader, DirectoryGrantedTextReader>();
        services.AddSingleton<IEncoderFileHash, DefaultEncoderFileHash>();
        services.AddSingleton<IRemoteFileSkipReport, RemoteFileSkipReport>();
        services.AddSingleton<IDatasetSkipReport, DatasetSkipReport>();
        services.AddSingleton<IIdleDetectorService, DisabledIdleDetectorService>();
        services.AddSingleton<ITextSearchService, TextSearchService>();
        services.AddSingleton<TextIngestEngine>();
        services.AddSingleton<IGrantedTextIngestService, GrantedTextIngestService>();
        services.AddSingleton<ScopeChangeApplier>();
        services.AddSingleton<ForegroundScanService>();
        services.AddSingleton<IIndexNowService, IndexNowService>();
        services.AddSingleton<ScopeWatchCoordinator>();
        services.AddSingleton<DisabledScopeWatcher>();
        return services;
    }
}
