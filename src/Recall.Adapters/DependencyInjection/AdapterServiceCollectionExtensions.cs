using Microsoft.Extensions.DependencyInjection;
using Recall.Adapters.Embedding;
using Recall.Adapters.Indexing;
using Recall.Core.Age;
using Recall.Core.Checkpoints;
using Recall.Core.Download;
using Recall.Core.Ingest;
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
        services.AddSingleton(sp => new VisionModelRuntime(
            resolvedVisionDirectory,
            sp.GetRequiredService<TextModelRuntime>(),
            sp.GetService<VisionModelDownloadOptions>()));
        services.AddSingleton<IVisionEmbeddingEncoder>(sp => sp.GetRequiredService<VisionModelRuntime>());
        services.AddSingleton(sp => new AudioModelRuntime(
            resolvedVisionDirectory,
            sp.GetRequiredService<TextModelRuntime>(),
            sp.GetService<AudioModelDownloadOptions>()));
        services.AddSingleton<IAudioEmbeddingEncoder>(sp => sp.GetRequiredService<AudioModelRuntime>());
        services.AddSingleton<ITextEmbeddingEncoder, LlamaTextEmbeddingEncoderLazy>();
        services.AddSingleton<ITextTokenCounter, LazyTextTokenCounter>();
        services.AddSingleton<ITextVectorIndex>(sp => new ZVecTextVectorIndex(
            vectorDatabaseRoot,
            sp.GetRequiredService<IIndexPolicy>(),
            sp.GetRequiredService<IIngestCursorStore>()));
        services.AddSingleton<IIngestModelSession>(sp =>
        {
            var textOptions = sp.GetService<TextModelDownloadOptions>() ?? new TextModelDownloadOptions();
            var visionOptions = sp.GetService<VisionModelDownloadOptions>() ?? new VisionModelDownloadOptions();
            return new LlamaIngestModelSession(
                sp.GetRequiredService<TextModelRuntime>(),
                sp.GetRequiredService<VisionModelRuntime>(),
                sp.GetRequiredService<AudioModelRuntime>(),
                Path.Combine(modelDirectory, textOptions.ModelFileName),
                Path.Combine(resolvedVisionDirectory, visionOptions.ModelFileName));
        });
        services.AddSingleton<IVisionFileIngest>(sp => new VisionFileIngest(
            sp.GetRequiredService<IVisionEmbeddingEncoder>(),
            sp.GetRequiredService<IMediaVectorIndex>(),
            sp.GetRequiredService<IEncoderFileHash>(),
            sp.GetRequiredService<IIngestCursorStore>(),
            sp.GetService<IVideoFrameSamplerService>(),
            sp.GetService<AgeMoveService>()));
        services.AddSingleton<IMediaVectorIndex>(_ => new ZVecMediaVectorIndex(vectorDatabaseRoot));
        services.AddSingleton<IPictureSearchService, PictureSearchService>();
        services.AddSingleton<IAudioSearchService, AudioSearchService>();
        return services;
    }
}
