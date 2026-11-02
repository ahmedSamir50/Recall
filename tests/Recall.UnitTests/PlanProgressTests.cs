using FluentAssertions;
using Recall.Core.Text;

namespace Recall.UnitTests;

public sealed class PlanProgressTests
{
    private static int[] Identity(string document) => Enumerable.Range(0, document.Length + 1).ToArray();

    private static PatchPlan Plan(string document) =>
        DocumentPatchPlanner.Plan("Notes", document, null, 0, Identity(document));

    private static PatchPlan PlanWithin(string document, TimeSpan budget)
    {
#pragma warning disable VSTHRD002
        var task = Task.Run(() => Plan(document));
        task.Wait(budget).Should().BeTrue("the planner must terminate instead of looping");
        return task.Result;
#pragma warning restore VSTHRD002
    }

    private static string Control() =>
        "Heading here. " + string.Join('\n', Enumerable.Range(0, 400).Select(i => $"| c1 | c2 | c3 | row {i} has no sentence mark |"));

    private static string Prose() =>
        string.Join(". ", Enumerable.Range(0, 60).Select(i => "This is sentence " + i + " with a few words in it"));

    private static string Arabic() =>
        "مقدمة. " + string.Join(". ", Enumerable.Range(0, 8).Select(i => "جملة رقم " + i + " " + new string('س', 2400))) + ".";

    [Fact]
    public void Plan_AdvancesWhenNoSentenceBoundaryExists()
    {
        var plan = PlanWithin("Intro. " + new string('X', 5000), TimeSpan.FromSeconds(5));

        plan.Patches.Should().ContainSingle();
        plan.Patches[0].CharStart.Should().Be(0);
        plan.Patches[0].CharEnd.Should().Be(5007);
        plan.Truncated.Should().BeFalse();
    }

    [Fact]
    public void Plan_HardCapCutHasNoBackwardOverlap()
    {
        var plan = PlanWithin("Intro. " + new string('X', 20000), TimeSpan.FromSeconds(5));

        plan.Patches.Should().HaveCount(3);
        plan.Patches[0].CharEnd.Should().Be(7649);
        plan.Patches[1].CharStart.Should().Be(7649);
        plan.Patches[2].CharEnd.Should().Be(20007);
    }

    [Fact]
    public void Plan_HardCapStaysInsideTheContextBudget()
    {
        var plan = PlanWithin("Intro. " + new string('X', 20000), TimeSpan.FromSeconds(5));

        foreach (var patch in plan.Patches)
        {
            var tokens = patch.CharEnd - patch.CharStart;
            tokens.Should().BeLessThan(DocumentPatchPlanner.HardCapTokens);
            tokens.Should().BeLessThanOrEqualTo(DocumentPatchPlanner.HardCapTokens - DocumentPatchPlanner.ContextGuardTokens + 1);
        }
    }

    [Fact]
    public void Plan_LastPatchEndsAtDocumentLength()
    {
        var document = Control();
        var plan = Plan(document);

        plan.Patches[^1].CharEnd.Should().Be(document.Length);
        plan.Truncated.Should().BeFalse();
    }

    [Fact]
    public void Plan_StopsAtMaxPatchesPerDocument()
    {
        var document = string.Join(". ", Enumerable.Range(0, 600).Select(i => "Sentence " + i + " " + new string('z', 2200))) + ".";

        var plan = Plan(document);

        plan.Patches.Should().HaveCount(DocumentPatchPlanner.MaxPatchesPerDocument);
        DocumentPatchPlanner.MaxPatchesPerDocument.Should().Be(512);
        plan.Truncated.Should().BeTrue();
    }

    [Fact]
    public void Plan_EmptyTokenEnds_ReturnsOnePatch()
    {
        var plan = DocumentPatchPlanner.Plan("Notes", "Hello world.", null, 0, []);

        plan.Patches.Should().ContainSingle();
        plan.Patches[0].CharEnd.Should().Be(12);
    }

    [Fact]
    public void Plan_BoundariesAreStableForTerminatingDocuments()
    {
        var control = Plan(Control());
        control.Patches.Select(p => (p.CharStart, p.CharEnd)).Should().Equal(
            (0, 2025), (2025, 4046), (4046, 6048), (6048, 8064), (8064, 10080),
            (10080, 12096), (12096, 14112), (14112, 16128), (16128, 18144), (18144, 19103));

        var prose = Plan(Prose());
        prose.Patches.Should().HaveCount(2);

        var arabicText = Arabic();
        var arabic = Plan(arabicText);
        arabic.Patches.Should().HaveCount(8);
        arabic.Patches[^1].CharEnd.Should().Be(arabicText.Length);
    }

    [Fact]
    public void Plan_ProseFirstOverlapIs263()
    {
        var prose = Plan(Prose());

        var overlap = prose.Patches[0].CharEnd - prose.Patches[1].CharStart;
        overlap.Should().Be(263);
    }
}
