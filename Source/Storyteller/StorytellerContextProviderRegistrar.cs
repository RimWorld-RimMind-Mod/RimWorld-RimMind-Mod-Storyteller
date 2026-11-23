using System;
using System.Collections.Generic;
using System.Text;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;
using RimMind.Storyteller.Extensions;
using RimMind.Storyteller.Memory;

namespace RimMind.Storyteller
{
    internal static class StorytellerContextProviderRegistrar
    {
        private const string ModId = StorytellerRequestIds.OwnerModId;

        private const string DefaultSystemInstruction =
            "You are the RimWorld storyteller AI. Based on the colony's current situation, select the most appropriate incident event. " +
            "Consider colony wealth, threat level, food supply, colonist count, and recent events. " +
            "Output must be valid JSON matching the IncidentOutput schema.";

        private const string DefaultWorldRules =
            "Storyteller rules:\n" +
            "- Only select from available RimWorld incident definitions\n" +
            "- Consider threat level relative to colony wealth\n" +
            "- Balance positive and negative events\n" +
            "- Food shortages should trigger related events\n" +
            "- Low mood colonists may need positive events";

        private const string DefaultChatSystemInstruction =
            "You are the AI Storyteller of this RimWorld colony speaking directly with the player through the Storyteller Altar. " +
            "Respond in character as the narrator and director — perceptive, dramatic, and aware of colony history, narrative tension, and recent events. " +
            "Reply in concise natural prose (do NOT output JSON). Consider player wishes without blindly obeying them.";

        private static readonly string[] SharedPawnExcludeKeys =
        {
            "npc_identity", "pawn_base_info", "fixed_relations",
            "ideology", "skills_summary", "health", "mood", "current_job",
            "combat_status", "target_info", "task_progress", "npc_commands"
        };

        private static readonly string[] ChatExcludeKeys =
        {
            "npc_identity", "pawn_base_info", "fixed_relations",
            "ideology", "skills_summary", "health", "mood", "current_job",
            "combat_status", "target_info", "task_progress", "npc_commands",
            "storyteller_rules", "storyteller_task"
        };

        private static bool IsIncidentScenario(string? scenario)
            => string.Equals(scenario, RimMindAPI.Context.ScenarioStoryteller, StringComparison.Ordinal);

        private static bool IsChatScenario(string? scenario)
            => string.Equals(scenario, StorytellerRequestIds.ChatScenarioId, StringComparison.Ordinal);

        private static bool IsAnyStorytellerScenario(string? scenario)
            => IsIncidentScenario(scenario) || IsChatScenario(scenario);

        internal static void RegisterAll()
        {
            // Register the typed event before any key names it, so the trigger resolves
            // to a real event (unknown names never fall back to the base bus event).
            StorytellerStateChangedEvent.Register();

            // Register Storyteller-owned IncidentOutput schema into Core's SchemaRegistry.
            RimMindAPI.Context.RegisterSchema(
                StorytellerRequestIds.IncidentOutputSchemaKey,
                StorytellerRequestIds.IncidentOutputSchemaJson);

            var sharedWeights = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                ["nearby_pawns"] = 0.5f,
                ["colony_status"] = 0.9f,
                ["current_area"] = 0.4f,
                ["weather"] = 0.6f,
                ["time_of_day"] = 0.5f,
                ["season"] = 0.7f,
                ["map_structure"] = 0.8f,
                ["memory_pawn"] = 0.3f,
                ["working_memory"] = 0.2f,
                ["memory_narrator"] = 0.9f,
                ["storyteller_system"] = 1.0f,
                ["storyteller_dialogue"] = 0.6f,
                ["storyteller_context"] = 0.95f,
                ["storyteller_reactions"] = 0.9f,
                ["storyteller_recent_incidents"] = 0.85f
            };

            var incidentWeights = new Dictionary<string, float>(sharedWeights, StringComparer.Ordinal)
            {
                ["storyteller_rules"] = 0.98f,
                ["storyteller_task"] = 0.95f
            };

            var chatWeights = new Dictionary<string, float>(sharedWeights, StringComparer.Ordinal)
            {
                ["storyteller_dialogue"] = 0.95f
            };

            RimMindAPI.Context.RegisterScenario(new ScenarioDescriptor
            {
                Id = ScenarioIds.Storyteller,
                DefaultBaseRounds = 8,
                Description = "RimMind.Application.Scenario.Storyteller",
                DefaultBudget = 0.7f,
                L4Mode = L4Mode.NarrativeMemory,
                DefaultExcludeKeys = SharedPawnExcludeKeys,
                OwnerModId = ModId,
                RelevanceWeights = incidentWeights
            });

