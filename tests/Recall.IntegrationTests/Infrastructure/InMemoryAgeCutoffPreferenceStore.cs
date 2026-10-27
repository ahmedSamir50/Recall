using Recall.Core.Ports;
using Recall.Core.Vocabulary;

namespace Recall.IntegrationTests.Infrastructure;

internal sealed class InMemoryAgeCutoffPreferenceStore : IAgeCutoffPreferenceStore
{
    private AgeCutoffYears years = AgeCutoffYears.Two;

    public AgeCutoffYears Get() => years;

    public void Set(AgeCutoffYears years) => this.years = years;
}
