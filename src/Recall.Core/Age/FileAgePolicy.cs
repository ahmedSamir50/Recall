using Recall.Core.Faults;
using Recall.Core.Vocabulary;

namespace Recall.Core.Age;

public static class FileAgePolicy
{
    public static bool IsPastCutoff(DateTimeOffset? lastWriteUtc, AgeCutoffYears years, DateTimeOffset utcNow)
    {
        if (lastWriteUtc is null)
        {
            return false;
        }

        if (years is not (AgeCutoffYears.Two or AgeCutoffYears.Five or AgeCutoffYears.Seven))
        {
            throw new RecallFaultException(RecallFault.AgeCutoffNotAllowed);
        }

        return lastWriteUtc < utcNow.AddYears(-(int)years);
    }
}
