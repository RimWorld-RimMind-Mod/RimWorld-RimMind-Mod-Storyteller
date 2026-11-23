using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;
using RimMind.Storyteller.Memory;
using RimMind.Storyteller.UI;
using UnityEngine;
using Verse;
using Xunit;

namespace RimMind.Storyteller.Tests.Contracts
{
    public sealed class StorytellerCoreAdoptionContracts
    {
        [Fact]
        public async Task Storyteller_OwnsItsOwnScenariosSchemaAndL0Prompts_ForBothIncidentAndAltarChatModes()
        {
            RimMindAPI.Context.ContextKeys.Definitions.Clear();
            RimMindAPI.Context.RegisteredSchemas.Clear();
            StorytellerMemory.Instance = new StorytellerMemory
            {
                CustomSystemPrompt = "Be mysterious and poetic."
            };
            try
            {
                StorytellerContextProviderRegistrar.RegisterAll();

                // Storyteller registers its own IncidentOutput JSON Schema
                string? incidentSchema = RimMindAPI.Context.FindSchema(StorytellerRequestIds.IncidentOutputSchemaKey);
                Assert.NotNull(incidentSchema);
                var parsedSchema = JObject.Parse(incidentSchema!);
                Assert.Equal("object", parsedSchema["type"]?.ToString());
                Assert.NotNull(parsedSchema["properties"]?["defName"]);

                var incidentScenario = ScenarioRegistry.Get(ScenarioIds.Storyteller);
                Assert.NotNull(incidentScenario);
                Assert.Equal(RimMindOwnerConsts.StorytellerModId, incidentScenario!.OwnerModId);
                Assert.Equal(L4Mode.NarrativeMemory, incidentScenario.L4Mode);
                Assert.True(incidentScenario.RelevanceWeights != null && incidentScenario.RelevanceWeights.ContainsKey("storyteller_system"));
                Assert.True(incidentScenario.RelevanceWeights!.ContainsKey("storyteller_rules"));
                // Pruned dead weights: no excluded key should also appear in RelevanceWeights
                foreach (string excluded in incidentScenario.DefaultExcludeKeys)
                {
                    Assert.False(incidentScenario.RelevanceWeights.ContainsKey(excluded),
                        $"Excluded key '{excluded}' must not remain in RelevanceWeights.");
                }

                // Altar chat has its own registered scenario cleanly separated from incident selection
                var chatScenario = ScenarioRegistry.Get(StorytellerRequestIds.ChatScenarioId);
                Assert.NotNull(chatScenario);
                Assert.Equal(RimMindOwnerConsts.StorytellerModId, chatScenario!.OwnerModId);
                Assert.Contains("storyteller_rules", chatScenario.DefaultExcludeKeys);
                Assert.Contains("storyteller_task", chatScenario.DefaultExcludeKeys);

                var systemProvider = RimMindAPI.Context.ContextKeys.Definitions.Single(d => d.Key == "storyteller_system");
                var rulesProvider = RimMindAPI.Context.ContextKeys.Definitions.Single(d => d.Key == "storyteller_rules");
                var taskProvider = RimMindAPI.Context.ContextKeys.Definitions.Single(d => d.Key == "storyteller_task");
                Assert.Equal(ContextLayer.L0_Static, systemProvider.Layer);
                Assert.Equal(ContextLayer.L0_Static, rulesProvider.Layer);
                Assert.Equal(ContextLayer.L0_Static, taskProvider.Layer);

                var incidentCtx = new ProviderContext(ScenarioIds.Storyteller, "t-inc") { NpcId = StorytellerRequestIds.DefaultNpcId };
                string? incidentSys = await systemProvider.Provider(incidentCtx, CancellationToken.None);
                string? incidentRules = await rulesProvider.Provider(incidentCtx, CancellationToken.None);
                string? incidentTask = await taskProvider.Provider(incidentCtx, CancellationToken.None);
                Assert.NotNull(incidentSys);
                Assert.Contains("Be mysterious and poetic.", incidentSys!);
                Assert.Contains("storyteller AI", incidentSys!);
                Assert.NotNull(incidentRules);
                Assert.Contains("Storyteller rules", incidentRules!);
                Assert.NotNull(incidentTask);
                // CustomSystemPrompt is composed once in storyteller_system, not duplicated in storyteller_task
                Assert.DoesNotContain("Be mysterious and poetic.", incidentTask!);

                // Non-storyteller scenarios must receive null (strict scenario guard)
                Assert.Null(await systemProvider.Provider(incidentCtx with { Scenario = ScenarioIds.Dialogue }, CancellationToken.None));
                Assert.Null(await rulesProvider.Provider(incidentCtx with { Scenario = ScenarioIds.AgentDecision }, CancellationToken.None));

                // Altar chat scenario receives chat persona + CustomSystemPrompt, and omits incident JSON rules/task
                var chatCtx = new ProviderContext(StorytellerRequestIds.ChatScenarioId, "t-chat")
                {
                    NpcId = StorytellerRequestIds.DefaultNpcId
                };
                string? chatSys = await systemProvider.Provider(chatCtx, CancellationToken.None);
                Assert.NotNull(chatSys);
                Assert.Contains("Be mysterious and poetic.", chatSys!);
                Assert.Contains("Storyteller Altar", chatSys!);
                Assert.Null(await rulesProvider.Provider(chatCtx, CancellationToken.None));
                Assert.Null(await taskProvider.Provider(chatCtx, CancellationToken.None));
            }
            finally
            {
                StorytellerMemory.Instance = null;
                RimMindAPI.Context.ContextKeys.Definitions.Clear();
                RimMindAPI.Context.RegisteredSchemas.Clear();
            }
        }

