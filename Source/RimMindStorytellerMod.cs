using HarmonyLib;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Presentation;
using RimMind.Presentation.Api;
using RimMind.Presentation.Settings;
using RimMind.Storyteller.Extensions;
using RimMind.Storyteller.Settings;
using UnityEngine;
using Verse;

namespace RimMind.Storyteller
{
    public class RimMindStorytellerMod : RimMindSubmodBase<RimMindStorytellerSettings>
    {
        public static new RimMindStorytellerSettings Settings = null!;

        protected override string HarmonyPackageId => "mcocdaa.RimMindStoryteller";

        public RimMindStorytellerMod(ModContentPack content) : base(content)
        {
            Settings = base.Settings;
            InitializeHarmony();

            RimMindAPI.Extensions<ISettingsTab>().Register(new StorytellerSettingsTabAdapter());
            RimMindAPI.Extensions<IModCooldown>().Register(new StorytellerModCooldown(Settings));
            RimMindAPI.Extensions<ISkipCheck>().Register(new StorytellerIncidentSkipCheck(Settings));

            StorytellerContextProviderRegistrar.RegisterAll();

            Log.Message("[RimMind-Storyteller] Initialized.");
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            StorytellerSettingsTab.Draw(rect);
        }
    }
}
