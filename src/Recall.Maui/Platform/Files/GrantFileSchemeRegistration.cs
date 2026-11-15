namespace Recall.Maui.Platform.Files;

public static partial class GrantFileSchemeRegistration
{
    public static void Register(IServiceProvider services, object? platformWebView) =>
        RegisterCore(services, platformWebView);

    private static partial void RegisterCore(IServiceProvider services, object? platformWebView);
}