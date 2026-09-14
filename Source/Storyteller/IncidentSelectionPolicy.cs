using System;

namespace RimMind.Storyteller
{
    public enum StorytellerIncidentKind
    {
        Other,
        ThreatSmall,
        ThreatBig,
    }

    public static class IncidentSelectionPolicy
    {
        public static float ClampPointsMultiplier(float multiplier)
            => Math.Clamp(multiplier, 0.3f, 2.0f);

        public static bool ShouldNotify(
            bool notificationsEnabled,
            StorytellerIncidentKind kind)
            => notificationsEnabled
               && (kind == StorytellerIncidentKind.ThreatBig
                   || kind == StorytellerIncidentKind.ThreatSmall);
    }
}
