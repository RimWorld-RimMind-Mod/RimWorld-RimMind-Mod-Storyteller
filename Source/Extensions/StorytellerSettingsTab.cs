using UnityEngine;
using RimMind.Presentation.Settings;
using RimMind.Storyteller.Settings;
using Verse;

namespace RimMind.Storyteller
{
    internal sealed class StorytellerSettingsTabAdapter : ISettingsTab
    {
        public string Id => "storyteller";
        public string OwnerModId => "RimMind.Storyteller";
        public string Label => "RimMind.Storyteller.UI.TabLabel".CanTranslate()
            ? (string)"RimMind.Storyteller.UI.TabLabel".Translate()
            : "Storyteller";
        public void Draw(Rect rect) => Settings.StorytellerSettingsTab.Draw(rect);
    }
}
