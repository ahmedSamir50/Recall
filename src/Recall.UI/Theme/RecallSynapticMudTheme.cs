using MudBlazor;

namespace Recall.UI.Theme;

/// <summary>MudBlazor palette aligned with tokens.css; layout chrome uses Tailwind.</summary>
public static class RecallSynapticMudTheme
{
    public static MudTheme Create() => new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#00F2FE",
            Secondary = "#C9A2FF",
            Tertiary = "#67F4B7",
            Background = "#0B0D12",
            Surface = "#111319",
            AppbarBackground = "rgba(12, 14, 19, 0.78)",
            DrawerBackground = "#0C0E13",
            TextPrimary = "#E8E5DF",
            TextSecondary = "#AEB9C4",
            ActionDefault = "#8793A0",
            Divider = "rgba(174, 185, 196, 0.24)",
            LinesDefault = "rgba(174, 185, 196, 0.14)",
            Info = "#00F2FE",
            Success = "#67F4B7",
            Warning = "#FFC46B",
            Error = "#FFB4AB",
        },
        PaletteLight = new PaletteLight
        {
            Primary = "#00737A",
            Secondary = "#6B21A8",
            Tertiary = "#047857",
            Background = "#EEF1F4",
            Surface = "#F4F6F8",
            AppbarBackground = "rgba(255, 255, 255, 0.74)",
            DrawerBackground = "#FFFFFF",
            TextPrimary = "#12151A",
            TextSecondary = "#3E4954",
            ActionDefault = "#5B6672",
            Divider = "rgba(18, 21, 26, 0.18)",
            LinesDefault = "rgba(18, 21, 26, 0.10)",
            Info = "#00C4CE",
            Success = "#047857",
            Warning = "#8A4B00",
            Error = "#B3261E",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
        },
    };
}
