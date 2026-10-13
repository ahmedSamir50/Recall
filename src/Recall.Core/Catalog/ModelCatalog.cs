using Recall.Core.Vocabulary;

namespace Recall.Core.Catalog;

public sealed class ModelCatalogOptions
{
    public const string SectionName = "ModelCatalog";

    public string TextModelId { get; set; } = "embeddinggemma-2-text";

    public string VisionModelId { get; set; } = "embeddinggemma-2-vision";

    public string AudioModelId { get; set; } = "embeddinggemma-2-audio";

    public IReadOnlyList<ModelCatalogEntry> Entries =>
    [
        new ModelCatalogEntry(Modality.Text, TextModelId),
        new ModelCatalogEntry(Modality.Image, VisionModelId),
        new ModelCatalogEntry(Modality.Audio, AudioModelId),
    ];
}

public static class ModelCatalog
{
    private static IReadOnlyList<ModelCatalogEntry> entries = new ModelCatalogOptions().Entries;

    public static IReadOnlyList<ModelCatalogEntry> All => entries;

    public static void Use(ModelCatalogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        entries = options.Entries;
    }
}

public sealed record ModelCatalogEntry(Modality Modality, string ModelId);
