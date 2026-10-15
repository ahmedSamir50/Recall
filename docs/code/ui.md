## src/Recall.UI/AppRoutes.cs

```csharp
namespace Recall.UI;
public static class AppRoutes
```

No branch.


## src/Recall.UI/RecallComponentBase.cs

```csharp
namespace Recall.UI;
public abstract class RecallComponentBase : UiStoreComponentBase
```

No branch.


## src/Recall.UI/RecallScripts.cs

```csharp
namespace Recall.UI;
public static class RecallScripts
```

No branch.


## src/Recall.UI/Routes.razor.cs

```csharp
namespace Recall.UI;
public partial class Routes
```

No branch.


## src/Recall.UI/UiCulture.cs

```csharp
namespace Recall.UI;
public static class UiCulture
    public static string Lang(CultureInfo culture)
    public static string Dir(CultureInfo culture)
        if (culture.TwoLetterISOLanguageName == Recall.Core.Locale.UiLanguage.Arabic)
        if (Lang(culture) == Recall.Core.Locale.UiLanguage.Arabic)
```


## src/Recall.UI/DependencyInjection/RecallUiServiceCollectionExtensions.cs

```csharp
namespace Recall.UI.DependencyInjection;
public static class RecallUiServiceCollectionExtensions
    public static IServiceCollection AddRecallUi(this IServiceCollection services)
```

No branch.


## src/Recall.UI/Localization/RecallText.cs

```csharp
namespace Recall.UI.Localization;
public static class RecallText
    public static string Get(string key, CultureInfo? culture = null)
    public static IReadOnlyList<string> AllKeys()
        if (set is null)
```


## src/Recall.UI/State/ShellUiStore.cs

```csharp
namespace Recall.UI.State;
public sealed class ShellUiStore : IDisposable
    public ShellUiStore(ThemeService theme, LocaleService locale, IScopeManagerService scope)
    public void Dispose() => subject.Dispose();
    public void SetRoute(string routePath)
    public void SetThemePreference(ThemePreference preference)
    public ThemePreference CycleTheme()
    public void SetLocale(UiLocale value)
    public UiLocale ToggleLocale()
    public void ApplyCulture()
    public ScopeGrantAddResult TryAddGrant(ScopeFolderGrant grant)
    public void RemoveGrant(string path)
    public void SetGrantFileKinds(string path, GrantFileKinds kinds)
    public void ReloadFromScope() => Publish(BuildSnapshot(Current.RoutePath));
        if (string.Equals(Current.RoutePath, normalized, StringComparison.Ordinal))
        if (string.IsNullOrWhiteSpace(routePath))
        if (query >= 0)
        if (hash >= 0)
```


## src/Recall.UI/State/ShellUiSnapshot.cs

```csharp
namespace Recall.UI.State;
public sealed record ShellUiSnapshot(
    public static ShellUiSnapshot CreateInitial(
        ThemePreference themePreference,
        string effectiveTheme,
        UiLocale locale,
        string routePath,
        IReadOnlyList<ScopeFolderGrant> grants,
        bool canContinueFolders)
```

No branch.


## src/Recall.UI/State/UiStoreComponentBase.cs

```csharp
namespace Recall.UI.State;
public abstract class UiStoreComponentBase : ComponentBase, IDisposable
    public void Dispose()
        if (disposed)
        if (disposing)
```


## src/Recall.UI/Layout/AppShell.razor.cs

```csharp
namespace Recall.UI.Layout;
public partial class AppShell
            catch (DeviceIdNotReadyException)
        if (!firstRender)
```


## src/Recall.UI/Layout/LandingLayout.razor.cs

```csharp
namespace Recall.UI.Layout;
public partial class LandingLayout
        if (!firstRender)
```


## src/Recall.UI/Layout/MainLayout.razor.cs

```csharp
namespace Recall.UI.Layout;
public partial class MainLayout
        if (!firstRender)
```


## src/Recall.UI/Layout/OnboardingLayout.razor.cs

```csharp
namespace Recall.UI.Layout;
public partial class OnboardingLayout
        if (!firstRender)
```


## src/Recall.UI/Layout/ShellLayoutBase.cs

```csharp
namespace Recall.UI.Layout;
public abstract class ShellLayoutBase : LayoutComponentBase, IDisposable
    public void Dispose()
        if (disposed)
        if (disposing)
```


## src/Recall.UI/Components/Chrome/InfinityMark.razor.cs

```csharp
namespace Recall.UI.Components;
public partial class InfinityMark
```

No branch.


## src/Recall.UI/Components/Chrome/LatticeBackground.razor.cs

```csharp
namespace Recall.UI.Components;
public partial class LatticeBackground
        if (!firstRender || mounted)
        if (UseClassicShader)
```


