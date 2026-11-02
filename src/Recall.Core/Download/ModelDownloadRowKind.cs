using Recall.Core.Vocabulary;

namespace Recall.Core.Download;

public enum ModelDownloadRowKind
{
    Text = 0,
    Vision = 1,
    Audio = 2,
    Helpers = 3,
}

public static class ModelDownloadRowKindExtensions
{
    public static ModelDownloadRowKind FromModality(Modality modality) =>
        modality switch
        {
            Modality.Text => ModelDownloadRowKind.Text,
            Modality.Image => ModelDownloadRowKind.Vision,
            Modality.Audio => ModelDownloadRowKind.Audio,
            _ => throw new ArgumentOutOfRangeException(nameof(modality), modality, null),
        };
}
