using FluentAssertions;
using Recall.Core.Catalog;
using Recall.Core.Vocabulary;

namespace Recall.UnitTests;

public sealed class ModelCatalogTests
{
    [Fact]
    public void All_ContainsTextVisionAndAudio_WithoutLanguageOrSpeechRows()
    {
        // Act
        var modalities = ModelCatalog.All.Select(entry => entry.Modality).ToArray();
        var ids = ModelCatalog.All.Select(entry => entry.ModelId).ToArray();

        // Assert
        modalities.Should().Equal(Modality.Text, Modality.Image, Modality.Audio);
        ids.Should().NotContain(id => id.Contains("whisper", StringComparison.OrdinalIgnoreCase));
        ids.Should().NotContain(id => id.Contains("language", StringComparison.OrdinalIgnoreCase));
        ids.Should().NotContain(id => id.Contains("speech", StringComparison.OrdinalIgnoreCase));
        ids.Should().OnlyContain(id => id.StartsWith("embeddinggemma-2-", StringComparison.Ordinal));
    }
}
