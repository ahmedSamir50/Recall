using Microsoft.Extensions.DependencyInjection;
using Recall.Adapters.Embedding;
using Recall.Adapters.Indexing;
using Recall.Core.Download;
using Recall.Core.Ports;

namespace Recall.Adapters.DependencyInjection;

public static class AdapterServiceCollectionExtensions
{
    public static IServiceCollection AddRecallAdapters(
        this IServiceCollection services,
        string modelDirectory,
        string vectorDatabaseRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(vectorDatabaseRoot);

        services.AddSingleton(sp => new TextModelRuntime(
            modelDirectory,
            sp.GetService<IRecallShellKind>()?.Kind ?? RecallShellKind.Unknown,
            sp.GetService<TextModelDownloadOptions>()));
        services.AddSingleton<ITextEmbeddingEncoder, LazyOnnxTextEmbeddingEncoder>();
        services.AddSingleton<ITextTokenCounter, LazyTextTokenCounter>();
        services.AddSingleton<ITextVectorIndex>(sp => new ZVecTextVectorIndex(
            vectorDatabaseRoot,
            sp.GetRequiredService<IIndexPolicy>()));
        return services;
    }
}
