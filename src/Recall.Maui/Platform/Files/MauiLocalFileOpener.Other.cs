#if !ANDROID
namespace Recall.Maui.Platform.Files;

public sealed partial class MauiLocalFileOpener
{
    private partial bool ContentExists(string sourcePath) => false;

    private partial bool TryOpenContent(string sourcePath) => false;
}
#endif
