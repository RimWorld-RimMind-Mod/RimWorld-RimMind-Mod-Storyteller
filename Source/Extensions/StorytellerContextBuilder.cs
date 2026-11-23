using System.Text;
using RimMind.Presentation.Api;
using RimWorld;
using Verse;

namespace RimMind.Storyteller.Extensions
{
    /// <summary>
    /// Builds storyteller context text sections (difficulty, threat, tension).
    /// Pure tier resolution helpers are unit-tested directly; Append helpers read Find.Storyteller
    /// and format text via <see cref="RimMindAPI.Text.TrOr"/> with English fallbacks.
    /// </summary>
    public static class StorytellerContextBuilder
    {
        private readonly struct DifficultyTierDescriptor
        {
            public readonly float MaxExclusiveThreatScale;
            public readonly string FallbackName;
            public readonly string GuidanceKey;
            public readonly string GuidanceFallback;

            public DifficultyTierDescriptor(
                float maxExclusiveThreatScale,
                string fallbackName,
                string guidanceKey,
                string guidanceFallback)
            {
                MaxExclusiveThreatScale = maxExclusiveThreatScale;
                FallbackName = fallbackName;
                GuidanceKey = guidanceKey;
                GuidanceFallback = guidanceFallback;
            }
        }

        private static readonly DifficultyTierDescriptor[] DifficultyTiers =
        {
            new DifficultyTierDescriptor(
                0.1f,
                "Peaceful",
                "RimMind.Storyteller.Prompt.DifficultyPeaceful",
                "Peaceful — Prioritize non-threatening events, avoid bringing serious crisis to the colony, maintain a relaxed atmosphere"),
            new DifficultyTierDescriptor(
                0.5f,
                "Community Builder",
                "RimMind.Storyteller.Prompt.DifficultyEasy",
                "Easy — Moderately reduce threat event frequency and intensity, occasionally provide small challenges"),
            new DifficultyTierDescriptor(
                0.8f,
                "Adventure Story",
                "RimMind.Storyteller.Prompt.DifficultyMedium",
                "Medium — Balance threat and peaceful events, maintain story rhythm"),
            new DifficultyTierDescriptor(
                1.2f,
                "Strive to Survive",
                "RimMind.Storyteller.Prompt.DifficultyRough",
                "Rough — Select events at a normal pace, provide appropriate challenges"),
            new DifficultyTierDescriptor(
                1.8f,
                "Blood and Dust",
                "RimMind.Storyteller.Prompt.DifficultyHard",
                "Hard — Increase threat event intensity and frequency, test the colony's survival ability"),
            new DifficultyTierDescriptor(
                float.PositiveInfinity,
                "Losing is Fun",
                "RimMind.Storyteller.Prompt.DifficultyExtreme",
                "Extreme — Select the most challenging events, give the colony no room to breathe"),
        };

        /// <summary>
        /// Resolves threatScale into a 0..5 difficulty tier.
        /// </summary>
        public static int ResolveDifficultyTier(float threatScale)
        {
            for (int i = 0; i < DifficultyTiers.Length; i++)
            {
                if (threatScale < DifficultyTiers[i].MaxExclusiveThreatScale)
                    return i;
            }
            return DifficultyTiers.Length - 1;
        }

        /// <summary>
        /// Returns the translation key for the given difficulty tier, or null if out of range.
        /// </summary>
        public static string? ResolveDifficultyGuidanceKey(int difficultyLevel)
        {
            if (difficultyLevel < 0 || difficultyLevel >= DifficultyTiers.Length)
                return null;
            return DifficultyTiers[difficultyLevel].GuidanceKey;
        }

        /// <summary>
        /// Returns the fallback vanilla difficulty name for the given tier.
        /// </summary>
        public static string ResolveDifficultyName(int difficultyLevel)
        {
            if (difficultyLevel < 0 || difficultyLevel >= DifficultyTiers.Length)
                return $"Custom ({difficultyLevel})";
            return DifficultyTiers[difficultyLevel].FallbackName;
        }

        /// <summary>
        /// Appends difficulty name (preferring RimWorld's live difficultyDef label), threat scale,
        /// threat-restriction flags, and behavioral guidance.
        /// </summary>
        public static void AppendDifficultyContext(StringBuilder sb)
        {
            var storyteller = Find.Storyteller;
            var diff = storyteller?.difficulty;
            if (diff == null) return;

            int tier = ResolveDifficultyTier(diff.threatScale);
            string? liveLabel = storyteller?.difficultyDef?.LabelCap.ToString();
            string difficultyName = !string.IsNullOrWhiteSpace(liveLabel)
                ? liveLabel!
                : ResolveDifficultyName(tier);

            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Storyteller.Prompt.DifficultyContext",
                "[Difficulty Settings]"));
            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Storyteller.Prompt.DifficultyDetail",
                "  {0} (threat multiplier={1})",
                difficultyName,
                $"{diff.threatScale:F2}"));

            if (!diff.allowBigThreats)
            {
                sb.AppendLine(RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Prompt.NoBigThreats",
                    "  Major threats disabled -> do not select ThreatBig category events"));
            }
            if (!diff.allowIntroThreats)
            {
                sb.AppendLine(RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Prompt.NoIntroThreats",
                    "  Intro threats disabled"));
            }
            if (!diff.allowViolentQuests)
            {
                sb.AppendLine(RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Prompt.NoViolentQuests",
                    "  Violent quests disabled"));
            }

            if (tier >= 0 && tier < DifficultyTiers.Length)
            {
                var descriptor = DifficultyTiers[tier];
                string guidanceText = RimMindAPI.Text.TrOr(descriptor.GuidanceKey, descriptor.GuidanceFallback);
                sb.AppendLine(RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Prompt.DifficultyGuidance",
                    "Difficulty behavior guidance: {0}",
                    guidanceText));
            }
        }

        /// <summary>
        /// Appends threat level label (None/Low/Medium/High) from current difficulty threatScale.
        /// </summary>
        public static void AppendThreatLevel(StringBuilder sb)
        {
            var diff = Find.Storyteller?.difficulty;
            if (diff == null) return;

            string threatLabel = diff.threatScale switch
            {
                < 0.1f => RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.ThreatNone", "None"),
                < 0.5f => RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.ThreatLow", "Low"),
                < 0.8f => RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.ThreatMedium", "Medium"),
                _ => RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.ThreatHigh", "High")
            };
            sb.AppendLine($"[Threat Level] {threatLabel}");
        }

        /// <summary>
        /// Appends narrative tension bucket label (VeryLow/Low/Medium/High/VeryHigh).
        /// </summary>
        public static void AppendTensionLabel(StringBuilder sb, float tension)
        {
            string tensionLabel = tension switch
            {
                < 0.2f => RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.TensionVeryLow", "Very Low"),
                < 0.4f => RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.TensionLow", "Low"),
                < 0.6f => RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.TensionMedium", "Medium"),
                < 0.8f => RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.TensionHigh", "High"),
                _ => RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.TensionVeryHigh", "Very High")
            };
            sb.AppendLine($"[Tension Category] {tensionLabel}");
        }
    }
}
