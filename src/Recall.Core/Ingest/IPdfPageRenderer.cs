namespace Recall.Core.Ingest;

public interface IPdfPageRenderer
{
    byte[] Render(string path, int pageNumber);
}