            RimMindAPI.Context.RegisterScenario(new ScenarioDescriptor
            {
                Id = StorytellerRequestIds.ChatScenarioId,
                DefaultBaseRounds = 8,
                Description = "RimMind.Storyteller.Dialogue.Title",
                DefaultBudget = 0.7f,
                L4Mode = L4Mode.NarrativeMemory,
                DefaultExcludeKeys = ChatExcludeKeys,
                OwnerModId = ModId,
                RelevanceWeights = chatWeights
            });

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "storyteller_system", ContextLayer.L0_Static, 1.0f,
                async (ctx, ct) =>
                {
                    if (!IsAnyStorytellerScenario(ctx.Scenario))
                        return null;

                    bool isChat = IsChatScenario(ctx.Scenario);
                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                    {
                        string baseInstruction = isChat
                            ? RimMindAPI.Text.TrOr(
                                "RimMind.Storyteller.Prompt.ChatSystemInstruction",
                                DefaultChatSystemInstruction)
                            : RimMindAPI.Text.TrOr(
                                "RimMind.Storyteller.Prompt.SystemInstruction",
                                DefaultSystemInstruction);

                        // Inject player CustomSystemPrompt in one unified L0 provider for both modes.
                        return StorytellerContextPolicy.ComposeTaskInstruction(
                            StorytellerMemory.Instance?.CustomSystemPrompt,
                            baseInstruction);
                    }, null, ct: ct).ConfigureAwait(false);
                }, ownerMod: ModId, stalenessTicks: 0,
                cacheScope: CacheScope.Static));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "storyteller_rules", ContextLayer.L0_Static, 0.98f,
                async (ctx, ct) =>
                {
                    if (!IsIncidentScenario(ctx.Scenario))
                        return null;

                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.Prompt.WorldRules", DefaultWorldRules),
                        null, ct: ct).ConfigureAwait(false);
                }, ownerMod: ModId, stalenessTicks: 0,
                cacheScope: CacheScope.Static));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "storyteller_dialogue", ContextLayer.L3_State, 0.5f,
                async (ctx, ct) =>
                {
                    if (!IsAnyStorytellerScenario(ctx.Scenario))
                        return null;

                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                    {
                        var mem = StorytellerMemory.Instance;
                        if (mem == null) return null;
                        string dialogue = mem.GetRecentDialogueSummary(5);
                        if (string.IsNullOrEmpty(dialogue))
                            return null;

                        string header = RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.Dialogue.StorytellerDialogueHeader",
                            "[RimMind Storyteller Dialogue]");
                        return $"{header}\n{dialogue}";
                    }, null, ct: ct).ConfigureAwait(false);
                }, ownerMod: ModId, stalenessTicks: 750, invalidationTriggers: new[] { StorytellerStateChangedEvent.EventName },
                cacheScope: CacheScope.Scenario));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "storyteller_task", ContextLayer.L0_Static, 0.95f,
                async (ctx, ct) =>
                {
                    if (!IsIncidentScenario(ctx.Scenario))
                        return null;

                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                        RimMindAPI.Prompt.BuildTaskInstruction(
                            "RimMind.Storyteller.Prompt.TaskInstruction",
                            null,
                            "Role", "Goal", "Process", "Constraint", "Example", "Output", "Fallback",
                            "SystemJsonFormat", "SystemTensionGuidance", "SystemChainGuidance",
                            "SystemParamsGuidance", "SystemRequirements"),
                        null, ct: ct).ConfigureAwait(false);
                }, ownerMod: ModId, stalenessTicks: 0,
                cacheScope: CacheScope.Static));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "storyteller_context", ContextLayer.L1_Baseline, 0.85f,
                async (ctx, ct) =>
                {
                    if (!IsAnyStorytellerScenario(ctx.Scenario))
                        return null;

                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                    {
                        var mem = StorytellerMemory.Instance;
                        if (mem == null) return null;
                        var sb = new StringBuilder();
                        sb.AppendLine(RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.Prompt.StorytellerStateHeader",
                            "[RimMind Storyteller]"));
                        StorytellerContextBuilder.AppendDifficultyContext(sb);
                        StorytellerContextBuilder.AppendThreatLevel(sb);
                        StorytellerContextBuilder.AppendTensionLabel(sb, mem.TensionLevel);
                        sb.AppendLine(RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.Prompt.TensionLevel",
                            "[Narrative Tension] Current tension: {0} ({1}/1.0)",
                            $"{(int)(mem.TensionLevel * 100)}%", $"{mem.TensionLevel:F2}"));
                        string summary = mem.GetRecentSummary(5);
                        if (!string.IsNullOrEmpty(summary))
                            sb.AppendLine(summary);
                        string chains = mem.GetActiveChainsSummary();
                        if (!string.IsNullOrEmpty(chains))
                            sb.AppendLine(chains);
                        return sb.ToString().TrimEnd();
                    }, null, ct: ct).ConfigureAwait(false);
                }, ownerMod: ModId, stalenessTicks: 3000, invalidationTriggers: new[] { StorytellerStateChangedEvent.EventName },
                cacheScope: CacheScope.Scenario));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "storyteller_reactions", ContextLayer.L1_Baseline, 0.8f,
                async (ctx, ct) =>
                {
                    if (!IsAnyStorytellerScenario(ctx.Scenario))
                        return null;

                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                    {
                        var mem = StorytellerMemory.Instance;
                        if (mem == null) return null;
                        string? text = mem.ConsumedReactionsText;
                        return !string.IsNullOrEmpty(text) ? text : null;
                    }, null, ct: ct).ConfigureAwait(false);
                }, ownerMod: ModId, stalenessTicks: 3000, invalidationTriggers: new[] { StorytellerStateChangedEvent.EventName },
                cacheScope: CacheScope.Scenario));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "storyteller_recent_incidents", ContextLayer.L4_History, 0.7f,
                async (ctx, ct) =>
                {
                    if (!IsAnyStorytellerScenario(ctx.Scenario))
                        return null;

                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                    {
                        var narrations = RimMindAPI.Memory.GetRecentNarrations(5);
                        if (narrations.Count == 0) return null;

                        var narrationText = new StringBuilder();
                        narrationText.AppendLine(RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.Prompt.RecentIncidents",
                            "[Recent Narrative Incidents]"));
                        foreach (var narration in narrations)
                        {
                            int day = RimMind.Domain.Common.RimMindTime.TicksToDay(narration.Tick);
                            narrationText.AppendLine($"[Day {day}] {narration.Content}");
                        }
                        return narrationText.ToString().TrimEnd();
                    }, null, ct: ct).ConfigureAwait(false);
                }, ownerMod: ModId, stalenessTicks: 3000, invalidationTriggers: new[] { StorytellerStateChangedEvent.EventName },
                cacheScope: CacheScope.Scenario));
        }
    }
}
