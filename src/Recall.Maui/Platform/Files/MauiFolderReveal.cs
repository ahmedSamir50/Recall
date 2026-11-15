using Recall.Core.Ports;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiFolderReveal : IFolderReveal
{
    public bool Reveal(string sourcePath) => TryReveal(sourcePath);

    private partial bool TryReveal(string sourcePath);
}