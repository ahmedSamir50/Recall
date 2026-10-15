## src/Recall.Maui/MauiProgram.cs

```csharp
namespace Recall.Maui;
public static class MauiProgram
    public static MauiApp CreateMauiApp()
                        if (iconPath is not null)
        if (settings is null)
            throw new InvalidOperationException("Recall.Maui.appsettings.json is not embedded.");
        if (File.Exists(secretsPath))
```


## src/Recall.Maui/App.xaml.cs

```csharp
namespace Recall.Maui;
public partial class App : Application
	public App()
```

No branch.


## src/Recall.Maui/MainPage.xaml.cs

```csharp
namespace Recall.Maui;
public partial class MainPage : ContentPage
	public MainPage()
		if (Handler is null)
```


## src/Recall.Maui/DependencyInjection/MauiServiceCollectionExtensions.cs

```csharp
namespace Recall.Maui.DependencyInjection;
public static class MauiServiceCollectionExtensions
    public static IServiceCollection AddRecallMauiPlatform(this IServiceCollection services, TextModelDownloadOptions textModel)
```

No branch.


## src/Recall.Maui/Platform/Preferences/MauiAgeCutoffPreferenceStore.cs

```csharp
namespace Recall.Maui.Platform;
public sealed class MauiAgeCutoffPreferenceStore : IAgeCutoffPreferenceStore
    public AgeCutoffYears Get()
    public void Set(AgeCutoffYears years) => Preferences.Default.Set(Key, (int)years);
```

No branch.


## src/Recall.Maui/Platform/Preferences/MauiDeviceRecordStore.cs

```csharp
namespace Recall.Maui.Platform;
public sealed class MauiDeviceRecordStore : IDeviceRecordStore
    public DeviceRecordState? Load()
    public void Save(DeviceRecordState state)
        if (string.IsNullOrWhiteSpace(json))
```


## src/Recall.Maui/Platform/Preferences/MauiOnboardingCompletionStore.cs

```csharp
namespace Recall.Maui.Platform;
public sealed class MauiOnboardingCompletionStore : IOnboardingCompletionStore
    public bool IsComplete() => Preferences.Default.Get(Key, false);
    public void MarkComplete() => Preferences.Default.Set(Key, true);
    public void ResetToOnboarding() => Preferences.Default.Set(Key, false);
```

No branch.


## src/Recall.Maui/Platform/Preferences/MauiRecallShellKind.cs

```csharp
namespace Recall.Maui.Platform;
public sealed class MauiRecallShellKind : IRecallShellKind
```

No branch.


## src/Recall.Maui/Platform/Preferences/MauiScopeGrantStore.cs

```csharp
namespace Recall.Maui.Platform;
public sealed class MauiScopeGrantStore : IScopeGrantStore
    public IReadOnlyList<ScopeFolderGrant> List()
    public void Save(IReadOnlyList<ScopeFolderGrant> grants)
        if (string.IsNullOrWhiteSpace(json))
```


## src/Recall.Maui/Platform/Preferences/MauiSystemThemeProvider.cs

```csharp
namespace Recall.Maui.Platform;
public sealed class MauiSystemThemeProvider : ISystemThemeProvider
    public bool IsDark()
```

No branch.


## src/Recall.Maui/Platform/Preferences/MauiThemePreferenceStore.cs

```csharp
namespace Recall.Maui.Platform;
public sealed class MauiThemePreferenceStore : IThemePreferenceStore
    public ThemePreference Get()
    public void Set(ThemePreference preference)
```

No branch.


## src/Recall.Maui/Platform/Preferences/MauiUiLocaleStore.cs

```csharp
namespace Recall.Maui.Platform;
public sealed class MauiUiLocaleStore : IUiLocaleStore
    public UiLocale Get()
    public void Set(UiLocale locale)
```

No branch.


## src/Recall.Maui/Platform/Download/MauiModelDownloadService.cs

```csharp
namespace Recall.Maui.Platform;
public sealed class MauiModelDownloadService : IModelDownloadService
    public MauiModelDownloadService(
        IHttpClientFactory httpClientFactory,
        IGrantedTextIngestService ingest,
        TextModelDownloadOptions options)
    public TextModelDownloadSnapshot GetTextDownloadSnapshot()
    public Task DownloadTextModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    public void CancelTextDownload()
    public void ClearLocalDownloadState()
    public Task RetryTextDownloadAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
        if (Directory.Exists(modelDirectory))
        catch (OperationCanceledException)
        catch (RecallFaultException fault) when (fault.Fault == RecallFault.DownloadHashMismatch)
        catch (RecallFaultException fault) when (fault.Fault == RecallFault.DownloadIncomplete)
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        if (await ModelHashVerifier.MatchesExpectedHashAsync(targetPath, expectedSha256, cancellationToken).ConfigureAwait(false))
        if (!allowRange)
        if (client.DefaultRequestHeaders.UserAgent.Count == 0)
        if (existing > 0)
        if (status == DownloadTransfer.StatusRangeNotSatisfiable)
            if (await ModelHashVerifier.MatchesExpectedHashAsync(partialPath, expectedSha256, cancellationToken).ConfigureAwait(false))
            if (allowRange)
            throw new RecallFaultException(RecallFault.DownloadIncomplete);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException();
            if (append)
            if (!DownloadTransfer.ReceivedFullBody(totalRead, expectedTotal))
                throw new RecallFaultException(RecallFault.DownloadIncomplete);
        if (!await ModelHashVerifier.MatchesExpectedHashAsync(partialPath, expectedSha256, cancellationToken).ConfigureAwait(false))
            throw new RecallFaultException(RecallFault.DownloadHashMismatch);
        if (!TextModelDownloadUiPolicy.ShouldPublishProgress(lastPublishedPercent, percent))
        if (File.Exists(targetPath))
        if (File.Exists(path))
```


