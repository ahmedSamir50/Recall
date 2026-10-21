namespace Recall.Core.Text;

public static class PdfSlicePolicy
{
    public const int TokenCap = 8192;

    public const int PageCap = 10;

    public static bool EndsSlice(int tokens, int pages) => tokens >= TokenCap || pages >= PageCap;
}
