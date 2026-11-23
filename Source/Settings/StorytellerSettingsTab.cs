using System;
using RimMind.Presentation.Api;
using RimMind.Presentation.UI;
using RimMind.Storyteller.Memory;
using UnityEngine;
using Verse;

namespace RimMind.Storyteller.Settings
{
    public static class StorytellerSettingsTab
    {
        private static readonly FallbackMode[] AllFallbackModes =
            (FallbackMode[])Enum.GetValues(typeof(FallbackMode));

        private static Vector2 _scrollPos = Vector2.zero;
        private static float _measuredHeight = 760f;
        private static string _promptText = string.Empty;
        private static StorytellerMemory? _loadedMemory;

        public static void Draw(Rect inRect)
        {
            SyncPromptWithActiveMemory();
            var settings = RimMindStorytellerMod.Settings;
            if (settings == null) return;

            SettingsUIDrawer.DrawScrollableSettingsPage(
                inRect,
                ref _scrollPos,
                ref _measuredHeight,
                listing =>
                {
                    SettingsUIDrawer.DrawSectionHeader(listing, RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.TriggerSources", "Trigger Sources"));
                    listing.CheckboxLabeled(
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.EnableIntervalTrigger", "Interval Trigger"),
                        ref settings.enableIntervalTrigger,
                        RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.UI.EnableIntervalTrigger.Desc",
                            "Request AI to select events at random intervals based on MTB (Mean Time Between). When disabled, the storyteller won't auto-trigger AI requests, but manual trigger still works."));
                    listing.CheckboxLabeled(
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.EnableEventNotification", "Event Notification"),
                        ref settings.enableEventNotification,
                        RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.UI.EnableEventNotification.Desc",
                            "When the storyteller selects a threat event, send a notification to the request overlay. Players can choose an emotional reaction, which affects narrative tension and future event selection."));

                    SettingsUIDrawer.DrawSectionHeader(listing, RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.Section.Fallback", "Fallback Mode"));
                    listing.LabelWithTooltip(
                        RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.UI.FallbackModeLabel",
                            "Fallback Mode (behavior when AI unavailable): {0}",
                            settings.fallbackMode.ToString()),
                        RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.UI.FallbackModeDesc",
                            "When AI request is cooling down, fails, or the Director is unhealthy (no recent success or recent failure), use vanilla storyteller logic to select events"));
                    if (Widgets.ButtonText(
                            listing.GetRect(30f),
                            RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.SwitchFallback", "Switch Fallback Mode")))
                    {
                        int idx = Array.IndexOf(AllFallbackModes, settings.fallbackMode);
                        settings.fallbackMode = AllFallbackModes[(idx + 1) % AllFallbackModes.Length];
                        GUI.changed = true;
                    }

                    SettingsUIDrawer.DrawCustomPromptSection(
                        listing,
                        RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.UI.StylePromptLabel",
                            "Storyteller Style Prompt (prepended to system prompt, leave empty to skip):"),
                        ref _promptText,
                        72f);

                    SettingsUIDrawer.DrawSectionHeader(listing, RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.Section.Request", "Request Settings"));
                    string mtbTip = RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.MTBDays.Desc",
                        "Average interval in game days for the AI storyteller to trigger events, using MTB random trigger mechanism similar to vanilla storytellers. Lower values mean more frequent events.");
                    listing.LabelWithTooltip(
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.MTBDays", "Average event interval: {0} game days", $"{settings.mtbDays:F1}"),
                        mtbTip);
                    settings.mtbDays = listing.SliderWithTooltip(settings.mtbDays, 0.5f, 10f, mtbTip);
                    settings.mtbDays = (float)Math.Round(settings.mtbDays, 1);

                    string reqExpireTip = RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.RequestExpire.Desc",
                        "Storyteller AI requests that don't receive a response within this time are automatically cancelled.");
                    listing.LabelWithTooltip(
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.RequestExpire", "Request expiry: {0} game days", $"{settings.requestExpireTicks / 60000f:F2}"),
                        reqExpireTip);
                    settings.requestExpireTicks = (int)listing.SliderWithTooltip(settings.requestExpireTicks, 3600f, 120000f, reqExpireTip);
                    settings.requestExpireTicks = (settings.requestExpireTicks / 1500) * 1500;

                    SettingsUIDrawer.DrawSectionHeader(listing, RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.Section.Debug", "Debug"));
                    listing.CheckboxLabeled(
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.DebugLogging", "Verbose Logging"),
                        ref settings.debugLogging,
                        RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.UI.DebugLogging.Desc",
                            "Output detailed AI request and response info to Player.log for troubleshooting."));

                    SettingsUIDrawer.DrawSectionHeader(listing, RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.Section.Memory", "Memory Settings"));
                    string eventRecTip = RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.MaxEventRecords.Desc",
                        "Number of recent event records kept for AI reference when selecting events.");
                    listing.LabelWithTooltip(
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.MaxEventRecords", "Max event records: {0}", settings.maxEventRecords),
                        eventRecTip);
                    settings.maxEventRecords = (int)listing.SliderWithTooltip(settings.maxEventRecords, 10f, 100f, eventRecTip);

                    string dialRecTip = RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.MaxDialogueRecords.Desc",
                        "Number of recent dialogue records kept for AI reference when responding to players.");
                    listing.LabelWithTooltip(
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.MaxDialogueRecords", "Max dialogue records: {0}", settings.maxDialogueRecords),
                        dialRecTip);
                    settings.maxDialogueRecords = (int)listing.SliderWithTooltip(settings.maxDialogueRecords, 5f, 60f, dialRecTip);

