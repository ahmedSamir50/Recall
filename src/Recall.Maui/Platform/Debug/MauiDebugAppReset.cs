using Recall.Core.Device;
using Recall.Core.Ports;
namespace Recall.Maui.Platform.Debug;

public sealed class MauiDebugAppReset : IDebugAppReset
{
    private readonly IModelDownloadService download;
    private readonly IDeviceRecordService device;
    private readonly IOnboardingCompletionStore onboarding;

    public MauiDebugAppReset(
        IModelDownloadService download,
        IDeviceRecordService device,
        IOnboardingCompletionStore onboarding)
    {
        this.download = download;
        this.device = device;
        this.onboarding = onboarding;
    }

    public bool IsEnabled => true;

    public Task ResetAllLocalDataAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        download.CancelTextDownload();
        if (download is MauiModelDownloadService mauiDownload)
        {
            mauiDownload.ClearLocalDownloadState();
        }

        Preferences.Default.Clear();
        SecureStorage.RemoveAll();
        ClearDirectory(FileSystem.AppDataDirectory);
        ClearDirectory(FileSystem.CacheDirectory);
        onboarding.ResetToOnboarding();
        if (device is DeviceRecordService record)
        {
            record.InvalidateCache();
        }

        return Task.CompletedTask;
    }

    private static void ClearDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
    }
}
