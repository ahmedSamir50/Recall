using Microsoft.Extensions.DependencyInjection;
using Recall.Core.Checkpoints;
using Recall.Core.DependencyInjection;
using Recall.Adapters.DependencyInjection;

namespace Recall.IntegrationTests.Infrastructure;

public static class IntegrationTestServiceCollection
{
    public static ServiceProvider Build(string modelDirectory, string vectorRoot)
    {
        var services = new ServiceCollection();
        services.AddRecallCore();
        services.AddSingleton<Recall.Core.Ports.IScopeGrantStore, InMemoryScopeGrantStore>();
        services.AddSingleton<IIngestCursorStore>(_ => new SqliteIngestCursorStore(Path.Combine(vectorRoot, CheckpointSql.FileName)));
        services.AddRecallAdapters(modelDirectory, vectorRoot);
        services.AddSingleton<Recall.Core.Ports.IEncoderFileHash, TestEncoderFileHash>();
        return services.BuildServiceProvider();
    }
}
