using Microsoft.AspNetCore.Components;
using Recall.Core.Age;
using Recall.Core.Scope;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Vocabulary;

namespace Recall.UI.Pages;

public partial class Preferences
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    [Inject]
    private AgeCutoffPreferenceService AgePreferences { get; set; } = null!;

    [Inject]
    private AgeMoveService AgeMove { get; set; } = null!;

    [Inject]
    private IDebugAppReset DebugReset { get; set; } = null!;

    [Inject]
    private UserSkipService SkipService { get; set; } = null!;

    private AgeCutoffYears age;

    private UserSkipAdditions skips = UserSkipAdditions.Empty;

    private string extensionDraft = string.Empty;

    private string folderDraft = string.Empty;

    private string fileDraft = string.Empty;

    private string? extensionNotice;

    private string? folderNotice;

    private string? fileNotice;

    private string extensionFilter = string.Empty;

    private string folderFilter = string.Empty;

    private string fileFilter = string.Empty;

    private bool foldersOpen;

    private bool secretsOpen;

    protected override void OnInitialized()
    {
        age = AgePreferences.Get();
        skips = SkipService.Load();
    }

    private void SetTheme(ThemePreference preference) => Ui.SetThemePreference(preference);

    public static bool IsThemeSelected(ThemePreference current, ThemePreference choice) => current == choice;

    public static string ThemeChoiceClass(ThemePreference current, ThemePreference choice) =>
        IsThemeSelected(current, choice) ? "btn-p" : "btn-g";

    private void SetLocale(UiLocale locale)
    {
        Ui.SetLocale(locale);
        Ui.ApplyCulture();
    }

    private void SetAge(AgeCutoffYears years)
    {
        AgeCutoffActions.Set(AgePreferences, AgeMove, years, DateTimeOffset.UtcNow);
        age = years;
    }

    private void ApplyExtension()
    {
        var status = SkipService.TryAdd(UserSkipKind.Extension, extensionDraft);
        extensionNotice = UserSkipService.MessageKey(status);
        if (status == UserSkipAddStatus.Added)
        {
            extensionDraft = string.Empty;
        }

        skips = SkipService.Load();
    }

    private void ApplyFolder()
    {
        var status = SkipService.TryAdd(UserSkipKind.FolderName, folderDraft);
        folderNotice = UserSkipService.MessageKey(status);
        if (status == UserSkipAddStatus.Added)
        {
            folderDraft = string.Empty;
        }

        skips = SkipService.Load();
    }

    private void ApplyFile()
    {
        var status = SkipService.TryAdd(UserSkipKind.FileName, fileDraft);
        fileNotice = UserSkipService.MessageKey(status);
        if (status == UserSkipAddStatus.Added)
        {
            fileDraft = string.Empty;
        }

        skips = SkipService.Load();
    }

    private void RemoveExtension(string value)
    {
        SkipService.TryRemove(UserSkipKind.Extension, value);
        skips = SkipService.Load();
    }

    private void RemoveFolder(string value)
    {
        SkipService.TryRemove(UserSkipKind.FolderName, value);
        skips = SkipService.Load();
    }

    private void RemoveFile(string value)
    {
        SkipService.TryRemove(UserSkipKind.FileName, value);
        skips = SkipService.Load();
    }

    private static IEnumerable<string> FilteredRows(IEnumerable<string> rows, string filter) =>
        rows.Where(row => SkipListFilter.Matches(row, filter));

    private void ToggleFolders() => foldersOpen = !foldersOpen;

    private void ToggleSecrets() => secretsOpen = !secretsOpen;

    private void GoFolders() => Nav.NavigateTo("/folders");

    private void GoModels() => Nav.NavigateTo("/models");

    private void GoDevice() => Nav.NavigateTo("/device");

    private void GoPin() => Nav.NavigateTo("/pin");

    private async Task ResetAllDebugAsync()
    {
        await DebugReset.ResetAllLocalDataAsync();
        Ui.SetThemePreference(ThemePreference.System);
        Ui.SetLocale(UiLocale.En);
        Ui.ApplyCulture();
        age = AgeCutoffYears.Two;
        Ui.ReloadFromScope();
        Nav.NavigateTo(AppRoutes.OnboardingWelcome, forceLoad: true);
    }
}
