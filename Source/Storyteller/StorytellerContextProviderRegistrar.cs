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
using Verse;

namespace RimMind.Storyteller
{
    internal static class StorytellerContextProviderRegistrar
    {
        private const string ModId = RimMind.Application.Common.Constants.RimMindOwnerConsts.StorytellerModId;

        private static string Tr(string key, params object[] args)
            => RimMindText.TrOr(key, key ?? string.Empty, args);

        internal static void RegisterAll()
        {
            // Register the typed event before any key names it, so the trigger resolves
            // to a real event (unknown names never fall back to the base bus event).
            StorytellerStateChangedEvent.Register();

            ScenarioRegistry.Register(new ScenarioDescriptor
            {
                Id = ScenarioIds.Storyteller,
                DefaultBaseRounds = 8,
                Description = "RimMind.Application.Scenario.Storyteller",
                DefaultBudget = 0.7f,
                L4Mode = L4Mode.NarrativeMemory,
                DefaultExcludeKeys = new[]
                {
                    "npc_identity", "pawn_base_info", "fixed_relations",
                    "ideology", "skills_summary", "health", "mood", "current_job",
                    "combat_status", "target_info", "task_progress", "npc_commands"
                },
                OwnerModId = ModId,
                RelevanceWeights = new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    ["health"] = 0.3f,
                    ["mood"] = 0.4f,
                    ["current_job"] = 0.2f,
                    ["combat_status"] = 0.8f,
                    ["target_info"] = 0.7f,
                    ["task_progress"] = 0.3f,
                    ["nearby_pawns"] = 0.5f,
                    ["colony_status"] = 0.9f,
                    ["current_area"] = 0.4f,
                    ["weather"] = 0.6f,
                    ["time_of_day"] = 0.5f,
                    ["season"] = 0.7f,
                    ["map_structure"] = 0.8f,
                    ["pawn_base_info"] = 0.3f,
                    ["fixed_relations"] = 0.3f,
                    ["ideology"] = 0.4f,
                    ["skills_summary"] = 0.2f,
                    ["memory_pawn"] = 0.3f,
                    ["working_memory"] = 0.2f,
                    ["memory_narrator"] = 0.9f,
                    ["storyteller_dialogue"] = 0.6f,
                    ["storyteller_task"] = 0.95f,
                    ["storyteller_context"] = 0.95f,
                    ["storyteller_reactions"] = 0.9f,
                    ["storyteller_recent_incidents"] = 0.85f
                }
            });

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "storyteller_dialogue", ContextLayer.L3_State, 0.5f,
                async (ctx, ct) =>
                {
                    if (ctx.Scenario != RimMindAPI.Context.ScenarioStoryteller)
                    {
                        return null;
                    }
                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                    {
                        var mem = StorytellerMemory.Instance;
                        if (mem == null) return null;
                        string dialogue = mem.GetRecentDialogueSummary(5);
                        return string.IsNullOrEmpty(dialogue)
                            ? null
                            : $"{Tr("RimMind.Storyteller.Dialogue.StorytellerDialogueHeader")}\n{dialogue}";
                    }, null, ct: ct).ConfigureAwait(false);
                }, ownerMod: ModId, stalenessTicks: 750, invalidationTriggers: new[] { StorytellerStateChangedEvent.EventName },
                cacheScope: CacheScope.Scenario));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "storyteller_task", ContextLayer.L0_Static, 0.95f,
                async (ctx, ct) =>
                {
                    if (ctx.Scenario != RimMindAPI.Context.ScenarioStoryteller)
                    {
                        return null;
                    }
                    if (ctx.Hints != null
                        && ctx.Hints.TryGetValue("IsStorytellerChat", out var isChat)
                        && (isChat is true || (isChat is string isChatStr && string.Equals(isChatStr, "true", System.StringComparison.OrdinalIgnoreCase))))
                    {
                        return null;
                    }
                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                    {
                        string taskInstruction = RimMindAPI.Prompt.BuildTaskInstruction(
                            "RimMind.Storyteller.Prompt.TaskInstruction",
                            null,
                            "Role", "Goal", "Process", "Constraint", "Example", "Output", "Fallback",
                            "SystemJsonFormat", "SystemTensionGuidance", "SystemChainGuidance",
                            "SystemParamsGuidance", "SystemRequirements");

                        // 将 UI 中存储的 CustomSystemPrompt 前置注入到任务指令中，
                        // 使玩家自定义的系统层提示词作为最高优先级上下文生效。
                        return StorytellerContextPolicy.ComposeTaskInstruction(
                            StorytellerMemory.Instance?.CustomSystemPrompt,
                            taskInstruction);
                    }, null, ct: ct).ConfigureAwait(false);
                }, ownerMod: ModId, stalenessTicks: 0,
                cacheScope: CacheScope.Static));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "storyteller_context", ContextLayer.L1_Baseline, 0.85f,
                async (ctx, ct) =>
                {
                    if (ctx.Scenario != RimMindAPI.Context.ScenarioStoryteller)
                    {
                        return null;
                    }
                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                    {
                        var mem = StorytellerMemory.Instance;
                        if (mem == null) return null;
                        var sb = new StringBuilder();
                        sb.AppendLine(Tr("RimMind.Storyteller.Prompt.StorytellerStateHeader"));
                        StorytellerContextBuilder.AppendDifficultyContext(sb);
                        StorytellerContextBuilder.AppendThreatLevel(sb);
                        StorytellerContextBuilder.AppendTensionLabel(sb, mem.TensionLevel);
                        sb.AppendLine(Tr("RimMind.Storyteller.Prompt.TensionLevel",
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
                    if (ctx.Scenario != RimMindAPI.Context.ScenarioStoryteller)
                    {
                        return null;
                    }
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
                    if (ctx.Scenario != RimMindAPI.Context.ScenarioStoryteller)
                    {
                        return null;
                    }
                    return await RimMindAPI.MainThreadSnapshot.GetAsync<string?>(() =>
                    {
                        var narrations = RimMindAPI.Memory.GetRecentNarrations(5);
                        if (narrations.Count == 0) return null;

                        var narrationText = new StringBuilder();
                        narrationText.AppendLine(Tr("RimMind.Storyteller.Prompt.RecentIncidents"));
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
