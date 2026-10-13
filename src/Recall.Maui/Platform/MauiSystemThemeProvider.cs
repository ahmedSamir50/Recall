using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed class MauiSystemThemeProvider : ISystemThemeProvider
{
    public bool IsDark()
    {
        return Application.Current?.RequestedTheme == AppTheme.Dark;
    }
}
