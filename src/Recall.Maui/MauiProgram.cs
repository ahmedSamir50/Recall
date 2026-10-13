using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Recall.Core.DependencyInjection;
using Recall.Maui.DependencyInjection;
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
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();
        builder.Services.AddRecallCore();
        builder.Services.AddRecallUi();
        builder.Services.AddRecallMauiPlatform();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
