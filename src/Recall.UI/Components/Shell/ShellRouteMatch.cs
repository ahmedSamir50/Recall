namespace Recall.UI.Components;

public static class ShellRouteMatch
{
    public static bool IsActive(string? route, string href)
    {
        if (string.Equals(route, href, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(href, "/preferences", StringComparison.Ordinal)
            && route is "/device" or "/pin";
    }
}
