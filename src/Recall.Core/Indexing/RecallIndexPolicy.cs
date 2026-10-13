using Recall.Core.Faults;
using Recall.Core.Ports;
using Recall.Core.Vocabulary;

namespace Recall.Core.Indexing;

public sealed class RecallIndexPolicy : IIndexPolicy
{
    public const int BlockedWidth = 128;

    public const int TextHnswM = 16;

    public const int TextHnswEfConstruction = 200;

    public const int TextHnswEfSearch = 64;

    public StoredWidth TextStoredWidth => StoredWidth.W512;

    public void EnsureStorableWidth(Modality modality, int width)
    {
        if (width == BlockedWidth)
        {
            throw new RecallFaultException(RecallFault.StoredWidthBlocked);
        }

        if (modality == Modality.Text && width != (int)StoredWidth.W512)
        {
            throw new RecallFaultException(RecallFault.TextWidthNot512);
        }
    }
}
