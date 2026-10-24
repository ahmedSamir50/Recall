using Recall.Core.Faults;
using Recall.Core.Preferences;
using Recall.Core.Vocabulary;

namespace Recall.Core.Age;

public static class AgeCutoffActions
{
    public static void Set(AgeCutoffPreferenceService preferences, AgeMoveService ageMove, AgeCutoffYears years, DateTimeOffset utcNow)
    {
        try
        {
            preferences.Set(years);
        }
        catch (RecallFaultException ex) when (ex.Fault == RecallFault.AgeCutoffNotAllowed)
        {
            throw;
        }

        ageMove.Sweep(utcNow);
    }
}
