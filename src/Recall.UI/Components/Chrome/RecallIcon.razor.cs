using Microsoft.AspNetCore.Components;

namespace Recall.UI.Components;

public partial class RecallIcon
{
    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    [Parameter]
    public string Css { get; set; } = "w-18 h-18 shrink-0";

    private string Paths => RecallIconPaths.Get(Name);

    private static class RecallIconPaths
    {
        private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["search"] = """<path d="M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14Zm9 3-4.3-4.3"/>""",
            ["lat"] = """<circle cx="5" cy="6" r="2"/><circle cx="19" cy="6" r="2"/><circle cx="12" cy="18" r="2"/><path d="M7 6h10M6 8l5 8m2 0 5-8"/>""",
            ["folder"] = """<path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2Z"/>""",
            ["chip"] = """<rect x="6" y="6" width="12" height="12" rx="2"/><path d="M9 2v4m6-4v4M9 18v4m6-4v4M2 9h4m-4 6h4m12-6h4m-4 6h4"/>""",
            ["gear"] = """<circle cx="12" cy="12" r="3"/><path d="M12 2v3m0 14v3M2 12h3m14 0h3M4.9 4.9 7 7m10 10 2.1 2.1M4.9 19.1 7 17m10-10 2.1-2.1"/>""",
            ["shield"] = """<path d="M12 3 4 6v6c0 4.5 3.4 8.3 8 9 4.6-.7 8-4.5 8-9V6Z"/><path d="m9 12 2 2 4-4"/>""",
            ["check"] = """<path d="m5 12 5 5L20 7"/>""",
            ["mic"] = """<rect x="9" y="3" width="6" height="11" rx="3"/><path d="M5 11a7 7 0 0 0 14 0M12 18v3"/>""",
            ["img"] = """<rect x="3" y="4" width="18" height="16" rx="2"/><circle cx="9" cy="10" r="2"/><path d="m21 16-5-5-9 9"/>""",
            ["doc"] = """<path d="M6 3h8l4 4v14H6Z"/><path d="M14 3v4h4M9 13h6m-6 4h6"/>""",
            ["play"] = """<path d="M8 5v14l11-7Z"/>""",
            ["alert"] = """<path d="M12 9v4m0 4h.01M10.3 3.9 2 18a2 2 0 0 0 1.7 3h16.6A2 2 0 0 0 22 18L13.7 3.9a2 2 0 0 0-3.4 0Z"/>""",
            ["lock"] = """<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/>""",
            ["film"] = """<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M7 4v16M17 4v16M3 9h4m10 0h4M3 15h4m10 0h4"/>""",
            ["sun"] = """<circle cx="12" cy="12" r="4"/><path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.4 1.4M17.6 17.6 19 19M5 19l1.4-1.4M17.6 6.4 19 5"/>""",
            ["moon"] = """<path d="M20 15A8 8 0 1 1 9 4a7 7 0 0 0 11 11Z"/>""",
            ["globe"] = """<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c3 3.5 3 14.5 0 18M12 3c-3 3.5-3 14.5 0 18"/>""",
            ["retry"] = """<path d="M4 12a8 8 0 0 1 14-5.3L20 9M20 4v5h-5M20 12a8 8 0 0 1-14 5.3L4 15m0 5v-5h5"/>""",
        };

        public static string Get(string name) =>
            Map.TryGetValue(name, out var paths) ? paths : string.Empty;
    }
}
