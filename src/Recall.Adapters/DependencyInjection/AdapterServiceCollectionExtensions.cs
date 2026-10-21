using Microsoft.Extensions.DependencyInjection;
using Recall.Adapters.Embedding;
using Recall.Adapters.Indexing;
using Recall.Core.Checkpoints;
using Recall.Core.Download;
using Recall.Core.Ports;
using Recall.Core.Search;

namespace Recall.Adapters.DependencyInjection;

public static class AdapterServiceCollectionExtensions
{
    public static IServiceCollection AddRecallAdapters(
        this IServiceCollection services,
        string modelDirectory,
        string vectorDatabaseRoot,
        string? visionModelDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(vectorDatabaseRoot);

        var resolvedVisionDirectory = visionModelDirectory ?? modelDirectory;
        services.AddSingleton(sp => new TextModelRuntime(
            modelDirectory,
            sp.GetService<IRecallShellKind>()?.Kind ?? RecallShellKind.Unknown,
            sp.GetService<TextModelDownloadOptions>()));
        services.AddSingleton<ITextEmbeddingEncoder, LlamaTextEmbeddingEncoderLazy>();
        services.AddSingleton<ITextTokenCounter, LazyTextTokenCounter>();
        services.AddSingleton<ITextVectorIndex>(sp => new ZVecTextVectorIndex(
            vectorDatabaseRoot,
            sp.GetRequiredService<IIndexPolicy>(),
            sp.GetRequiredService<IIngestCursorStore>()));
        services.AddSingleton<IVisionEmbeddingEncoder>(sp => new VisionModelRuntime(
            resolvedVisionDirectory,
            sp.GetRequiredService<TextModelRuntime>(),
            sp.GetService<VisionModelDownloadOptions>()));
        services.AddSingleton<IMediaVectorIndex>(_ => new ZVecMediaVectorIndex(vectorDatabaseRoot));
        services.AddSingleton<IPictureSearchService, PictureSearchService>();
        return services;
    }
}
