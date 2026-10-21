using Microsoft.Extensions.Configuration;
using Recall.Core.Catalog;
using Recall.Core.Download;
using Recall.Core.Text;

namespace Recall.Maui.Platform;

public static class TextModelConfiguration
{
    public static TextModelDownloadOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(TextModelDownloadOptions.SectionName);
        var defaults = new TextModelDownloadOptions();
        return new TextModelDownloadOptions
        {
            ModelBaseUrl = Value(section, nameof(TextModelDownloadOptions.ModelBaseUrl), defaults.ModelBaseUrl),
            ModelFileName = Value(section, nameof(TextModelDownloadOptions.ModelFileName), defaults.ModelFileName),
            ModelSha256 = Value(section, nameof(TextModelDownloadOptions.ModelSha256), defaults.ModelSha256),
            UserAgentProduct = Value(section, nameof(TextModelDownloadOptions.UserAgentProduct), defaults.UserAgentProduct),
            UserAgentVersion = Value(section, nameof(TextModelDownloadOptions.UserAgentVersion), defaults.UserAgentVersion),
            RelativeModelDirectory = Value(section, nameof(TextModelDownloadOptions.RelativeModelDirectory), defaults.RelativeModelDirectory),
            RelativeVectorDirectory = Value(section, nameof(TextModelDownloadOptions.RelativeVectorDirectory), defaults.RelativeVectorDirectory),
        };
    }

    public static VisionModelDownloadOptions ReadVision(IConfiguration configuration)
    {
        var section = configuration.GetSection(VisionModelDownloadOptions.SectionName);
        var defaults = new VisionModelDownloadOptions();
        return new VisionModelDownloadOptions
        {
            ModelBaseUrl = Value(section, nameof(VisionModelDownloadOptions.ModelBaseUrl), defaults.ModelBaseUrl),
            ModelFileName = Value(section, nameof(VisionModelDownloadOptions.ModelFileName), defaults.ModelFileName),
            ModelSha256 = Value(section, nameof(VisionModelDownloadOptions.ModelSha256), defaults.ModelSha256),
            RelativeModelDirectory = Value(section, nameof(VisionModelDownloadOptions.RelativeModelDirectory), defaults.RelativeModelDirectory),
        };
    }

    public static ModelCatalogOptions ReadCatalog(IConfiguration configuration)
    {
        var section = configuration.GetSection(ModelCatalogOptions.SectionName);
        var defaults = new ModelCatalogOptions();
        return new ModelCatalogOptions
        {
            TextModelId = Value(section, nameof(ModelCatalogOptions.TextModelId), defaults.TextModelId),
            VisionModelId = Value(section, nameof(ModelCatalogOptions.VisionModelId), defaults.VisionModelId),
            AudioModelId = Value(section, nameof(ModelCatalogOptions.AudioModelId), defaults.AudioModelId),
        };
    }

    public static TextIngestOptions ReadIngest(IConfiguration configuration)
    {
        var section = configuration.GetSection(TextIngestOptions.SectionName);
        var defaults = new TextIngestOptions();
        return new TextIngestOptions
        {
            Extensions = Value(section, nameof(TextIngestOptions.Extensions), defaults.Extensions),
        };
    }

    private static string Value(IConfiguration section, string key, string fallback)
    {
        var configured = section[key];
        return string.IsNullOrWhiteSpace(configured) ? fallback : configured;
    }
}
