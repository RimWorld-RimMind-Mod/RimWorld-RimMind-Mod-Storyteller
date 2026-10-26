using System.Text;
using RimMind.Application.Common.Interfaces.Context;
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
        {
            try
            {
                if (string.IsNullOrEmpty(key)) return string.Empty;
                string template = key.CanTranslate() ? key.TranslateSimple() : key;
                return (args == null || args.Length == 0) ? template : string.Format(template, args);
            }
            catch { return key ?? string.Empty; }
        }

        internal static void RegisterAll()
        {
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
                }, ownerMod: ModId, stalenessTicks: 750, invalidationTriggers: new[] { "StorytellerEvent" },
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
                }, ownerMod: ModId, stalenessTicks: 0, invalidationTriggers: new[] { "StorytellerEvent" },
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
                }, ownerMod: ModId, stalenessTicks: 3000, invalidationTriggers: new[] { "StorytellerEvent" },
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
                }, ownerMod: ModId, stalenessTicks: 3000, invalidationTriggers: new[] { "StorytellerEvent" },
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
                }, ownerMod: ModId, stalenessTicks: 3000, invalidationTriggers: new[] { "StorytellerEvent" },
                cacheScope: CacheScope.Scenario));
        }

    }
}
