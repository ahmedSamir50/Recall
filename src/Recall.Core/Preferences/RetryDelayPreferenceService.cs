using Recall.Core.Faults;
using Recall.Core.Ports;

namespace Recall.Core.Preferences;

public sealed class RetryDelayPreferenceService
{
    private readonly IRetryDelayPreferenceStore store;

    public RetryDelayPreferenceService(IRetryDelayPreferenceStore store)
    {
        this.store = store;
    }

    public int Get()
    {
        var minutes = store.Get();
        return RetryDelayPolicy.Allowed(minutes) ? minutes : RetryDelayPolicy.DefaultMinutes;
    }

    public void Set(int minutes)
    {
        if (!RetryDelayPolicy.Allowed(minutes))
        {
            throw new RecallFaultException(RecallFault.RetryDelayNotAllowed);
        }

        store.Set(minutes);
    }
}