using Microsoft.Extensions.DependencyInjection;
using Recall.Adapters.DependencyInjection;
using Recall.Core.Checkpoints;
using Recall.Core.DependencyInjection;
using Recall.Core.Ports;
using Recall.Maui.Platform;

namespace Recall.IntegrationTests.Infrastructure;

public static class IntegrationTestServiceCollection
{
    public static ServiceProvider Build(string modelDirectory, string vectorRoot, string? visionModelDirectory = null)
    {
        var services = new ServiceCollection();
        services.AddRecallCore();
        services.AddSingleton<IScopeGrantStore, InMemoryScopeGrantStore>();
        services.AddSingleton<IIngestCursorStore>(_ => new SqliteIngestCursorStore(Path.Combine(vectorRoot, CheckpointSql.FileName)));
        services.AddRecallAdapters(modelDirectory, vectorRoot, visionModelDirectory);
        services.AddSingleton<IVideoFrameSamplerService, WindowsVideoFrameSampler>();
        services.AddSingleton<IAudioSource, WindowsAudioSource>();
        services.AddSingleton<IAgeCutoffPreferenceStore, InMemoryAgeCutoffPreferenceStore>();
        services.AddSingleton<IEncoderFileHash, TestEncoderFileHash>();
        return services.BuildServiceProvider();
    }
}