## src/Recall.Maui/Platform/Download/TextModelConfiguration.cs

```csharp
namespace Recall.Maui.Platform;
public static class TextModelConfiguration
    public static TextModelDownloadOptions Read(IConfiguration configuration)
    public static ModelCatalogOptions ReadCatalog(IConfiguration configuration)
    public static TextIngestOptions ReadIngest(IConfiguration configuration)
```

No branch.


## src/Recall.Maui/Platform/Files/MauiLocalFileOpener.cs

```csharp
namespace Recall.Maui.Platform.Files;
public sealed partial class MauiLocalFileOpener : ILocalFileOpener
    public bool Exists(string sourcePath)
    public async Task<LocalFileOpenResult> OpenAsync(string sourcePath, CancellationToken cancellationToken = default)
        if (probe == LocalFileProbe.Empty)
        if (probe == LocalFileProbe.ContentUri)
            if (exists)
```


## src/Recall.Maui/Platform/Files/MauiLocalFileOpener.Android.cs

```csharp
namespace Recall.Maui.Platform.Files;
public sealed partial class MauiLocalFileOpener
        if (activity is null || resolver is null)
            if (uri is null)
        if (activity is null)
            if (uri is null)
```


## src/Recall.Maui/Platform/Files/MauiLocalFileOpener.Other.cs

```csharp
namespace Recall.Maui.Platform.Files;
public sealed partial class MauiLocalFileOpener
```

No branch.


## src/Recall.Maui/Platform/Debug/MauiDebugAppReset.cs

```csharp
namespace Recall.Maui.Platform.Debug;
public sealed class MauiDebugAppReset : IDebugAppReset
    public MauiDebugAppReset(
        IModelDownloadService download,
        IDeviceRecordService device,
        IOnboardingCompletionStore onboarding)
    public Task ResetAllLocalDataAsync(CancellationToken cancellationToken = default)
        if (download is MauiModelDownloadService mauiDownload)
        if (device is DeviceRecordService record)
        if (!Directory.Exists(path))
```


## src/Recall.Maui/Platform/Debug/DebugAppResetDisabled.cs

```csharp
namespace Recall.Maui.Platform.Debug;
public sealed class DebugAppResetDisabled : IDebugAppReset
    public Task ResetAllLocalDataAsync(CancellationToken cancellationToken = default) =>
```

No branch.


## src/Recall.Maui/Platforms/Android/MainActivity.cs

```csharp
namespace Recall.Maui;
public partial class MainActivity : MauiAppCompatActivity
        public SafeAreaRetry(MainActivity activity)
        public void Run()
        public SystemBarPadding(MainActivity activity)
        public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View? v, WindowInsetsCompat? insets)
        public void OnActivityResult(Java.Lang.Object? result)
        if (savedInstanceState is null)
        if (Window is null)
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        if (content is null)
        if (safeAreaAttempts <= 0)
        if (topPx <= 0 && bottomPx <= 0)
        if (density < 1f)
        if (safeAreaAttempts <= 0 && top == publishedSafeTop && bottom == publishedSafeBottom)
        if (web is null)
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            if (insets is not null)
                if (bars.Top > 0)
        if (root is Android.Webkit.WebView web)
        if (root is not ViewGroup group)
            if (found is not null)
            if (content is not null)
            if (v is not null)
```


## src/Recall.Maui/Platforms/Android/MainActivity.FolderPicker.cs

```csharp
namespace Recall.Maui;
public partial class MainActivity
        if (requestCode == AndroidFolderPickerCoordinator.OpenDocumentTreeRequestCode)
```


## src/Recall.Maui/Platforms/Android/MainApplication.cs

```csharp
namespace Recall.Maui;
public class MainApplication : MauiApplication
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
```

No branch.


## src/Recall.Maui/Platforms/iOS/AppDelegate.cs

```csharp
namespace Recall.Maui;
public class AppDelegate : MauiUIApplicationDelegate
```

No branch.


## src/Recall.Maui/Platforms/iOS/Program.cs

```csharp
namespace Recall.Maui;
public static class Program
```

No branch.


## src/Recall.Maui/Platforms/MacCatalyst/AppDelegate.cs

```csharp
namespace Recall.Maui;
public class AppDelegate : MauiUIApplicationDelegate
```

No branch.


## src/Recall.Maui/Platforms/MacCatalyst/Program.cs

```csharp
namespace Recall.Maui;
public static class Program
```

No branch.


## src/Recall.Maui/Platforms/Windows/App.xaml.cs

```csharp
namespace Recall.Maui.WinUI;
public partial class App : MauiWinUIApplication
	public App()
```

No branch.


## src/Recall.Maui/Platform/Preferences/MauiUserSkipStore.cs

```csharp
namespace Recall.Maui.Platform;
public sealed class MauiUserSkipStore : IUserSkipStore
```

No branch.

