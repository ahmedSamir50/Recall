using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Recall.Core.Preferences;

namespace Recall.UI.Components;

public partial class ThemeLanguageToggles
{
    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    private string LanguageLabel => Ui.Current.Locale == UiLocale.Ar
        ? T("Language_TargetEnglish")
        : T("Language_TargetArabic");

    private async Task ToggleThemeAsync()
    {
        Ui.CycleTheme();
        await ApplyChromeAsync();
    }

    private async Task ToggleLanguageAsync()
    {
        Ui.ToggleLocale();
        Ui.ApplyCulture();
        await ApplyChromeAsync();
    }

    private async Task ApplyChromeAsync()
    {
        var snap = Ui.Current;
        await Js.InvokeVoidAsync(RecallScripts.ApplyChrome, snap.EffectiveTheme, snap.Lang, snap.Dir);
    }
}
