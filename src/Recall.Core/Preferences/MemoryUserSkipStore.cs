using Recall.Core.Ports;
using Recall.Core.Preferences;

namespace Recall.Core.Preferences;

public sealed class MemoryUserSkipStore : IUserSkipStore
{
    private UserSkipAdditions additions = UserSkipAdditions.Empty;

    public UserSkipAdditions Load() => additions;

    public void Save(UserSkipAdditions additions) => this.additions = additions;
}
