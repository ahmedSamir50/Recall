#if WINDOWS
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml.Controls;
using Recall.Core.Ports;
using Recall.Core.Scope;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Recall.Maui.Platform.Files;

public static partial class GrantFileSchemeRegistration
{
    private static bool registered;

    private static partial void RegisterCore(IServiceProvider services, object? platformWebView)
    {
        if (platformWebView is not BlazorWebView blazorWebView)
        {
            return;
        }

        blazorWebView.HandlerChanged += (_, _) =>
        {
            if (registered || blazorWebView.Handler?.PlatformView is not WebView2 webView2)
            {
                return;
            }

            registered = true;
            _ = HookAsync(services, webView2);
        };
    }

    private static async Task HookAsync(IServiceProvider services, WebView2 webView2)
    {
        await webView2.EnsureCoreWebView2Async();
        if (webView2.CoreWebView2 is null)
        {
            return;
        }

        var core = webView2.CoreWebView2;
#pragma warning disable S1075
        core.AddWebResourceRequestedFilter("recall-file://*", CoreWebView2WebResourceContext.All);
#pragma warning restore S1075
        core.WebResourceRequested += (_, e) =>
        {
            if (!e.Request.Uri.StartsWith(GrantFileUrlPolicy.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var scope = services.GetService(typeof(IScopeManagerService)) as IScopeManagerService;
            var grants = scope?.ListGrants() ?? [];
            var grantUrls = services.GetService(typeof(IGrantFileUrl)) as IGrantFileUrl;
            if (grantUrls is null || !Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var uri))
            {
                return;
            }

            if (!grantUrls.TryResolveUri(uri, grants, out var path) || path is null)
            {
                e.Response = core.Environment.CreateWebResourceResponse(null, 403, "Forbidden", string.Empty);
                return;
            }

            try
            {
                var stream = File.OpenRead(path);
                e.Response = core.Environment.CreateWebResourceResponse(
                    stream.AsRandomAccessStream(),
                    200,
                    "OK",
                    $"Content-Type: {ContentTypeForPath(path)}");
            }
            catch (IOException)
            {
                e.Response = core.Environment.CreateWebResourceResponse(null, 404, "Not Found", string.Empty);
            }
        };
    }

    private static string ContentTypeForPath(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            ".mkv" => "video/x-matroska",
            ".avi" => "video/x-msvideo",
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".wav" => "audio/wav",
            ".flac" => "audio/flac",
            ".ogg" => "audio/ogg",
            ".aac" => "audio/aac",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream",
        };
}
#endif