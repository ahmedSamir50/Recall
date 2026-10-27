namespace Recall.IntegrationTests.Infrastructure;

public static class DemoDatasetPaths
{
    public const string DefaultRoot = @"D:\A_S\Recall_DemoData\recall_demo_dataset";

    public static string Root =>
        Environment.GetEnvironmentVariable("RECALL_DEMO_DATASET") ?? DefaultRoot;

    public static string Combine(params string[] segments) => Path.Combine([Root, .. segments]);

    public static void RequireRoot()
    {
        if (!Directory.Exists(Root))
        {
            throw new InvalidOperationException(
                $"Demo dataset required at {Root}. Set RECALL_DEMO_DATASET if your copy lives elsewhere.");
        }
    }
}
