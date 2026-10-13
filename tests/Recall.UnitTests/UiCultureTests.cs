using System.Globalization;
using FluentAssertions;
using Recall.UI;

namespace Recall.UnitTests;

public sealed class UiCultureTests
{
    [Fact]
    public void Lang_Arabic_IsAr()
    {
        UiCulture.Lang(CultureInfo.GetCultureInfo("ar")).Should().Be("ar");
    }

    [Fact]
    public void Lang_Other_IsEn()
    {
        UiCulture.Lang(CultureInfo.GetCultureInfo("fr")).Should().Be("en");
    }

    [Fact]
    public void Dir_Arabic_IsRtl()
    {
        UiCulture.Dir(CultureInfo.GetCultureInfo("ar")).Should().Be("rtl");
    }

    [Fact]
    public void Dir_English_IsLtr()
    {
        UiCulture.Dir(CultureInfo.GetCultureInfo("en")).Should().Be("ltr");
    }
}
