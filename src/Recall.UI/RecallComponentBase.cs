using Recall.UI.Localization;
using Recall.UI.State;

namespace Recall.UI;

public abstract class RecallComponentBase : UiStoreComponentBase
{
    protected string T(string key) => RecallText.Get(key, Ui.Current.Culture);
}
