using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimMind.Presentation.Api;

namespace RimMind.Storyteller.Memory
{
    /// <summary>
    /// Stateless prompt-text formatting helpers separated from <see cref="StorytellerMemory"/>
    /// ring-buffer storage and WorldComponent persistence.
    /// </summary>
    internal static class StorytellerMemoryPromptFormatter
    {
        public static string FormatRecentIncidents(IReadOnlyList<IncidentHistoryRecord> records, int count)
        {
            if (records == null || records.Count == 0 || count <= 0)
                return string.Empty;

            var sb = new StringBuilder();
            int start = Math.Max(0, records.Count - count);
            for (int i = start; i < records.Count; i++)
            {
                var r = records[i];
                int day = TensionMath.TicksToDay(r.TriggeredTick);
                sb.AppendLine(RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Prompt.DaySummary",
                    "Day {0}: {1} ({2})",
                    day,
                    r.Label,
                    r.MapName));
            }
            return sb.ToString().TrimEnd();
        }

        public static string FormatRecentDialogues(IReadOnlyList<DialogueRecord> records, int count)
        {
            if (records == null || records.Count == 0 || count <= 0)
                return string.Empty;

            var sb = new StringBuilder();
            int start = Math.Max(0, records.Count - count);
            for (int i = start; i < records.Count; i++)
            {
                var r = records[i];
                int day = TensionMath.TicksToDay(r.tick);
                string prefix = string.Equals(r.role, "user", StringComparison.OrdinalIgnoreCase)
                    ? RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.RolePlayer", "Player")
                    : RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.RoleNarrator", "Narrator");
                sb.AppendLine(RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Prompt.DialogueRecordLine",
                    "Day {0} {1}: {2}",
                    day.ToString(),
                    prefix,
                    r.content));
            }
            return sb.ToString().TrimEnd();
        }

        public static string FormatPlayerReactions(IReadOnlyList<PlayerReactionRecord> reactions, int maxCount)
        {
            if (reactions == null || reactions.Count == 0 || maxCount <= 0)
                return string.Empty;

            int takeCount = Math.Min(maxCount, reactions.Count);
            var sb = new StringBuilder();
            sb.AppendLine(RimMindAPI.Text.TrOr(
                "RimMind.Storyteller.Prompt.PlayerReactions",
                "[Player Reaction Records]"));
            for (int i = 0; i < takeCount; i++)
            {
                var r = reactions[i];
                int day = TensionMath.TicksToDay(r.tick);
                sb.AppendLine(RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Prompt.ReactionRecordLine",
                    "Day {0} {1} → Player: {2}",
                    day.ToString(),
                    r.incidentLabel,
                    r.reactionLabel));
            }
            return sb.ToString().TrimEnd();
        }

        public static string FormatActiveChains(IReadOnlyList<EventChain> chains)
        {
            if (chains == null || chains.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < chains.Count; i++)
            {
                var chain = chains[i];
                string triggeredNames = string.Join(", ", chain.steps.Select(s => s.incidentDefName));
                sb.AppendLine(RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Prompt.ChainProgress",
                    "{0} (step {1}/{2}, triggered: {3})",
                    chain.chainId,
                    chain.currentStep.ToString(),
                    chain.totalSteps.ToString(),
                    triggeredNames));
                sb.AppendLine(RimMindAPI.Text.TrOr(
                    "RimMind.Storyteller.Prompt.ChainHint",
                    "  AI hint: {0}",
                    chain.nextHint ?? string.Empty));
            }
            return sb.ToString().TrimEnd();
        }
    }
}
