using System;
using System.Collections.Generic;

namespace RimMind.Storyteller
{
    public enum StorytellerIncidentKind
    {
        Other,
        ThreatSmall,
        ThreatBig,
    }

    public enum FallbackCategorySlot
    {
        ThreatBig,
        ThreatSmall,
        Misc,
        FactionArrival,
    }

    public sealed class FallbackProfile
    {
        public FallbackProfile(
            float mtbDays,
            float threatBigWeight,
            float threatSmallWeight,
            float miscWeight,
            float factionArrivalWeight)
        {
            MtbDays = mtbDays;
            ThreatBigWeight = threatBigWeight;
            ThreatSmallWeight = threatSmallWeight;
            MiscWeight = miscWeight;
            FactionArrivalWeight = factionArrivalWeight;
        }

        public float MtbDays { get; }
        public float ThreatBigWeight { get; }
        public float ThreatSmallWeight { get; }
        public float MiscWeight { get; }
        public float FactionArrivalWeight { get; }

        public FallbackCategorySlot ChooseCategorySlot(float sample01)
        {
            float clamped = sample01 < 0f ? 0f : (sample01 > 1f ? 1f : sample01);
            float total = ThreatBigWeight + ThreatSmallWeight + MiscWeight + FactionArrivalWeight;
            if (total <= 0f)
                return FallbackCategorySlot.ThreatBig;

            float threshold = clamped * total;
            float cumulative = ThreatBigWeight;
            if (ThreatBigWeight > 0f && threshold < cumulative)
                return FallbackCategorySlot.ThreatBig;

            cumulative += ThreatSmallWeight;
            if (ThreatSmallWeight > 0f && threshold < cumulative)
                return FallbackCategorySlot.ThreatSmall;

            cumulative += MiscWeight;
            if (MiscWeight > 0f && threshold < cumulative)
                return FallbackCategorySlot.Misc;

            if (FactionArrivalWeight > 0f)
                return FallbackCategorySlot.FactionArrival;

            return FallbackCategorySlot.ThreatBig;
        }
    }

    public static class IncidentSelectionPolicy
    {
        public const string ThreatBigCategoryDefName = "ThreatBig";
        public const string ThreatSmallCategoryDefName = "ThreatSmall";
        public const string MiscCategoryDefName = "Misc";
        public const string FactionArrivalCategoryDefName = "FactionArrival";
        public const string RaidFriendlyDefName = "RaidFriendly";

        private static readonly Dictionary<string, float> CategoryTensionDeltas =
            new Dictionary<string, float>(StringComparer.Ordinal)
            {
                [ThreatBigCategoryDefName] = 0.25f,
                [ThreatSmallCategoryDefName] = 0.12f,
                [MiscCategoryDefName] = -0.05f,
                [FactionArrivalCategoryDefName] = -0.08f,
            };

        private static readonly Dictionary<string, FallbackProfile> FallbackProfilesByModeName =
            new Dictionary<string, FallbackProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["Cassandra"] = new FallbackProfile(4.6f, threatBigWeight: 1.0f, threatSmallWeight: 0f, miscWeight: 0f, factionArrivalWeight: 0f),
                ["Randy"] = new FallbackProfile(1.35f, threatBigWeight: 0.3f, threatSmallWeight: 0.3f, miscWeight: 0.4f, factionArrivalWeight: 0f),
                ["Phoebe"] = new FallbackProfile(8.0f, threatBigWeight: 0f, threatSmallWeight: 0.6f, miscWeight: 0f, factionArrivalWeight: 0.4f),
            };

        private static readonly FallbackProfile DisabledFallbackProfile =
            new FallbackProfile(999f, threatBigWeight: 1.0f, threatSmallWeight: 0f, miscWeight: 0f, factionArrivalWeight: 0f);

        public static float ClampPointsMultiplier(float multiplier)
            => Math.Clamp(multiplier, 0.3f, 2.0f);

        public static bool ShouldNotify(
            bool notificationsEnabled,
            StorytellerIncidentKind kind)
            => notificationsEnabled
               && (kind == StorytellerIncidentKind.ThreatBig
                   || kind == StorytellerIncidentKind.ThreatSmall);

        public static bool RequiresHostileFaction(
            string? incidentDefName,
            string? categoryDefName)
            => (string.Equals(categoryDefName, ThreatBigCategoryDefName, StringComparison.Ordinal)
                || string.Equals(categoryDefName, ThreatSmallCategoryDefName, StringComparison.Ordinal))
               && !string.Equals(incidentDefName, RaidFriendlyDefName, StringComparison.Ordinal);

        public static float GetTensionDeltaForCategory(string? categoryDefName)
        {
            if (string.IsNullOrEmpty(categoryDefName))
                return 0f;

            return CategoryTensionDeltas.TryGetValue(categoryDefName!, out float delta) ? delta : 0f;
        }

        public static FallbackProfile ResolveFallbackProfile(string? modeName)
        {
            if (!string.IsNullOrEmpty(modeName) &&
                FallbackProfilesByModeName.TryGetValue(modeName!, out var profile))
            {
                return profile;
            }

            return DisabledFallbackProfile;
        }
    }
}
