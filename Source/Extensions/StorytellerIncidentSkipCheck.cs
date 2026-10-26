using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Storyteller.Settings;

namespace RimMind.Storyteller
{
    internal sealed class StorytellerIncidentSkipCheck : ISkipCheck
    {
        private readonly RimMindStorytellerSettings _settings;
        public StorytellerIncidentSkipCheck(RimMindStorytellerSettings settings) { _settings = settings; }
        public string Id => "storyteller.incident";
        public string OwnerModId => "RimMind.Storyteller";
        public SkipCheckKind Kind => SkipCheckKind.StorytellerIncident;
        public bool ShouldSkip(in SkipCheckArgs args) => !_settings.enableIntervalTrigger;

        public static bool ShouldSkipStorytellerIncident()
            => RimMind.Presentation.Api.RimMindAPI.ShouldSkipStorytellerIncident();
    }
}
