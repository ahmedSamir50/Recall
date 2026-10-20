namespace Recall.Core.Text;

public static class EncoderRepair
{
    public static bool NeedsRepair(string? storedHash, string? currentHash) =>
        !string.IsNullOrEmpty(storedHash)
        && !string.IsNullOrEmpty(currentHash)
        && !string.Equals(storedHash, currentHash, StringComparison.OrdinalIgnoreCase);
}
