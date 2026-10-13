namespace Recall.UI.Pages;

public partial class Home
{
    protected static IReadOnlyList<int> WaveHeights { get; } = Enumerable
        .Range(0, 48)
        .Select(i => 8 + (int)(Math.Abs(Math.Sin(i * 0.7) * Math.Cos(i * 0.23)) * 30))
        .ToArray();

    private IReadOnlyList<string> ContextChips =>
    [
        T("Home_Ctx1"),
        T("Home_Ctx2"),
        T("Home_Ctx3"),
        T("Home_Ctx4"),
    ];
}