        [Fact]
        public void StorytellerResponseParserPure_HandlesThinkingTagsAndPseudoFunctionCallsViaCore()
        {
            string withThinking = "<thinking>Tension is low.</thinking>\n```json\n{\"defName\":\"RaidEnemy\",\"reason\":\"raise tension\"}\n```";
            var parsed = StorytellerResponseParserPure.ParseResponse(withThinking);
            Assert.NotNull(parsed);
            Assert.Equal("RaidEnemy", parsed!.defName);
            Assert.Equal("raise tension", parsed.reason);

            string pseudoCall = "select_incident(defName=\"TraderCaravanArrival\", reason=\"colony needs relief\")";
            var pseudoParsed = StorytellerResponseParserPure.ParseResponse(pseudoCall);
            Assert.NotNull(pseudoParsed);
            Assert.Equal("TraderCaravanArrival", pseudoParsed!.defName);
            Assert.Equal("colony needs relief", pseudoParsed.reason);

            Assert.Null(StorytellerResponseParserPure.TryRepairTruncatedJson("{\"defName\":\"Eclipse\"}"));
            string? repaired = StorytellerResponseParserPure.TryRepairTruncatedJson("{\"defName\":\"Eclipse\",\"reason\":\"darkness");
            Assert.NotNull(repaired);
            Assert.Contains("\"Eclipse\"", repaired!);
        }

        [Fact]
        public void Window_StorytellerDialogue_UsesDedicatedChatScenarioAndModId_CancelsOnPostClose_AndPersistsReplies()
        {
            RimMindAPI.Configured = true;
            RimMindAPI.Sent.Clear();
            RimMindAPI.Memory.Narrations.Clear();
            Widgets.Labels.Clear();
            Find.TickManager.TicksGame = 60000;
            var memory = new StorytellerMemory();
            memory.RecordDialogue("assistant", "Greetings, colonist.", 0);
            StorytellerMemory.Instance = memory;

            try
            {
                var window = new Window_StorytellerDialogue(new Map());
                // Pre-loaded history must appear without duplicating into memory
                Assert.Single(window.Messages);
                Assert.Single(memory.DialogueRecords);

                Widgets.NextText = "Send us traders please.";
                Widgets.ClickSend = true;
                window.DoWindowContents(new Rect(0, 0, 520, 560));

                var sent = Assert.Single(RimMindAPI.Sent);
                Assert.Equal(StorytellerRequestIds.ChatScenarioId, sent.Envelope.ScenarioId);
                Assert.Equal(StorytellerRequestIds.ChatModId, sent.Envelope.ModId);
                Assert.NotEqual(StorytellerRequestIds.RequestModId, sent.Envelope.ModId);
                Assert.Equal(false, sent.Envelope.RequireAliveColonist);
                Assert.True(sent.Envelope.Ct.CanBeCanceled);
                Assert.False(sent.Envelope.Ct.IsCancellationRequested);
                // Control flags must never leak into GameStateInfo prompt text
                string serializedGameState = sent.Envelope.GameStateInfo?.ToString() ?? string.Empty;
                Assert.DoesNotContain("IsStorytellerChat", serializedGameState);

                // Deliver assistant reply and drain on next frame
                sent.Complete(Result<LlmResponse, RimMindError>.Ok(new LlmResponse { Content = "Fortune favors the bold." }));
                window.DoWindowContents(new Rect(0, 0, 520, 560));
                Assert.Equal(3, window.Messages.Count);
                Assert.Equal(3, memory.DialogueRecords.Count);
                Assert.Equal(2, RimMindAPI.Memory.Narrations.Count);

                // Second turn: closing window MUST cancel the in-flight CancellationToken and drop late replies
                Widgets.NextText = "Any storms ahead?";
                Widgets.ClickSend = true;
                window.DoWindowContents(new Rect(0, 0, 520, 560));
                Assert.Equal(2, RimMindAPI.Sent.Count);

                window.PostClose();
                Assert.True(RimMindAPI.Sent[1].Envelope.Ct.IsCancellationRequested);

                int recordsBeforeLateReply = memory.DialogueRecords.Count;
                RimMindAPI.Sent[1].Complete(Result<LlmResponse, RimMindError>.Ok(new LlmResponse { Content = "Late reply" }));
                window.DoWindowContents(new Rect(0, 0, 520, 560));
                Assert.Equal(recordsBeforeLateReply, memory.DialogueRecords.Count);
            }
            finally
            {
                StorytellerMemory.Instance = null;
                RimMindAPI.Sent.Clear();
                RimMindAPI.Memory.Narrations.Clear();
            }
        }

