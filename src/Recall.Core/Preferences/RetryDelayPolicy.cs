namespace Recall.Core.Preferences;

public static class RetryDelayPolicy
{
    public const int DefaultMinutes = 5;

    public const int MinMinutes = 1;

    public const int MaxMinutes = 60;

    public static bool Allowed(int minutes) => minutes >= MinMinutes && minutes <= MaxMinutes;
}