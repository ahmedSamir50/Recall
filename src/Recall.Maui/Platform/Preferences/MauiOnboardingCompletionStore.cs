using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed class MauiOnboardingCompletionStore : IOnboardingCompletionStore
{
    private const string Key = "recall.onboarding.complete";

    public bool IsComplete() => Preferences.Default.Get(Key, false);

    public void MarkComplete() => Preferences.Default.Set(Key, true);

    public void ResetToOnboarding() => Preferences.Default.Set(Key, false);
}
