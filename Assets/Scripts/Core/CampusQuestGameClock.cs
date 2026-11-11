using UnityEngine;

/// <summary>Unscaled gameplay time excluding explicit pauses. UI menus keep real unscaled time.</summary>
public static class CampusQuestGameClock
{
    static float pausedSeconds, pauseStarted;
    static bool paused;
    public static float Now => Time.unscaledTime - pausedSeconds - (paused ? Time.unscaledTime - pauseStarted : 0);
    public static float Delta => paused ? 0 : Time.unscaledDeltaTime;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { pausedSeconds = pauseStarted = 0; paused = false; }
    public static void SetPaused(bool value)
    {
        if (value == paused) return;
        if (value) pauseStarted = Time.unscaledTime;
        else pausedSeconds += Time.unscaledTime - pauseStarted;
        paused = value;
    }
}
