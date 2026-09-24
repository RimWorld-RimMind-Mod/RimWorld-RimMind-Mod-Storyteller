using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Presentation.Api;
using RimMind.Storyteller.Memory;
using Xunit;

namespace RimMind.Storyteller.Tests.Contracts
{
    public sealed class StorytellerWorldProviderContracts
    {
        public static IEnumerable<object[]> WorldRequests()
        {
            var expectedContent = new Dictionary<string, string>
            {
                ["storyteller_task"] = "TaskInstruction.Role",
                ["storyteller_context"] = "world incident summary",
                ["storyteller_dialogue"] = "world dialogue",
                ["storyteller_reactions"] = "player reaction",
                ["storyteller_recent_incidents"] = "narrator memory"
            };
            foreach (string npcId in new[] { "NPC-map-7", "NPC-storyteller" })
            foreach (var item in expectedContent)
                yield return new object[] { npcId, item.Key, item.Value };
        }

        [Theory]
        [MemberData(nameof(WorldRequests))]
        public async Task World_request_includes_registered_context_without_a_pawn(string npcId, string key, string expected)
        {
            RimMindAPI.Context.ContextKeys.Definitions.Clear();
            RimMindAPI.Memory.Narrations.Clear();
            StorytellerMemory.Instance = new StorytellerMemory
            {
                Dialogue = "world dialogue",
                IncidentSummary = "world incident summary",
                ActiveChains = "active event chain",
                ConsumedReactionsText = "player reaction"
            };
            RimMindAPI.Memory.Narrations.Add(new RimMindAPI.Narration { Tick = 60000, Content = "narrator memory" });
            try
            {
                StorytellerContextProviderRegistrar.RegisterAll();
                var provider = RimMindAPI.Context.ContextKeys.Definitions.Single(definition => definition.Key == key);
                var context = new ProviderContext(ScenarioIds.Storyteller, "world-provider") { NpcId = npcId, PawnId = 0 };

                Assert.Contains(expected, await provider.Provider(context, CancellationToken.None));
                Assert.Null(await provider.Provider(context with { Scenario = ScenarioIds.Dialogue }, CancellationToken.None));
                Assert.Null(await provider.Provider(context with { Scenario = ScenarioIds.Decision, PawnId = 7 }, CancellationToken.None));
            }
            finally
            {
                StorytellerMemory.Instance = null;
                RimMindAPI.Context.ContextKeys.Definitions.Clear();
                RimMindAPI.Memory.Narrations.Clear();
            }
        }
    }
}
