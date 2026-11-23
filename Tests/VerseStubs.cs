using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Json;
using RimMind.Application.Features.Prompt;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace Verse
{
    public static class Extensions
    {
        public static readonly HashSet<string> UntranslatableKeys = new HashSet<string>();
        public static string Translate(this string key, params object[] args) => key;
        public static bool CanTranslate(this string key) => false;
        public static string TranslateSimple(this string key) => key;
    }
    public static class UnityData { public static bool IsInMainThread = true; }
    public static class Log { public static void Message(string message) { } }
    public class Pawn { public int thingIDNumber; public string ThingID => $"Pawn_{thingIDNumber}"; }
    public class WorldPawns { public List<Pawn> AllPawnsAlive { get; } = new(); }
    public class MapPawns { public List<Pawn> FreeColonists { get; } = new(); }
    public class Map : RimWorld.IIncidentTarget { public MapPawns mapPawns { get; } = new(); }
    public class TickManager { public int TicksGame; }
    public class Window
    {
        public bool doCloseButton, doCloseX, closeOnAccept, forcePause, closeOnClickedOutside,
            absorbInputAroundWindow, draggable;
        public virtual UnityEngine.Vector2 InitialSize => default;
        public virtual void PostClose() { }
        public virtual void DoWindowContents(UnityEngine.Rect rect) { }
        public void Close() => PostClose();
    }
    public static class Widgets
    {
        public static string? NextText;
        public static bool ClickSend;
        public static readonly List<string> Labels = new();
        public static void Label(UnityEngine.Rect rect, string text) => Labels.Add(text);
        public static string TextField(UnityEngine.Rect rect, string text)
        {
            string result = NextText ?? text;
            NextText = null;
            return result;
        }
        public static bool ButtonText(UnityEngine.Rect rect, string text)
        {
            bool click = ClickSend;
            ClickSend = false;
            return click;
        }
    }
    public static class Find
    {
        public static TickManager TickManager { get; } = new();
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
    public sealed class DifficultyDef
    {
        public string LabelCap = string.Empty;
    }
    public sealed class Storyteller
    {
        public Difficulty difficulty { get; } = new();
        public DifficultyDef? difficultyDef { get; set; }
    }
    public sealed class IncidentCategoryDef { public string defName = "ThreatBig"; }
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
        public static Faction OfPlayer { get; } = new() { IsPlayer = true };
        public FactionDef def { get; } = new();
        public bool IsPlayer;
        public bool IsHostile;
        public bool defeated;
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

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => default;
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
    }
    public struct Color
    {
        public Color(float r, float g, float b, float a = 1) { }
        public static Color white => default;
    }
}

namespace RimMind.Presentation.UI
{
    public readonly struct ChatWindowMessage
    {
        public ChatWindowMessage(string role, string content, string? senderPrefix = null)
        {
            Role = role ?? string.Empty;
            Content = content ?? string.Empty;
            SenderPrefix = senderPrefix;
        }
        public string Role { get; }
        public string Content { get; }
        public string? SenderPrefix { get; }
    }

    public abstract class RimMindChatWindowBase : Verse.Window
    {
        private readonly List<ChatWindowMessage> _messages = new();
        private readonly ConcurrentQueue<Action> _mainThreadUpdates = new();
        private readonly CancellationTokenSource _lifetime = new();
        private string _inputText = string.Empty;
        private bool _isWaiting;
        private bool _closed;

        protected RimMindChatWindowBase()
        {
            doCloseButton = false;
            doCloseX = true;
            closeOnAccept = false;
            forcePause = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
            draggable = true;
        }

        public override UnityEngine.Vector2 InitialSize => new UnityEngine.Vector2(480f, 540f);
        protected CancellationToken LifetimeToken => _lifetime.Token;
        protected bool IsClosed => _closed;
        protected bool IsWaiting => _isWaiting;
        public IReadOnlyList<ChatWindowMessage> Messages => _messages;

        protected virtual UnityEngine.Color HeaderColor => new UnityEngine.Color(0.8f, 0.95f, 1f);
        protected abstract string GetTitle();
        protected abstract string GetWaitingText();
        protected abstract string GetSendButtonLabel();
        protected abstract string GetPrefixForRole(string role);
        protected abstract void OnSubmitUserMessage(string trimmedInput);
        protected virtual bool ValidateCanStayOpen() => true;
        protected virtual void OnMessageAppended(ChatWindowMessage message) { }

