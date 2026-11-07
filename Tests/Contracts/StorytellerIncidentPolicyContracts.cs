using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
                ("only threat categories notify the player", NotificationPolicyRemainsNarrow),
                ("director xml includes vanilla trade visitors and random quest comps", DirectorDefIncludesVanillaEcosystemComps));
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

            string fencedJson = "```json\n" + json + "\n```";
            IncidentResponse? fencedResponse = StorytellerResponseParserPure.ParseResponse(fencedJson);
            Assert.NotNull(fencedResponse);
            Assert.Equal("RaidEnemy", fencedResponse!.defName);
        }

        private static void RecoverableResponsesAreRepaired()
        {
            IncidentResponse? truncated = StorytellerResponseParserPure.ParseResponse(
                "{\"defName\":\"WandererJoin\",\"reason\":\"mercy\"");
            IncidentResponse? trailingComma = StorytellerResponseParserPure.ParseResponse(
                "{\"defName\":\"CargoPodCrash\",\"reason\":\"gift\",}");
            IncidentResponse? fencedTruncated = StorytellerResponseParserPure.ParseResponse(
                "```json\n{\"defName\":\"WandererJoin\",\"reason\":\"mercy\"\n```");
            IncidentResponse? nestedTruncatedInString = StorytellerResponseParserPure.ParseResponse(
                "{\"defName\":\"RaidEnemy\",\"params\":{\"points_multiplier\":1.5},\"reason\":\"test");
            IncidentResponse? fencedNestedTruncated = StorytellerResponseParserPure.ParseResponse(
                "```json\n{\"defName\":\"RaidEnemy\",\"params\":{\"points_multiplier\":1.5}\n```");

            Assert.Equal("WandererJoin", truncated?.defName);
            Assert.Equal("CargoPodCrash", trailingComma?.defName);
            Assert.Equal("WandererJoin", fencedTruncated?.defName);
            Assert.Equal("RaidEnemy", nestedTruncatedInString?.defName);
            Assert.Equal(1.5f, nestedTruncatedInString?.@params?.points_multiplier);
            Assert.Equal("test", nestedTruncatedInString?.reason);
            Assert.Equal("RaidEnemy", fencedNestedTruncated?.defName);
            Assert.Equal(1.5f, fencedNestedTruncated?.@params?.points_multiplier);
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
            var raidDef = new IncidentDef { defName = "RaidEnemy" };
            raidDef.category.defName = "ThreatBig";
            var caravanDef = new IncidentDef { defName = "TraderCaravanArrival" };
            caravanDef.category.defName = "FactionArrival";
            var friendlyRaidDef = new IncidentDef { defName = "RaidFriendly" };
            friendlyRaidDef.category.defName = "ThreatBig";

            var ally = new Faction { IsHostile = false };
            ally.def.defName = "OutlanderCivil";
            var defeatedAlly = new Faction { IsHostile = false, defeated = true };
            defeatedAlly.def.defName = "DefeatedCivil";
            var pirate = new Faction { IsHostile = true };
            pirate.def.defName = "Pirate";

            DefDatabase<IncidentDef>.Definitions[raidDef.defName] = raidDef;
            DefDatabase<IncidentDef>.Definitions[caravanDef.defName] = caravanDef;
            DefDatabase<IncidentDef>.Definitions[friendlyRaidDef.defName] = friendlyRaidDef;
            Find.FactionManager.AllFactions.Add(ally);
            Find.FactionManager.AllFactions.Add(defeatedAlly);
            Find.FactionManager.AllFactions.Add(pirate);
            try
            {
                Assert.True(IncidentSelectionPolicy.RequiresHostileFaction("RaidEnemy", "ThreatBig"));
                Assert.True(IncidentSelectionPolicy.RequiresHostileFaction("ManhunterPack", "ThreatSmall"));
                Assert.False(IncidentSelectionPolicy.RequiresHostileFaction("RaidFriendly", "ThreatBig"));
                Assert.False(IncidentSelectionPolicy.RequiresHostileFaction("TraderCaravanArrival", "FactionArrival"));
                Assert.False(IncidentSelectionPolicy.RequiresHostileFaction("OrbitalTraderArrival", "OrbitalVisitor"));

                var (raidIncident, _) = RimMindIncidentSelector.ParseResponse(
                    "{\"defName\":\"RaidEnemy\",\"params\":{\"faction_hint\":\"OutlanderCivil\",\"raid_strategy_hint\":\"Unknown\"}}",
                    new Map(), new StorytellerComp());
                Assert.NotNull(raidIncident);
                Assert.Null(raidIncident!.parms.faction);
                Assert.Null(raidIncident.parms.raidStrategy);

                var (caravanIncident, _) = RimMindIncidentSelector.ParseResponse(
                    "{\"defName\":\"TraderCaravanArrival\",\"params\":{\"faction_hint\":\"OutlanderCivil\"}}",
                    new Map(), new StorytellerComp());
                Assert.NotNull(caravanIncident);
                Assert.Same(ally, caravanIncident!.parms.faction);

                var (hostileCaravanIncident, _) = RimMindIncidentSelector.ParseResponse(
                    "{\"defName\":\"TraderCaravanArrival\",\"params\":{\"faction_hint\":\"Pirate\"}}",
                    new Map(), new StorytellerComp());
                Assert.NotNull(hostileCaravanIncident);
                Assert.Null(hostileCaravanIncident!.parms.faction);

                var (defeatedCaravanIncident, _) = RimMindIncidentSelector.ParseResponse(
                    "{\"defName\":\"TraderCaravanArrival\",\"params\":{\"faction_hint\":\"DefeatedCivil\"}}",
                    new Map(), new StorytellerComp());
                Assert.NotNull(defeatedCaravanIncident);
                Assert.Null(defeatedCaravanIncident!.parms.faction);

                var (friendlyRaidIncident, _) = RimMindIncidentSelector.ParseResponse(
                    "{\"defName\":\"RaidFriendly\",\"params\":{\"faction_hint\":\"OutlanderCivil\"}}",
                    new Map(), new StorytellerComp());
                Assert.NotNull(friendlyRaidIncident);
                Assert.Same(ally, friendlyRaidIncident!.parms.faction);
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

            var activeMap = new Map();
            var mapPawn = new Pawn { thingIDNumber = 42 };
            var worldOnlyPawn = new Pawn { thingIDNumber = 99 };
            activeMap.mapPawns.FreeColonists.Add(mapPawn);
            Find.CurrentMap = activeMap;
            Find.WorldPawns.AllPawnsAlive.Add(worldOnlyPawn);
            try
            {
                Assert.Null(PawnLookup.FindPawnById(0));
                Assert.Same(mapPawn, PawnLookup.FindPawnById(42));
                Assert.Same(mapPawn, PawnLookup.FindById("Pawn_42"));
                Assert.Null(PawnLookup.FindPawnById(99));
                Assert.Null(PawnLookup.FindById("Pawn_99"));
            }
            finally
            {
                Find.CurrentMap = null;
                Find.WorldPawns.AllPawnsAlive.Clear();
            }
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

        private static void DirectorDefIncludesVanillaEcosystemComps()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Defs", "StorytellerDefs", "RimMindDirector.xml")))
            {
                string candidate = Path.Combine(dir.FullName, "RimMind-Storyteller", "Defs", "StorytellerDefs", "RimMindDirector.xml");
                if (File.Exists(candidate))
                {
                    dir = new DirectoryInfo(Path.Combine(dir.FullName, "RimMind-Storyteller"));
                    break;
                }
                dir = dir.Parent;
            }

            Assert.NotNull(dir);
            string xmlPath = Path.Combine(dir!.FullName, "Defs", "StorytellerDefs", "RimMindDirector.xml");
            XDocument doc = XDocument.Load(xmlPath);
            var comps = doc.Root!
                .Elements("StorytellerDef")
                .Single(e => e.Element("defName")?.Value == "RimMindDirector")
                .Element("comps")!
                .Elements("li")
                .ToList();

            var incidents = comps
                .Select(c => c.Element("incident")?.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .ToHashSet(StringComparer.Ordinal);
            var classes = comps
                .Select(c => c.Attribute("Class")?.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .ToHashSet(StringComparer.Ordinal);

            Assert.Contains("TraderCaravanArrival", incidents);
            Assert.Contains("VisitorGroup", incidents);
            Assert.Contains("TravelerGroup", incidents);
            Assert.Contains("RaidFriendly", incidents);
            Assert.Contains("OrbitalTraderArrival", incidents);
            Assert.Contains("StorytellerCompProperties_RandomQuest", classes);
            Assert.Contains("StorytellerCompProperties_ShipChunkDrop", classes);
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
