using Recall.Core.Faults;
using Recall.Core.Ports;
using Recall.Core.Vocabulary;

namespace Recall.Core.Preferences;

public sealed class AgeCutoffPreferenceService
{
    private readonly IAgeCutoffPreferenceStore store;

    public AgeCutoffPreferenceService(IAgeCutoffPreferenceStore store)
    {
        this.store = store;
    }

    public AgeCutoffYears Get() => store.Get();

    public void Set(AgeCutoffYears years)
    {
        if (years is not (AgeCutoffYears.Two or AgeCutoffYears.Five or AgeCutoffYears.Seven))
        {
            throw new RecallFaultException(RecallFault.AgeCutoffNotAllowed);
        }

        store.Set(years);
    }
}