        protected void AppendMessage(string role, string content, string? senderPrefix = null, bool notifyHook = true)
        {
            if (string.IsNullOrEmpty(content)) return;
            var msg = new ChatWindowMessage(role, content, senderPrefix);
            _messages.Add(msg);
            if (notifyHook) OnMessageAppended(msg);
        }

        protected void PostReply(string role, string content, bool clearWaiting = true, string? senderPrefix = null)
        {
            if (_closed) return;
            _mainThreadUpdates.Enqueue(() =>
            {
                if (_closed) return;
                if (clearWaiting) _isWaiting = false;
                if (!string.IsNullOrEmpty(content)) AppendMessage(role, content, senderPrefix);
            });
        }

        protected void SetWaiting(bool waiting) => _isWaiting = waiting;

        public override void PostClose()
        {
            if (!_closed)
            {
                _closed = true;
                _isWaiting = false;
                try { _lifetime.Cancel(); } catch (ObjectDisposedException) { }
                try { _lifetime.Dispose(); } catch (ObjectDisposedException) { }
            }
            base.PostClose();
        }

        public override void DoWindowContents(UnityEngine.Rect inRect)
        {
            if (!ValidateCanStayOpen())
            {
                Close();
                return;
            }
            while (_mainThreadUpdates.TryDequeue(out var update))
                update?.Invoke();

            Verse.Widgets.Label(inRect, GetTitle());
            if (_isWaiting) Verse.Widgets.Label(inRect, GetWaitingText());
            _inputText = Verse.Widgets.TextField(inRect, _inputText ?? string.Empty);
            bool sendClicked = Verse.Widgets.ButtonText(inRect, GetSendButtonLabel());
            if (sendClicked && !_isWaiting && !string.IsNullOrWhiteSpace(_inputText))
            {
                string trimmed = _inputText.Trim();
                _inputText = string.Empty;
                OnSubmitUserMessage(trimmed);
            }
        }
    }
}

namespace RimMind.Presentation.Api
{
    public static class RimMindAPI
    {
        public static bool Configured = true;
        public static readonly List<(LlmRequestEnvelope Envelope, Action<Result<LlmResponse, RimMindError>> Complete)> Sent = new();

        public static class Settings
        {
            public static bool IsConfigured() => Configured;
        }

        public static class Request
        {
            public static void Send(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> onComplete)
                => Sent.Add((envelope, onComplete));
        }

        public static class Text
        {
            public static string TrOr(string key, string fallback, params object[] args)
                => RimMindText.TrOr(key, fallback, args);
            public static string Tr(string key, params object[] args)
                => RimMindText.Tr(key, args);
            public static string WrapForMote(string? text, int maxCharsPerLine = MoteTextFormatter.DefaultMaxCharsPerLine)
                => MoteTextFormatter.WrapForMote(text, maxCharsPerLine);
        }

        public static class Json
        {
            public static string StripThinkingAndCodeFences(string? raw)
                => LlmPayloadParser.StripThinkingAndCodeFences(raw);
            public static string? ExtractJsonPayload(string? raw)
                => LlmPayloadParser.ExtractJsonPayload(raw);
            public static bool TryParseJObjectWithRepair(string? raw, out JObject? obj)
                => LlmPayloadParser.TryParseJObjectWithRepair(raw, out obj);
            public static bool TryDeserializeWithRepair<T>(string? raw, out T? value) where T : class
                => LlmPayloadParser.TryDeserializeWithRepair<T>(raw, out value);
            public static IReadOnlyList<ParsedPseudoFunctionCall> ParsePseudoFunctionCalls(string? raw)
                => LlmPayloadParser.ParsePseudoFunctionCalls(raw);
            public static string? TryRepairJson(string input)
                => JsonRepairer.TryRepair(input);
            public static string? TryRepairTruncatedJson(string input)
                => JsonRepairer.TryRepairTruncatedJson(input);
        }

        public static class MainThreadSnapshot
        {
            public static RimMind.Application.Common.Threading.MainThreadReader Reader { get; set; } =
                new RimMind.Application.Common.Threading.MainThreadReader(a => a(), () => true);