## src/Recall.UI/Components/Chrome/RecallIcon.razor.cs

```csharp
namespace Recall.UI.Components;
public partial class RecallIcon
        public static string Get(string name) =>
```

No branch.


## src/Recall.UI/Components/Chrome/ThemeLanguageToggles.razor.cs

```csharp
namespace Recall.UI.Components;
public partial class ThemeLanguageToggles
```

No branch.


## src/Recall.UI/Components/Models/ModelRow.razor.cs

```csharp
namespace Recall.UI.Components;
public partial class ModelRow
    public ModelRow()
        if (firstRender && AutoStart && TextRow && !downloadKickoffScheduled)
            if (phase == TextModelDownloadPhase.Ready)
            if (phase == TextModelDownloadPhase.HashFailed)
            if (phase is TextModelDownloadPhase.Downloading or TextModelDownloadPhase.Verifying)
        if (TextRow && PhaseChanged.HasDelegate)
        if (!TextModelDownloadUiPolicy.ShouldAutoStartDownload(phase))
        catch (OperationCanceledException)
        if (!TextModelDownloadUiPolicy.ShouldOfferRetry(phase))
        catch (OperationCanceledException)
```


## src/Recall.UI/Components/Onboarding/FolderGrantPanel.razor.cs

```csharp
namespace Recall.UI.Components;
public partial class FolderGrantPanel
        catch (Exception)
        if (result.Outcome == FolderPickerOutcome.Denied)
        if (result.Outcome != FolderPickerOutcome.Selected || string.IsNullOrWhiteSpace(result.Path))
        if (add.Status == ScopeGrantAddStatus.OverlapChildOfExistingParent)
        if (grant is null)
        if (next == GrantFileKinds.None)
        if (!string.IsNullOrWhiteSpace(tree))
            if (colon >= 0 && colon < tree.Length - 1)
        if (!path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        if (index < 0)
        if (slash >= 0)
```


## src/Recall.UI/Components/Onboarding/OnboardingHeader.razor.cs

```csharp
namespace Recall.UI.Components;
public partial class OnboardingHeader
```

No branch.


## src/Recall.UI/Components/Shell/BootstrapRedirect.razor.cs

```csharp
namespace Recall.UI.Components;
public sealed partial class BootstrapRedirect
        if (!firstRender || Onboarding.IsComplete())
        if (path.StartsWith("/onboarding", StringComparison.OrdinalIgnoreCase)
```


## src/Recall.UI/Components/Shell/ShellNavLink.razor.cs

```csharp
namespace Recall.UI.Components;
public partial class ShellNavLink
```

No branch.


## src/Recall.UI/Components/Shell/ShellRouteSync.razor.cs

```csharp
namespace Recall.UI.Components;
public sealed partial class ShellRouteSync
        if (disposing)
```


## src/Recall.UI/Components/Shell/ShellTabLink.razor.cs

```csharp
namespace Recall.UI.Components;
public partial class ShellTabLink
```

No branch.


## src/Recall.UI/Pages/Find/Home.razor.cs

```csharp
namespace Recall.UI.Pages;
public partial class Home
        if (!string.Equals(args.Key, "Enter", StringComparison.Ordinal))
        if (!TextFindSession.ShouldQuery(query))
        catch (Exception ex)
        if (card.Unavailable)
        if (result == Recall.Core.Search.LocalFileOpenResult.Failed)
        else if (result == Recall.Core.Search.LocalFileOpenResult.Missing)
```


## src/Recall.UI/Pages/Find/Lattice.razor.cs

```csharp
namespace Recall.UI.Pages;
public partial class Lattice
```

No branch.


## src/Recall.UI/Pages/Find/TextFindSession.cs

```csharp
namespace Recall.UI.Pages;
public static class TextFindSession
public sealed record TextFindCard(string SourcePath, string FileName, string KindKey, string LocalTime, string ReasonKey, bool Unavailable);
public sealed record TextFindView(IReadOnlyList<TextFindCard> Immediate, IReadOnlyList<TextFindCard> Deferred, bool HasHits)
    public static bool ShouldQuery(string? text) => !string.IsNullOrWhiteSpace(text);
    public static string? FaultKey(Exception? exception)
    public static bool MarkUnavailable(LocalFileOpenResult result) => result == LocalFileOpenResult.Missing;
    public static string? OpenFaultKey(LocalFileOpenResult result) =>
    public static TextFindView Build(IReadOnlyList<TextSearchHit> hits, Func<string, bool> fileExists)
        if (exception is null)
        if (hits.Count == 0)
```


## src/Recall.UI/Pages/Library/Device.razor.cs

```csharp
namespace Recall.UI.Pages;
public partial class Device
        catch (DeviceIdNotReadyException ex)
        if (result == DisplayNameSetResult.Empty)
```


