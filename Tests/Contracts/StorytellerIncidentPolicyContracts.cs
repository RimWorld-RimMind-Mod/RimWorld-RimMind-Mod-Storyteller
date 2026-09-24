using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Storyteller.Extensions;
using RimMind.Storyteller.Memory;
using RimMind.Testing;
using RimWorld;
using Verse;
using Xunit;

namespace RimMind.Storyteller.Tests.Contracts
{
    public sealed class StorytellerIncidentPolicyContracts
    {
        [Fact]
        public void Stable_incident_policy_boundaries()
        {
            ContractCaseRunner.Run(
                ("valid incident responses retain parameters and chain metadata", ValidResponsesRetainMetadata),
                ("truncated and trailing comma responses are repaired", RecoverableResponsesAreRepaired),
                ("empty malformed and missing def names become no incident", InvalidResponsesBecomeNoIncident),
                ("pawn lookup rejects invalid ids before world then map lookup", PawnLookupOrderRemainsStable),
                ("only threat categories notify the player", NotificationPolicyRemainsNarrow));
        }

        private static void ValidResponsesRetainMetadata()
        {
            const string json =
                "{\"defName\":\"RaidEnemy\",\"reason\":\"pressure\"," +
                "\"params\":{\"points_multiplier\":1.25,\"faction_hint\":\"Pirate\",\"raid_strategy_hint\":\"ImmediateAttack\"}," +
                "\"chain\":{\"chain_id\":\"arc-7\",\"chain_step\":2,\"chain_total\":4,\"next_hint\":\"reprisal\"}}";

            IncidentResponse? response = StorytellerResponseParserPure.ParseResponse(json);

            Assert.NotNull(response);
            Assert.Equal("RaidEnemy", response!.defName);
            Assert.Equal(1.25f, response.@params!.points_multiplier);
            Assert.Equal("Pirate", response.@params.faction_hint);
            Assert.Equal("ImmediateAttack", response.@params.raid_strategy_hint);
            Assert.Equal("arc-7", response.chain!.chain_id);
            Assert.Equal(2, response.chain.chain_step);
            Assert.Equal(4, response.chain.chain_total);
            Assert.Equal("reprisal", response.chain.next_hint);
        }

        private static void RecoverableResponsesAreRepaired()
        {
            IncidentResponse? truncated = StorytellerResponseParserPure.ParseResponse(
                "{\"defName\":\"WandererJoin\",\"reason\":\"mercy\"");
            IncidentResponse? trailingComma = StorytellerResponseParserPure.ParseResponse(
                "{\"defName\":\"CargoPodCrash\",\"reason\":\"gift\",}");

            Assert.Equal("WandererJoin", truncated?.defName);
            Assert.Equal("CargoPodCrash", trailingComma?.defName);
        }

        private static void InvalidResponsesBecomeNoIncident()
        {
            Assert.Null(StorytellerResponseParserPure.ParseResponse(string.Empty));
            Assert.Null(StorytellerResponseParserPure.ParseResponse("not-json"));
            Assert.Null(StorytellerResponseParserPure.ParseResponse("{\"reason\":\"missing def\"}"));
            Assert.Null(StorytellerResponseParserPure.ParseResponse("{\"defName\":\"\"}"));
        }

        [Theory]
        [InlineData("not-json", true, false, false)]
        [InlineData("{\"defName\":\"Unknown\"}", true, false, true)]
        [InlineData("{\"defName\":\"RaidEnemy\"}", false, false, true)]
        [InlineData("{\"defName\":\"RaidEnemy\"}", true, true, true)]
        public void Incident_selection_retains_response_but_never_fires_invalid_or_unavailable_events(
            string json, bool canFire, bool expectedIncident, bool expectedResponse)
        {
            var definition = new IncidentDef { defName = "RaidEnemy" };
            definition.Worker.CanFire = canFire;
            DefDatabase<IncidentDef>.Definitions[definition.defName] = definition;
            try
            {
                var (incident, response) = RimMindIncidentSelector.ParseResponse(json, new Map(), new StorytellerComp());
                Assert.Equal(expectedIncident, incident != null);
                Assert.Equal(expectedResponse, response != null);
                if (incident != null)
                {
                    Assert.Same(definition, incident.def);
                    Assert.Same(incident.parms, definition.Worker.CheckedParameters);
                }
            }
            finally { DefDatabase<IncidentDef>.Definitions.Clear(); }
        }

