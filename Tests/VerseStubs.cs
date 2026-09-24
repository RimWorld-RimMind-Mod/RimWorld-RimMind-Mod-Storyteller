using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Context;

namespace Verse
{
    public static class Extensions
    {
        public static string Translate(this string key, params object[] args) => key;
    }
    public static class Log { public static void Message(string message) { } }
    public class Pawn { public int thingIDNumber; }
    public class WorldPawns { public List<Pawn> AllPawnsAlive { get; } = new(); }
    public class MapPawns { public List<Pawn> FreeColonists { get; } = new(); }
    public class Map : RimWorld.IIncidentTarget { public MapPawns mapPawns { get; } = new(); }
    public static class Find
    {
        public static WorldPawns WorldPawns { get; } = new();
        public static Map? CurrentMap { get; set; }
        public static RimWorld.Storyteller? Storyteller { get; set; }
        public static RimWorld.FactionManager FactionManager { get; } = new();
    }
    public static class DefDatabase<T> where T : class
    {
        public static Dictionary<string, T> Definitions { get; } = new();
        public static T? GetNamedSilentFail(string name) => Definitions.TryGetValue(name, out T? value) ? value : null;
    }

    public enum LookMode
    {
        Undef,
        Value,
        Deep,
    }

    public static class ScribeRecorder
    {
        public static readonly List<(string Label, object? DefaultValue)> ValueCalls =
            new List<(string, object?)>();
        public static readonly List<(string Label, LookMode Mode)> CollectionCalls =
            new List<(string, LookMode)>();
        public static bool AssignNullCollections { get; set; }

        public static void Reset()
        {
            ValueCalls.Clear();
            CollectionCalls.Clear();
            AssignNullCollections = false;
        }
    }

    public static class Scribe_Values
    {
        public static void Look<T>(ref T value, string label, T defaultValue = default!)
            => ScribeRecorder.ValueCalls.Add((label, defaultValue));
    }

    public static class Scribe_Collections
    {
        public static void Look<T>(
            ref List<T> values,
            string label,
            LookMode lookMode = LookMode.Undef)
        {
            ScribeRecorder.CollectionCalls.Add((label, lookMode));
            if (ScribeRecorder.AssignNullCollections)
                values = null!;
        }
    }
}

namespace RimWorld
{
    public interface IIncidentTarget { }
    public sealed class StorytellerComp { }
    public sealed class Difficulty
    {
        public float threatScale = 1f;
        public bool allowBigThreats = true;
        public bool allowIntroThreats = true;
        public bool allowViolentQuests = true;
    }
    public sealed class Storyteller { public Difficulty difficulty { get; } = new(); }
    public sealed class IncidentCategoryDef { }
    public sealed class IncidentDef
    {
        public string defName = "";
        public IncidentCategoryDef category { get; } = new();
        public IncidentWorker Worker { get; } = new();
    }
    public sealed class IncidentWorker
    {
        public bool CanFire = true;
        public IncidentParms? CheckedParameters { get; private set; }
        public bool CanFireNow(IncidentParms parameters)
        {
            CheckedParameters = parameters;
            return CanFire;
        }
    }
    public sealed class IncidentParms
    {
        public float points = 100f;
        public Faction? faction;
        public RaidStrategyDef? raidStrategy;
    }
    public sealed class FactionDef { public string defName = ""; }
    public sealed class Faction
    {
        public static Faction OfPlayer { get; } = new();
        public FactionDef def { get; } = new();
        public bool IsHostile;
        public bool HostileTo(Faction other) => IsHostile;
    }
    public sealed class FactionManager { public List<Faction> AllFactions { get; } = new(); }
    public sealed class RaidStrategyDef { }
    public static class StorytellerUtility
    {
        public static IncidentParms DefaultParmsNow(IncidentCategoryDef category, IIncidentTarget target) => new();
    }
    public sealed class FiringIncident
    {
        public FiringIncident(IncidentDef def, StorytellerComp source, IncidentParms parms)
        { this.def = def; this.parms = parms; }
        public IncidentDef def { get; }
        public IncidentParms parms { get; }
    }
}

namespace RimMind.Presentation.Api
{
    public static class RimMindAPI
    {
        public static class Context
        {
            public static string ScenarioStoryteller => RimMind.Application.Common.Models.Context.ScenarioIds.Storyteller;
            public static TestContextRegistry ContextKeys { get; } = new();
        }
        public sealed class TestContextRegistry
        {
            public List<ContextProviderDef> Definitions { get; } = new();
            public void Register(ContextProviderDef definition) => Definitions.Add(definition);
        }
        public static class Prompt
        {
            public static string BuildTaskInstruction(string prefix, object? args, params string[] subKeys)
                => string.Join("\n", subKeys.Select(key => prefix + "." + key));
        }
        public static class Memory
        {
            public static List<Narration> Narrations { get; } = new();
            public static IReadOnlyList<Narration> GetRecentNarrations(int count) => Narrations.Take(count).ToList();
        }
        public sealed class Narration { public int Tick; public string Content = ""; }
    }
}

namespace RimMind.Storyteller.Memory
{
    // World-backed data is seeded at the external boundary; real provider delegates format and filter it.
    public sealed class StorytellerMemory
    {
        public static StorytellerMemory? Instance { get; set; }
        public string? CustomSystemPrompt;
        public string? ConsumedReactionsText;
        public float TensionLevel = 0.5f;
        public string Dialogue = "";
        public string IncidentSummary = "";
        public string ActiveChains = "";
        public string GetRecentDialogueSummary(int count) => Dialogue;
        public string GetRecentSummary(int count) => IncidentSummary;
        public string GetActiveChainsSummary() => ActiveChains;
    }
}
