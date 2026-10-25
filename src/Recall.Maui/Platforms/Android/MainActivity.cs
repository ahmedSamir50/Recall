using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.View;
using Recall.Maui.Platform;
using Recall.Maui.Platform.Bench;

namespace Recall.Maui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public partial class MainActivity : MauiAppCompatActivity
{
    private const string SavedStateRegistryKey = "androidx.lifecycle.BundlableSavedStateRegistry.key";
    private const string SupportFragmentsKey = "android:support:fragments";

    private int publishedSafeTop = -1;
    private int publishedSafeBottom = -1;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        DropRestoredNavigationFragment(savedInstanceState);
        base.OnCreate(savedInstanceState);
        var benchFiles = FilesDir?.AbsolutePath ?? string.Empty;
        new Thread(() => AndroidTextBenchSession.RunIfReady(benchFiles)) { IsBackground = true }.Start();
        new Thread(() => AndroidAudioDecodeSession.RunIfReady(benchFiles)) { IsBackground = true }.Start();
        AndroidFolderPickerCoordinator.BindLauncher(
            RegisterForActivityResult(new ActivityResultContracts.OpenDocumentTree(), new TreeResultCallback()));
    }

    private static void DropRestoredNavigationFragment(Bundle? savedInstanceState)
    {
        if (savedInstanceState is null)
        {
            return;
        }

        savedInstanceState.Remove(SupportFragmentsKey);
        savedInstanceState.Remove("android:fragments");
        savedInstanceState.GetBundle(SavedStateRegistryKey)?.Remove(SupportFragmentsKey);
    }

    protected override void OnResume()
    {
        base.OnResume();
        publishedSafeTop = -1;
        publishedSafeBottom = -1;
        KeepContentBelowSystemBars();
    }

    private void KeepContentBelowSystemBars()
    {
        if (Window is null)
        {
            return;
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            WindowCompat.SetDecorFitsSystemWindows(Window, true);
            return;
        }

        var content = FindViewById(Android.Resource.Id.Content);
        if (content is null)
        {
            return;
        }

        ViewCompat.SetOnApplyWindowInsetsListener(content, new SystemBarPadding(this));
        ViewCompat.RequestApplyInsets(content);
        publishedSafeTop = -1;
        publishedSafeBottom = -1;
        safeAreaAttempts = 8;
        TryPublishSafeArea(content);
    }

    private int safeAreaAttempts;

    private void TryPublishSafeArea(Android.Views.View content)
    {
        PublishSafeArea(content);
        if (safeAreaAttempts <= 0)
        {
            return;
        }

        safeAreaAttempts--;
        content.PostDelayed(new SafeAreaRetry(this), 400);
    }

    private void PublishSafeArea(Android.Views.View content)
    {
        var (topPx, bottomPx) = SystemBarPixels(content);
        if (topPx <= 0 && bottomPx <= 0)
        {
            return;
        }

        var density = content.Resources?.DisplayMetrics?.Density ?? 1f;
        if (density < 1f)
        {
            density = 1f;
        }

        var top = (int)Math.Ceiling(topPx / density);
        var bottom = (int)Math.Ceiling(bottomPx / density);
        if (safeAreaAttempts <= 0 && top == publishedSafeTop && bottom == publishedSafeBottom)
        {
            return;
        }

        var web = FindWebView(content);
        if (web is null)
        {
            return;
        }

        publishedSafeTop = top;
        publishedSafeBottom = bottom;
        web.EvaluateJavascript(
            $"document.documentElement.style.setProperty('--safe-top','{top}px');document.documentElement.style.setProperty('--safe-bottom','{bottom}px');",
            null);
    }

    private (int Top, int Bottom) SystemBarPixels(Android.Views.View content)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var insets = content.RootWindowInsets ?? Window?.DecorView?.RootWindowInsets;
            if (insets is not null)
            {
                var bars = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
                if (bars.Top > 0)
                {
                    return (bars.Top, bars.Bottom);
                }
            }
        }

        var id = content.Resources?.GetIdentifier("status_bar_height", "dimen", "android") ?? 0;
        var top = id > 0 ? content.Resources!.GetDimensionPixelSize(id) : 0;
        return (top, 0);
    }

    private static Android.Webkit.WebView? FindWebView(Android.Views.View? root)
    {
        if (root is Android.Webkit.WebView web)
        {
            return web;
        }

        if (root is not ViewGroup group)
        {
            return null;
        }

        for (var i = 0; i < group.ChildCount; i++)
        {
            var found = FindWebView(group.GetChildAt(i));
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private sealed class SafeAreaRetry : Java.Lang.Object, Java.Lang.IRunnable
    {
        private readonly MainActivity activity;

        public SafeAreaRetry(MainActivity activity)
        {
            this.activity = activity;
        }

        public void Run()
        {
            var content = activity.FindViewById(Android.Resource.Id.Content);
            if (content is not null)
            {
                activity.TryPublishSafeArea(content);
            }
        }
    }

    private sealed class SystemBarPadding : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        private readonly MainActivity activity;

        public SystemBarPadding(MainActivity activity)
        {
            this.activity = activity;
        }

        public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View? v, WindowInsetsCompat? insets)
        {
            if (v is not null)
            {
                activity.PublishSafeArea(v);
            }

            return insets ?? new WindowInsetsCompat.Builder().Build()!;
        }
    }

    private sealed class TreeResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object? result)
        {
            AndroidFolderPickerCoordinator.CompleteUri(result as Android.Net.Uri);
        }
    }
}
