using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Recall.Core.DependencyInjection;
using Recall.Maui.DependencyInjection;
using Recall.Core.Catalog;
using Recall.Core.Text;
using Recall.Maui.Platform;
using Recall.Core.Ports;
using Recall.Maui.Platform.Debug;
using Recall.UI.DependencyInjection;

namespace Recall.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            })
#if WINDOWS
            .ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windows => windows.OnWindowCreated(window =>
                {
                    try
                    {
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                        var baseDir = AppContext.BaseDirectory;
                        var candidates = new[]
                        {
                            Path.Combine(baseDir, "appicon.ico"),
                            Path.Combine(baseDir, "Platforms", "Windows", "appicon.ico"),
                            Path.Combine(baseDir, "appicon.scale-400.png"),
                        };
                        var iconPath = candidates.FirstOrDefault(File.Exists);
                        if (iconPath is not null)
                        {
                            appWindow.SetIcon(iconPath);
                        }
                    }
                    catch
                    {
                        // Icon is best-effort; the embedded exe icon still applies.
                    }
                }));
            })
#endif
            ;

        builder.Services.AddMauiBlazorWebView();
        var settings = typeof(MauiProgram).Assembly.GetManifestResourceStream("Recall.Maui.appsettings.json");
        if (settings is null)
        {
            throw new InvalidOperationException("Recall.Maui.appsettings.json is not embedded.");
        }

        builder.Configuration.AddJsonStream(settings);

        var secretsPath = Path.Combine(FileSystem.AppDataDirectory, "appsettings.secrets.json");
        if (File.Exists(secretsPath))
        {
            builder.Configuration.AddJsonFile(secretsPath, optional: true, reloadOnChange: false);
        }

        var textModel = TextModelConfiguration.Read(builder.Configuration);
        ModelCatalog.Use(TextModelConfiguration.ReadCatalog(builder.Configuration));
        GrantTextFiles.Use(TextModelConfiguration.ReadIngest(builder.Configuration));
        builder.Services.AddRecallCore();
        builder.Services.AddRecallUi();
        builder.Services.AddRecallMauiPlatform(textModel);
#if DEBUG
        builder.Services.AddSingleton<IDebugAppReset, MauiDebugAppReset>();
#else
        builder.Services.AddSingleton<IDebugAppReset, DebugAppResetDisabled>();
#endif

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