## src/Recall.UI/Pages/Library/Models.razor.cs

```csharp
namespace Recall.UI.Pages;
public partial class Models
```

No branch.


## src/Recall.UI/Pages/Library/Preferences.razor.cs

```csharp
namespace Recall.UI.Pages;
public partial class Preferences
    public static bool IsThemeSelected(ThemePreference current, ThemePreference choice) => current == choice;
    public static string ThemeChoiceClass(ThemePreference current, ThemePreference choice) =>
```

No branch.


## src/Recall.UI/Pages/Onboarding/Folders.razor.cs

```csharp
namespace Recall.UI.Pages.Onboarding;
public partial class Folders
```

No branch.


## src/Recall.UI/Pages/Onboarding/Models.razor.cs

```csharp
namespace Recall.UI.Pages.Onboarding;
public partial class Models
        if (!CanContinue)
```


## src/Recall.UI/Pages/Onboarding/Ready.razor.cs

```csharp
namespace Recall.UI.Pages.Onboarding;
public partial class Ready
```

No branch.


## src/Recall.UI/Pages/Onboarding/Welcome.razor.cs

```csharp
namespace Recall.UI.Pages.Onboarding;
public partial class Welcome
```

No branch.


## src/Recall.UI/Pages/Start/Landing.razor.cs

```csharp
namespace Recall.UI.Pages;
public partial class Landing;
```

No branch.


## src/Recall.UI/Pages/Start/ShellPage.razor.cs

```csharp
namespace Recall.UI.Pages;
public partial class ShellPage
```

No branch.


## src/Recall.UI/_Imports.razor

Markup only.

## src/Recall.UI/Routes.razor

Markup only.

## src/Recall.UI/Layout/AppShell.razor

Markup only.

## src/Recall.UI/Layout/LandingLayout.razor

Markup only.

## src/Recall.UI/Layout/MainLayout.razor

Markup only.

## src/Recall.UI/Layout/OnboardingLayout.razor

Markup only.

## src/Recall.UI/Components/Chrome/InfinityMark.razor

Markup only.

## src/Recall.UI/Components/Chrome/LatticeBackground.razor

Markup only.

## src/Recall.UI/Components/Chrome/PrivacyPill.razor

Markup only.

## src/Recall.UI/Components/Chrome/RecallIcon.razor

Markup only.

## src/Recall.UI/Components/Chrome/ThemeLanguageToggles.razor

```razor
<button type="button" class="w-9 h-9 grid place-items-center rounded-lg btn-g ink2" aria-label="@T("Theme_Label")" @onclick="ToggleThemeAsync">
<button type="button" class="h-9 px-3 rounded-lg btn-g ink2 text-13 flex items-center gap-2" aria-label="@T("Language_Label")" @onclick="ToggleLanguageAsync">
```

## src/Recall.UI/Components/Models/ModelRow.razor

```razor
                <button type="button" class="btn btn-p text-13" @onclick="RetryAsync">
```

## src/Recall.UI/Components/Onboarding/FolderGrantPanel.razor

```razor
                    <button type="button" class="btn btn-g shrink-0" aria-label="@T("Folders_Remove")" @onclick="() => Remove(grant.Path)">×</button>
    <button type="button" class="btn btn-g w-full justify-center mb-4 border-dashed" @onclick="AddFolderAsync">@T("Folders_Add")</button>
            <button type="button" class="btn btn-g" @onclick="OnBack">@T("Btn_Back")</button>
            <button type="button" class="btn btn-p" disabled="@(!Ui.Current.CanContinueFolders)" @onclick="OnContinue">@T("Btn_Continue")</button>
```

## src/Recall.UI/Components/Onboarding/OnboardingHeader.razor

Markup only.

## src/Recall.UI/Components/Shell/BootstrapRedirect.razor

Markup only.

## src/Recall.UI/Components/Shell/ShellNavLink.razor

```razor
<button type="button" class="@Css" aria-current="@(Active ? "page" : null)" aria-label="@Recall.UI.Localization.RecallText.Get(LabelKey, Ui.Current.Culture)" @onclick="Go">
```

## src/Recall.UI/Components/Shell/ShellRouteSync.razor

Markup only.

## src/Recall.UI/Components/Shell/ShellTabLink.razor

```razor
<button type="button" class="@Css" @onclick="Go">
```

## src/Recall.UI/Pages/Find/Home.razor

```razor
@page "/"
            <input @bind="query" @bind:event="oninput" aria-label="@T("Search_Placeholder")" placeholder="@T("Search_Placeholder")" @onkeydown="OnKeyDownAsync" class="w-full search-input" />
                            <button type="button" class="btn btn-g" @onclick="() => OpenAsync(card)">@T("Home_OpenFile")</button>
                            <button type="button" class="btn btn-g" @onclick="() => OpenAsync(card)">@T("Home_OpenFile")</button>
```

