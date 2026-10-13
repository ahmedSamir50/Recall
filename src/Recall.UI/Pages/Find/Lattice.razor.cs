namespace Recall.UI.Pages;

public partial class Lattice
{
    private sealed record LatticeNode(double X, double Y, string ColorClass, int Radius, string? Label);

    private static readonly LatticeNode[] Nodes =
    [
        new(50, 50, "cy", 14, "Colosseum"),
        new(28, 30, "vi", 9, "Voice"),
        new(72, 28, "em", 9, "Parts.pdf"),
        new(22, 70, "cy", 8, "Vespa"),
        new(78, 68, "am", 8, "Video"),
        new(50, 18, "vi", 6, null),
        new(40, 82, "em", 6, null),
        new(62, 82, "cy", 6, null),
        new(12, 46, "em", 5, null),
        new(88, 46, "vi", 5, null),
    ];
}
