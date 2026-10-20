using Recall.Core.Vocabulary;

namespace Recall.Core.Text;

public static class WidthOnEdit
{
    public static StoredWidth Choose(Modality modality, StoredWidth current, bool pinned, bool pastCutoff)
    {
        if (modality == Modality.Text)
        {
            return StoredWidth.W512;
        }

        if (current == StoredWidth.W512 && !pinned)
        {
            return StoredWidth.W512;
        }

        if (current == StoredWidth.W512 && pinned)
        {
            return StoredWidth.W768;
        }

        if (current == StoredWidth.W768 && !pastCutoff)
        {
            return StoredWidth.W768;
        }

        return StoredWidth.W512;
    }
}
