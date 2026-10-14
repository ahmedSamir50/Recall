using Microsoft.AspNetCore.Components;
using Recall.Core.Preferences;
using Recall.Core.Vocabulary;

namespace Recall.UI.Pages;

public partial class Preferences
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    [Inject]
    private AgeCutoffPreferenceService AgePreferences { get; set; } = null!;

    private AgeCutoffYears age;

    protected override void OnInitialized() => age = AgePreferences.Get();

    private void SetTheme(ThemePreference preference) => Ui.SetThemePreference(preference);

    private void SetLocale(UiLocale locale)
    {
        Ui.SetLocale(locale);
        Ui.ApplyCulture();
    }

    private void SetAge(AgeCutoffYears years)
    {
        AgePreferences.Set(years);
        age = years;
    }

    private void GoFolders() => Nav.NavigateTo("/folders");

    private void GoModels() => Nav.NavigateTo("/models");

    private void GoDevice() => Nav.NavigateTo("/device");

    private void GoPin() => Nav.NavigateTo("/pin");
}