        [Fact]
        public void StorytellerIncidentRequestFactory_OptsOutOfColonistLivenessGate()
        {
            var envelope = StorytellerIncidentRequestFactory.Create(ScenarioIds.Storyteller, "{}", StorytellerRequestIds.DefaultNpcId);
            Assert.Equal(false, envelope.RequireAliveColonist);
            Assert.Equal(false, envelope.EffectiveRequireAliveColonist);
        }

        [Fact]
        public void StorytellerMemoryPromptFormatter_FormatsRecordsAndHandlesEmptyInputs()
        {
            Assert.Equal(string.Empty, StorytellerMemoryPromptFormatter.FormatRecentIncidents(new List<IncidentHistoryRecord>(), 5));
            Assert.Equal(string.Empty, StorytellerMemoryPromptFormatter.FormatRecentDialogues(new List<DialogueRecord>(), 5));
            Assert.Equal(string.Empty, StorytellerMemoryPromptFormatter.FormatPlayerReactions(new List<PlayerReactionRecord>(), 5));
            Assert.Equal(string.Empty, StorytellerMemoryPromptFormatter.FormatActiveChains(new List<EventChain>()));

            var incidents = new List<IncidentHistoryRecord>
            {
                new() { Label = "Raid", TriggeredTick = 0, MapName = "Base" },
                new() { Label = "Eclipse", TriggeredTick = 60000, MapName = "Base" }
            };
            string incText = StorytellerMemoryPromptFormatter.FormatRecentIncidents(incidents, 1);
            Assert.DoesNotContain("Raid", incText);
            Assert.Contains("Eclipse", incText);
            Assert.Contains("Day 2", incText);

            var dialogues = new List<DialogueRecord>
            {
                new() { role = "user", content = "Hello", tick = 0 },
                new() { role = "assistant", content = "Welcome", tick = 60000 }
            };
            string dlgText = StorytellerMemoryPromptFormatter.FormatRecentDialogues(dialogues, 5);
            Assert.Contains("Player: Hello", dlgText);
            Assert.Contains("Narrator: Welcome", dlgText);

            var reactions = new List<PlayerReactionRecord>
            {
                new() { incidentLabel = "Raid", reactionLabel = "Excited", tick = 60000 }
            };
            string reactText = StorytellerMemoryPromptFormatter.FormatPlayerReactions(reactions, 5);
            Assert.Contains("[Player Reaction Records]", reactText);
            Assert.Contains("Raid", reactText);
            Assert.Contains("Excited", reactText);

            var chains = new List<EventChain>
            {
                new()
                {
                    chainId = "pirate_arc",
                    currentStep = 1,
                    totalSteps = 3,
                    nextHint = "retaliation",
                    steps = new List<ChainStep> { new() { incidentDefName = "RaidEnemy" } }
                }
            };
            string chainText = StorytellerMemoryPromptFormatter.FormatActiveChains(chains);
            Assert.Contains("pirate_arc", chainText);
            Assert.Contains("RaidEnemy", chainText);
            Assert.Contains("retaliation", chainText);
        }
    }
}
