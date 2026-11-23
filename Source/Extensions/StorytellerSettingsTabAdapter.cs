using RimMind.Presentation.Api;
using RimMind.Presentation.Settings;
using UnityEngine;

namespace RimMind.Storyteller
{
    internal sealed class StorytellerSettingsTabAdapter : ISettingsTab
    {
        public string Id => "storyteller";
        public string OwnerModId => StorytellerRequestIds.OwnerModId;
        public string Label => RimMindAPI.Text.TrOr("RimMind.Storyteller.UI.TabLabel", "Storyteller");
        public void Draw(Rect rect) => Settings.StorytellerSettingsTab.Draw(rect);
    }
}
