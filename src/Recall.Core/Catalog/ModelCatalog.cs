using Recall.Core.Vocabulary;

namespace Recall.Core.Catalog;

public static class ModelCatalog
{
    public static IReadOnlyList<ModelCatalogEntry> All { get; } =
    [
        new ModelCatalogEntry(Modality.Text, "embeddinggemma-2-text"),
        new ModelCatalogEntry(Modality.Image, "embeddinggemma-2-vision"),
        new ModelCatalogEntry(Modality.Audio, "embeddinggemma-2-audio"),
    ];
}

public sealed record ModelCatalogEntry(Modality Modality, string ModelId);
