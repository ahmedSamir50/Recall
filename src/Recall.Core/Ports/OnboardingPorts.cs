namespace Recall.Core.Ports;

public interface IOnboardingCompletionStore
{
    bool IsComplete();

    void MarkComplete();

    void ResetToOnboarding();
}
