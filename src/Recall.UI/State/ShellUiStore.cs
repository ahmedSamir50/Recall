using Recall.Core.Locale;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Theme;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Recall.UI.State;

/// <summary>Single reactive UI snapshot for theme, locale, route, and folder grants.</summary>
public sealed class ShellUiStore : IDisposable
{
    private readonly BehaviorSubject<ShellUiSnapshot> subject;
    private readonly ThemeService theme;
    private readonly LocaleService locale;
    private readonly IScopeManagerService scope;

    public ShellUiStore(ThemeService theme, LocaleService locale, IScopeManagerService scope)
    {
        this.theme = theme;
        this.locale = locale;
        this.scope = scope;
        subject = new BehaviorSubject<ShellUiSnapshot>(BuildSnapshot("/"));
    }

    public IObservable<ShellUiSnapshot> State => subject.AsObservable();

    public ShellUiSnapshot Current => subject.Value;

    public void Dispose() => subject.Dispose();

    public void SetRoute(string routePath)
    {
        var normalized = NormalizeRoute(routePath);
        if (string.Equals(Current.RoutePath, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Publish(BuildSnapshot(normalized));
    }

    public void SetThemePreference(ThemePreference preference)
    {
        theme.SetPreference(preference);
        Publish(BuildSnapshot(Current.RoutePath));
    }

    public ThemePreference CycleTheme()
    {
        theme.CyclePreference();
        Publish(BuildSnapshot(Current.RoutePath));
        return Current.ThemePreference;
    }

    public void SetLocale(UiLocale value)
    {
        locale.SetLocale(value);
        Publish(BuildSnapshot(Current.RoutePath));
    }

    public UiLocale ToggleLocale()
    {
        locale.ToggleLocale();
        Publish(BuildSnapshot(Current.RoutePath));
        return Current.Locale;
    }

    public void ApplyCulture()
    {
        locale.ApplyCulture(Current.Locale);
    }

    public ScopeGrantAddResult TryAddGrant(ScopeFolderGrant grant)
    {
        var result = scope.TryAddGrant(grant);
        Publish(BuildSnapshot(Current.RoutePath));
        return result;
    }

    public void RemoveGrant(string path)
    {
        scope.RemoveGrant(path);
        Publish(BuildSnapshot(Current.RoutePath));
    }

    public void SetGrantFileKinds(string path, GrantFileKinds kinds)
    {
        scope.SetGrantFileKinds(path, kinds);
        Publish(BuildSnapshot(Current.RoutePath));
    }

    public void ReloadFromScope() => Publish(BuildSnapshot(Current.RoutePath));

    private ShellUiSnapshot BuildSnapshot(string routePath)
    {
        var grants = scope.ListGrants();
        return ShellUiSnapshot.CreateInitial(
            theme.GetPreference(),
            theme.GetEffectiveTheme(),
            locale.GetLocale(),
            routePath,
            grants,
            scope.CanContinue());
    }

    private void Publish(ShellUiSnapshot snapshot) => subject.OnNext(snapshot);

    private static string NormalizeRoute(string routePath)
    {
        if (string.IsNullOrWhiteSpace(routePath))
        {
            return "/";
        }

        var cut = routePath.AsSpan();
        var query = cut.IndexOf('?');
        if (query >= 0)
        {
            cut = cut[..query];
        }

        var hash = cut.IndexOf('#');
        if (hash >= 0)
        {
            cut = cut[..hash];
        }

        var path = cut.Length == 0 ? "/" : cut.ToString();
        return path.Length == 0 ? "/" : path;
    }
}