                    string reactionTip = RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.MaxPlayerReactions.Desc",
                        "Maximum number of player emotional reaction records kept.");
                    listing.LabelWithTooltip(
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.MaxPlayerReactions", "Max player reactions: {0}", settings.maxPlayerReactions),
                        reactionTip);
                    settings.maxPlayerReactions = (int)listing.SliderWithTooltip(settings.maxPlayerReactions, 5f, 50f, reactionTip);

                    string chainExpireTip = RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.ChainExpireDays.Desc",
                        "Event chains not advanced within this time are automatically removed.");
                    listing.LabelWithTooltip(
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.ChainExpireDays", "Chain expiry: {0} days", $"{settings.chainExpireDays:F1}"),
                        chainExpireTip);
                    settings.chainExpireDays = listing.SliderWithTooltip(settings.chainExpireDays, 3f, 30f, chainExpireTip);
                    settings.chainExpireDays = (float)Math.Round(settings.chainExpireDays * 2f) / 2f;

                    string tensionDecayTip = RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.TensionDecayPerDay.Desc",
                        "Amount of narrative tension that decays per game day. Higher values mean tension drops faster.");
                    listing.LabelWithTooltip(
                        RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.TensionDecayPerDay", "Tension decay: {0}/day", $"{settings.tensionDecayPerDay:F3}"),
                        tensionDecayTip);
                    settings.tensionDecayPerDay = listing.SliderWithTooltip(settings.tensionDecayPerDay, 0.01f, 0.10f, tensionDecayTip);
                    settings.tensionDecayPerDay = (float)Math.Round(settings.tensionDecayPerDay * 200f) / 200f;

                    SettingsUIDrawer.DrawSectionHeader(listing, RimMindAPI.Text.TrOr(
                        "RimMind.Storyteller.UI.MemoryTitle", "Storyteller Memory (read-only):"));
                    var memory = StorytellerMemory.Instance;
                    if (memory == null)
                    {
                        listing.Label(RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.MemoryNeedLoad", "  Load a game to view"));
                    }
                    else
                    {
                        listing.Label(RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.UI.MemoryRecordCount",
                            "  Records: {0}/{1}",
                            memory.Records.Count,
                            settings.maxEventRecords));
                        listing.Label(RimMindAPI.Text.TrOr(
                            "RimMind.Storyteller.UI.DialogueRecordCount",
                            "  Dialogue records: {0}",
                            memory.DialogueRecords.Count));
                        for (int i = memory.Records.Count - 1; i >= 0 && i >= memory.Records.Count - 10; i--)
                        {
                            var r = memory.Records[i];
                            int day = TensionMath.TicksToDay(r.TriggeredTick);
                            listing.Label(RimMindAPI.Text.TrOr(
                                "RimMind.Storyteller.UI.MemoryDayEntry",
                                "  Day {0}: {1} ({2})",
                                day,
                                r.Label,
                                r.MapName));
                        }
                        if (Widgets.ButtonText(
                                listing.GetRect(30f),
                                RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.ClearMemory", "Clear Memory")))
                        {
                            memory.ClearRecords();
                            memory.ClearDialogueRecords();
                        }
                    }
                },
                onReset: () =>
                {
                    settings.enableIntervalTrigger = true;
                    settings.fallbackMode = FallbackMode.Cassandra;
                    settings.mtbDays = 1.5f;
                    settings.debugLogging = false;
                    settings.requestExpireTicks = 30000;
                    settings.maxEventRecords = 50;
                    settings.maxDialogueRecords = 30;
                    settings.enableEventNotification = true;
                    settings.maxPlayerReactions = 20;
                    settings.chainExpireDays = 10.0f;
                    settings.tensionDecayPerDay = 0.03f;
                    _promptText = string.Empty;
                    SavePrompt();
                },
                onDirtySave: () =>
                {
                    SavePrompt();
                    settings.Write();
                });
        }

        private static void SyncPromptWithActiveMemory()
        {
            var memory = StorytellerMemory.Instance;
            if (ReferenceEquals(memory, _loadedMemory))
                return;

            _loadedMemory = memory;
            _promptText = memory?.CustomSystemPrompt ?? string.Empty;
        }

        private static void SavePrompt()
        {
            var memory = StorytellerMemory.Instance;
            if (memory != null)
            {
                memory.CustomSystemPrompt = _promptText;
            }
        }
    }
}
