using Microsoft.AspNetCore.Components;
using Recall.Core.Preferences;
using Recall.Core.Theme;

namespace Recall.UI.Pages;

public partial class Preferences
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private void SetTheme(ThemePreference preference) => Ui.SetThemePreference(preference);

    private void GoFolders() => Nav.NavigateTo("/folders");

    private void GoModels() => Nav.NavigateTo("/models");
}
