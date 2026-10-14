using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.View;
using Recall.Maui.Platform;

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
        PublishSafeArea(content);
        content.Post(() =>
        {
            publishedSafeTop = -1;
            publishedSafeBottom = -1;
            PublishSafeArea(content);
        });
    }

    private void PublishSafeArea(Android.Views.View content)
    {
        var insets = content.RootWindowInsets;
        if (insets is null || !OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            return;
        }

        var bars = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
        var density = content.Resources?.DisplayMetrics?.Density ?? 1f;
        if (density < 1f)
        {
            density = 1f;
        }

        var top = (int)Math.Ceiling(bars.Top / density);
        var bottom = (int)Math.Ceiling(bars.Bottom / density);
        if (top == publishedSafeTop && bottom == publishedSafeBottom)
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