        [Theory]
        [InlineData(-1f, 30f)]
        [InlineData(8f, 200f)]
        public void Incident_parameters_are_bounded_before_worker_validation(float multiplier, float expectedPoints)
        {
            var definition = new IncidentDef { defName = "RaidEnemy" };
            var faction = new Faction { IsHostile = true };
            faction.def.defName = "Pirate";
            var strategy = new RaidStrategyDef();
            DefDatabase<IncidentDef>.Definitions[definition.defName] = definition;
            DefDatabase<RaidStrategyDef>.Definitions["ImmediateAttack"] = strategy;
            Find.FactionManager.AllFactions.Add(faction);
            try
            {
                var (incident, _) = RimMindIncidentSelector.ParseResponse(
                    Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        defName = "RaidEnemy",
                        @params = new { points_multiplier = multiplier, faction_hint = "Pirate", raid_strategy_hint = "ImmediateAttack" }
                    }), new Map(), new StorytellerComp());
                Assert.NotNull(incident);
                Assert.Equal(expectedPoints, incident!.parms.points, 3);
                Assert.Same(faction, incident.parms.faction);
                Assert.Same(strategy, incident.parms.raidStrategy);
                Assert.Same(incident.parms, definition.Worker.CheckedParameters);
            }
            finally
            {
                DefDatabase<IncidentDef>.Definitions.Clear();
                DefDatabase<RaidStrategyDef>.Definitions.Clear();
                Find.FactionManager.AllFactions.Clear();
            }
        }

        [Fact]
        public void Nonhostile_faction_and_unknown_strategy_hints_do_not_override_defaults()
        {
            var definition = new IncidentDef { defName = "RaidEnemy" };
            var ally = new Faction { IsHostile = false };
            ally.def.defName = "Ally";
            DefDatabase<IncidentDef>.Definitions[definition.defName] = definition;
            Find.FactionManager.AllFactions.Add(ally);
            try
            {
                var (incident, _) = RimMindIncidentSelector.ParseResponse(
                    "{\"defName\":\"RaidEnemy\",\"params\":{\"faction_hint\":\"Ally\",\"raid_strategy_hint\":\"Unknown\"}}",
                    new Map(), new StorytellerComp());
                Assert.NotNull(incident);
                Assert.Null(incident!.parms.faction);
                Assert.Null(incident.parms.raidStrategy);
            }
            finally
            {
                DefDatabase<IncidentDef>.Definitions.Clear();
                Find.FactionManager.AllFactions.Clear();
            }
        }

        private static void PawnLookupOrderRemainsStable()
        {
            int enumerations = 0;
            IEnumerable<TestPawn> Track(params TestPawn[] pawns)
            {
                enumerations++;
                foreach (TestPawn pawn in pawns)
                    yield return pawn;
            }

            Assert.Null(PawnLookupCore.FindById(
                0,
                () => Track(new TestPawn(1, "world")),
                () => Track(new TestPawn(1, "map")),
                pawn => pawn.Id));
            Assert.Equal(0, enumerations);

            var world = new[] { new TestPawn(7, "world") };
            var map = new[] { new TestPawn(7, "map"), new TestPawn(8, "fallback") };
            int mapFactoryCalls = 0;
            Assert.Equal(
                "world",
                PawnLookupCore.FindById(
                    7,
                    () => world,
                    () =>
                    {
                        mapFactoryCalls++;
                        return map;
                    },
                    pawn => pawn.Id)!.Source);
            Assert.Equal(0, mapFactoryCalls);
            Assert.Equal(
                "fallback",
                PawnLookupCore.FindById(
                    8,
                    () => world,
                    () =>
                    {
                        mapFactoryCalls++;
                        return map;
                    },
                    pawn => pawn.Id)!.Source);
            Assert.Equal(1, mapFactoryCalls);
        }

        private static void NotificationPolicyRemainsNarrow()
        {
            Assert.True(IncidentSelectionPolicy.ShouldNotify(
                true, StorytellerIncidentKind.ThreatBig));
            Assert.True(IncidentSelectionPolicy.ShouldNotify(
                true, StorytellerIncidentKind.ThreatSmall));
            Assert.False(IncidentSelectionPolicy.ShouldNotify(
                true, StorytellerIncidentKind.Other));
            Assert.False(IncidentSelectionPolicy.ShouldNotify(
                false, StorytellerIncidentKind.ThreatBig));
        }

        private sealed class TestPawn
        {
            public TestPawn(int id, string source)
            {
                Id = id;
                Source = source;
            }

            public int Id { get; }
            public string Source { get; }
        }
    }
}