            public static System.Threading.Tasks.Task<T> GetAsync<T>(
                Func<T> readOnMainThread,
                T fallback = default!,
                TimeSpan? timeout = null,
                System.Threading.CancellationToken ct = default)
                => Reader.ReadAsync(
                    readOnMainThread,
                    fallback,
                    timeout ?? RimMind.Application.Common.Threading.MainThreadReader.DefaultTimeout,
                    ct);
        }

        public static class Context
        {
            public static string ScenarioStoryteller => RimMind.Application.Common.Models.Context.ScenarioIds.Storyteller;
            public static TestContextRegistry ContextKeys { get; } = new();
            public static Dictionary<string, string> RegisteredSchemas { get; } = new(StringComparer.Ordinal);
            public static void RegisterScenario(ScenarioDescriptor descriptor)
                => ScenarioRegistry.Register(descriptor);
            public static void RegisterSchema(string key, string schema)
                => RegisteredSchemas[key] = schema;
            public static string? FindSchema(string key)
                => RegisteredSchemas.TryGetValue(key, out string? schema) ? schema : null;
        }
        public sealed class TestContextRegistry
        {
            public List<ContextProviderDef> Definitions { get; } = new();
            public void Register(ContextProviderDef definition)
            {
                Definitions.RemoveAll(d => d.Key == definition.Key);
                Definitions.Add(definition);
            }
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
            public static bool AddNarratorMemory(string content, int tick, float importance = 0.3f)
            {
                Narrations.Add(new Narration { Tick = tick, Content = content });
                return true;
            }
        }
        public sealed class Narration { public int Tick; public string Content = ""; }
        public static class Bus
        {
            public static void RegisterEventType<T>(string name) where T : RimMind.Domain.Events.AgentBusEvent
                => RimMind.Application.Features.AgentBus.AgentBusImpl.RegisterKnownEventType(name, typeof(T));
            public static void PublishOnMainThread<T>(T evt) where T : RimMind.Domain.Events.AgentBusEvent { }
        }
    }

    public static class RimMindPawnLookup
    {
        public static Verse.Pawn? FindPawnByNumber(int thingIdNumber)
        {
            if (thingIdNumber <= 0) return null;
            var mapColonists = Verse.Find.CurrentMap?.mapPawns?.FreeColonists;
            if (mapColonists != null)
            {
                var found = mapColonists.FirstOrDefault(p => p.thingIDNumber == thingIdNumber);
                if (found != null) return found;
            }
            return null;
        }

        public static Verse.Pawn? FindPawnById(string? pawnId)
        {
            if (string.IsNullOrEmpty(pawnId)) return null;
            var mapColonists = Verse.Find.CurrentMap?.mapPawns?.FreeColonists;
            if (mapColonists != null)
            {
                var found = mapColonists.FirstOrDefault(p => p.ThingID == pawnId);
                if (found != null) return found;
            }
            return null;
        }
    }
}

namespace RimMind.Storyteller.Memory
{
    public class IncidentHistoryRecord
    {
        public string Label = string.Empty;
        public int TriggeredTick;
        public string MapName = string.Empty;
    }

    public class DialogueRecord
    {
        public string role = string.Empty;
        public string content = string.Empty;
        public int tick;
    }

    public class PlayerReactionRecord
    {
        public string incidentDefName = string.Empty;
        public string incidentLabel = string.Empty;
        public string reaction = string.Empty;
        public string reactionLabel = string.Empty;
        public int tick;
    }

    public class ChainStep
    {
        public string incidentDefName = string.Empty;
        public int triggeredTick;
        public bool completed;
    }

    public class EventChain
    {
        public string chainId = string.Empty;
        public List<ChainStep> steps = new();
        public int currentStep;
        public int totalSteps;
        public string nextHint = string.Empty;
    }

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
        public List<DialogueRecord> DialogueRecords { get; } = new();
        public void RecordDialogue(string role, string content, int tick)
            => DialogueRecords.Add(new DialogueRecord { role = role, content = content, tick = tick });
        public string GetRecentDialogueSummary(int count) => Dialogue;
        public string GetRecentSummary(int count) => IncidentSummary;
        public string GetActiveChainsSummary() => ActiveChains;
    }
}
