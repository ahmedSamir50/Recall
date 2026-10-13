using Recall.Core.Ports;
using Recall.Maui.Platform;

namespace Recall.Maui.DependencyInjection;

public static class MauiServiceCollectionExtensions
{
    public static IServiceCollection AddRecallMauiPlatform(this IServiceCollection services)
    {
        services.AddSingleton<IThemePreferenceStore, MauiThemePreferenceStore>();
        services.AddSingleton<IUiLocaleStore, MauiUiLocaleStore>();
        services.AddSingleton<IScopeGrantStore, MauiScopeGrantStore>();
        services.AddSingleton<ISystemThemeProvider, MauiSystemThemeProvider>();
        services.AddSingleton<IFolderPickerService, MauiFolderPickerService>();
        services.AddSingleton<IDeviceRecordService, MauiDeviceRecordService>();
        return services;
    }
}
