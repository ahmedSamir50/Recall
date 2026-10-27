using FluentAssertions;
using Recall.Core.Ocr;

namespace Recall.UnitTests;

public sealed class OcrPackLanguageTests
{
    [Fact]
    public void OcrPack_HasNoChineseOrHindi()
    {
        foreach (var file in OcrModelFiles.Pack)
        {
            file.LocalName.Should().NotContain("zh");
            file.LocalName.ToLowerInvariant().Should().NotContain("chinese");
            file.LocalName.Should().NotContain("hi");
            file.LocalName.ToLowerInvariant().Should().NotContain("hindi");
        }
    }
}