## src/Recall.UI/Pages/Find/Lattice.razor

```razor
@page "/lattice"
```

## src/Recall.UI/Pages/Library/Device.razor

```razor
@page "/device"
            <button type="button" class="btn btn-p mt-3" @onclick="SaveDisplayName">@T("Device_Save")</button>
```

## src/Recall.UI/Pages/Library/Folders.razor

```razor
@page "/folders"
```

## src/Recall.UI/Pages/Library/Models.razor

```razor
@page "/models"
```

## src/Recall.UI/Pages/Library/Pin.razor

```razor
@page "/pin"
```

## src/Recall.UI/Pages/Library/Preferences.razor

```razor
@page "/preferences"
                <button type="button" class="btn @ThemeChoiceClass(Ui.Current.ThemePreference, ThemePreference.System)" aria-pressed="@IsThemeSelected(Ui.Current.ThemePreference, ThemePreference.System)" @onclick="() => SetTheme(ThemePreference.System)">@T("Theme_System")</button>
                <button type="button" class="btn @ThemeChoiceClass(Ui.Current.ThemePreference, ThemePreference.Dark)" aria-pressed="@IsThemeSelected(Ui.Current.ThemePreference, ThemePreference.Dark)" @onclick="() => SetTheme(ThemePreference.Dark)">@T("Theme_Dark")</button>
                <button type="button" class="btn @ThemeChoiceClass(Ui.Current.ThemePreference, ThemePreference.Light)" aria-pressed="@IsThemeSelected(Ui.Current.ThemePreference, ThemePreference.Light)" @onclick="() => SetTheme(ThemePreference.Light)">@T("Theme_Light")</button>
                <button type="button" class="btn @(Ui.Current.Locale == UiLocale.En ? "btn-p" : "btn-g")" aria-pressed="@(Ui.Current.Locale == UiLocale.En)" @onclick="() => SetLocale(UiLocale.En)">@T("Language_TargetEnglish")</button>
                <button type="button" class="btn @(Ui.Current.Locale == UiLocale.Ar ? "btn-p" : "btn-g")" aria-pressed="@(Ui.Current.Locale == UiLocale.Ar)" @onclick="() => SetLocale(UiLocale.Ar)">@T("Language_TargetArabic")</button>
                <button type="button" class="btn @(age == AgeCutoffYears.Two ? "btn-p" : "btn-g")" @onclick="() => SetAge(AgeCutoffYears.Two)">2</button>
                <button type="button" class="btn @(age == AgeCutoffYears.Five ? "btn-p" : "btn-g")" @onclick="() => SetAge(AgeCutoffYears.Five)">5</button>
                <button type="button" class="btn @(age == AgeCutoffYears.Seven ? "btn-p" : "btn-g")" @onclick="() => SetAge(AgeCutoffYears.Seven)">7</button>
            <button type="button" class="btn btn-g text-start" @onclick="GoDevice">@T("Preferences_DeviceLink")</button>
            <button type="button" class="btn btn-g text-start" @onclick="GoPin">@T("Preferences_PinLink")</button>
            <button type="button" class="btn btn-g text-start" @onclick="GoFolders">@T("Preferences_FoldersLink")</button>
            <button type="button" class="btn btn-g text-start" @onclick="GoModels">@T("Preferences_ModelsLink")</button>
                <button type="button" class="btn btn-g er text-start w-full" @onclick="ResetAllDebugAsync">@T("Debug_ResetAll_Button")</button>
```

## src/Recall.UI/Pages/Onboarding/Folders.razor

```razor
@page "/onboarding/folders"
```

## src/Recall.UI/Pages/Onboarding/Models.razor

```razor
@page "/onboarding/models"
            <button type="button" class="btn btn-g" @onclick="GoBack">@T("Btn_Back")</button>
            <button type="button" class="btn btn-p" disabled="@(!CanContinue)" @onclick="GoNext">@T("Btn_Continue")</button>
```

## src/Recall.UI/Pages/Onboarding/Ready.razor

```razor
@page "/onboarding/ready"
        <button type="button" class="btn btn-p" @onclick="GoHome">@T("Ready_Start")</button>
```

## src/Recall.UI/Pages/Onboarding/Welcome.razor

```razor
@page "/onboarding/welcome"
            <button type="button" class="btn btn-p justify-center" @onclick="GoToFolders">@T("Onboard_ChooseFolders")</button>
```

## src/Recall.UI/Pages/Start/Landing.razor

```razor
@page "/landing"
```

## src/Recall.UI/Pages/Start/ShellPage.razor

```razor
@page "/shell-redirect"
```
