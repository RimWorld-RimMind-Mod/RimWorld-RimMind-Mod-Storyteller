using System.Collections.Generic;
using RimMind.Presentation.UI;
using RimMind.Storyteller.Memory;
using UnityEngine;
using Verse;

namespace RimMind.Storyteller.Settings
{
    public static class StorytellerSettingsTab
    {
        private static Vector2 _scrollPos = Vector2.zero;
        private static float _measuredHeight = 760f;
        private static string _promptText = string.Empty;
        private static StorytellerMemory? _loadedMemory;

        public static void Draw(Rect inRect)
        {
            SyncPromptWithActiveMemory();
            var settings = RimMindStorytellerMod.Settings;

            SettingsUIDrawer.DrawScrollableSettingsPage(
                inRect,
                ref _scrollPos,
                ref _measuredHeight,
                listing =>
                {
                    SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Storyteller.UI.TriggerSources".Translate());
                    listing.CheckboxLabeled("RimMind.Storyteller.UI.EnableIntervalTrigger".Translate(), ref settings.enableIntervalTrigger,
                        "RimMind.Storyteller.UI.EnableIntervalTrigger.Desc".Translate());
                    listing.CheckboxLabeled("RimMind.Storyteller.UI.EnableEventNotification".Translate(), ref settings.enableEventNotification,
                        "RimMind.Storyteller.UI.EnableEventNotification.Desc".Translate());

                    SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Storyteller.UI.Section.Fallback".Translate());
                    listing.LabelWithTooltip("RimMind.Storyteller.UI.FallbackModeLabel".Translate(settings.fallbackMode.ToString()),
                        "RimMind.Storyteller.UI.FallbackModeDesc".Translate());
                    if (Widgets.ButtonText(listing.GetRect(30f), "RimMind.Storyteller.UI.SwitchFallback".Translate()))
                    {
                        var modes = new List<FallbackMode>
                        {
                            FallbackMode.Cassandra,
                            FallbackMode.Randy,
                            FallbackMode.Phoebe,
                            FallbackMode.None,
                        };
                        int idx = modes.IndexOf(settings.fallbackMode);
                        settings.fallbackMode = modes[(idx + 1) % modes.Count];
                        GUI.changed = true;
                    }

                    SettingsUIDrawer.DrawCustomPromptSection(listing,
                        "RimMind.Storyteller.UI.StylePromptLabel".Translate(),
                        ref _promptText, 72f);

                    SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Storyteller.UI.Section.Request".Translate());
                    string mtbTip = "RimMind.Storyteller.UI.MTBDays.Desc".Translate();
                    listing.LabelWithTooltip("RimMind.Storyteller.UI.MTBDays".Translate($"{settings.mtbDays:F1}"), mtbTip);
                    settings.mtbDays = listing.SliderWithTooltip(settings.mtbDays, 0.5f, 10f, mtbTip);
                    settings.mtbDays = (float)System.Math.Round(settings.mtbDays, 1);

                    string reqExpireTip = "RimMind.Storyteller.UI.RequestExpire.Desc".Translate();
                    listing.LabelWithTooltip("RimMind.Storyteller.UI.RequestExpire".Translate($"{settings.requestExpireTicks / 60000f:F2}"), reqExpireTip);
                    settings.requestExpireTicks = (int)listing.SliderWithTooltip(settings.requestExpireTicks, 3600f, 120000f, reqExpireTip);
                    settings.requestExpireTicks = (settings.requestExpireTicks / 1500) * 1500;

                    SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Storyteller.UI.Section.Debug".Translate());
                    listing.CheckboxLabeled("RimMind.Storyteller.UI.DebugLogging".Translate(), ref settings.debugLogging,
                        "RimMind.Storyteller.UI.DebugLogging.Desc".Translate());

                    SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Storyteller.UI.Section.Memory".Translate());
                    string eventRecTip = "RimMind.Storyteller.UI.MaxEventRecords.Desc".Translate();
                    listing.LabelWithTooltip("RimMind.Storyteller.UI.MaxEventRecords".Translate(settings.maxEventRecords), eventRecTip);
                    settings.maxEventRecords = (int)listing.SliderWithTooltip(settings.maxEventRecords, 10f, 100f, eventRecTip);

                    string dialRecTip = "RimMind.Storyteller.UI.MaxDialogueRecords.Desc".Translate();
                    listing.LabelWithTooltip("RimMind.Storyteller.UI.MaxDialogueRecords".Translate(settings.maxDialogueRecords), dialRecTip);
                    settings.maxDialogueRecords = (int)listing.SliderWithTooltip(settings.maxDialogueRecords, 5f, 60f, dialRecTip);

                    string reactionTip = "RimMind.Storyteller.UI.MaxPlayerReactions.Desc".Translate();
                    listing.LabelWithTooltip("RimMind.Storyteller.UI.MaxPlayerReactions".Translate(settings.maxPlayerReactions), reactionTip);
                    settings.maxPlayerReactions = (int)listing.SliderWithTooltip(settings.maxPlayerReactions, 5f, 50f, reactionTip);

                    string chainExpireTip = "RimMind.Storyteller.UI.ChainExpireDays.Desc".Translate();
                    listing.LabelWithTooltip("RimMind.Storyteller.UI.ChainExpireDays".Translate($"{settings.chainExpireDays:F1}"), chainExpireTip);
                    settings.chainExpireDays = listing.SliderWithTooltip(settings.chainExpireDays, 3f, 30f, chainExpireTip);
                    settings.chainExpireDays = (float)System.Math.Round(settings.chainExpireDays * 2f) / 2f;

                    string tensionDecayTip = "RimMind.Storyteller.UI.TensionDecayPerDay.Desc".Translate();
                    listing.LabelWithTooltip("RimMind.Storyteller.UI.TensionDecayPerDay".Translate($"{settings.tensionDecayPerDay:F3}"), tensionDecayTip);
                    settings.tensionDecayPerDay = listing.SliderWithTooltip(settings.tensionDecayPerDay, 0.01f, 0.10f, tensionDecayTip);
                    settings.tensionDecayPerDay = (float)System.Math.Round(settings.tensionDecayPerDay * 200f) / 200f;

                    SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Storyteller.UI.MemoryTitle".Translate());
                    var memory = StorytellerMemory.Instance;
                    if (memory == null)
                    {
                        listing.Label("RimMind.Storyteller.UI.MemoryNeedLoad".Translate());
                    }
                    else
                    {
                        listing.Label("RimMind.Storyteller.UI.MemoryRecordCount".Translate(memory.Records.Count, settings.maxEventRecords));
                        listing.Label("RimMind.Storyteller.UI.DialogueRecordCount".Translate(memory.DialogueRecords.Count));
                        for (int i = memory.Records.Count - 1; i >= 0 && i >= memory.Records.Count - 10; i--)
                        {
                            var r = memory.Records[i];
                            int day = TensionMath.TicksToDay(r.TriggeredTick);
                            listing.Label("RimMind.Storyteller.UI.MemoryDayEntry".Translate(day, r.Label, r.MapName));
                        }
                        if (Widgets.ButtonText(listing.GetRect(30f), "RimMind.Storyteller.UI.ClearMemory".Translate()))
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
