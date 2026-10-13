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
            OnnxBaseUrl = Value(section, nameof(TextModelDownloadOptions.OnnxBaseUrl), defaults.OnnxBaseUrl),
            RepositoryBaseUrl = Value(section, nameof(TextModelDownloadOptions.RepositoryBaseUrl), defaults.RepositoryBaseUrl),
            OnnxFileName = Value(section, nameof(TextModelDownloadOptions.OnnxFileName), defaults.OnnxFileName),
            OnnxDataFileName = Value(section, nameof(TextModelDownloadOptions.OnnxDataFileName), defaults.OnnxDataFileName),
            TokenizerFileName = Value(section, nameof(TextModelDownloadOptions.TokenizerFileName), defaults.TokenizerFileName),
            OnnxSha256 = Value(section, nameof(TextModelDownloadOptions.OnnxSha256), defaults.OnnxSha256),
            OnnxDataSha256 = Value(section, nameof(TextModelDownloadOptions.OnnxDataSha256), defaults.OnnxDataSha256),
            TokenizerSha256 = Value(section, nameof(TextModelDownloadOptions.TokenizerSha256), defaults.TokenizerSha256),
            UserAgentProduct = Value(section, nameof(TextModelDownloadOptions.UserAgentProduct), defaults.UserAgentProduct),
            UserAgentVersion = Value(section, nameof(TextModelDownloadOptions.UserAgentVersion), defaults.UserAgentVersion),
            RelativeModelDirectory = Value(section, nameof(TextModelDownloadOptions.RelativeModelDirectory), defaults.RelativeModelDirectory),
            RelativeVectorDirectory = Value(section, nameof(TextModelDownloadOptions.RelativeVectorDirectory), defaults.RelativeVectorDirectory),
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
