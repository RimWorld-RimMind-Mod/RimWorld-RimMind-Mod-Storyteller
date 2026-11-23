using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Storyteller.Settings;

namespace RimMind.Storyteller
{
    internal sealed class StorytellerModCooldown : IModCooldown
    {
        private readonly RimMindStorytellerSettings _settings;
        public StorytellerModCooldown(RimMindStorytellerSettings settings) { _settings = settings; }
        // Must equal the incident-selection envelope ModId: Core stores queue cooldowns by that key.
        public string Id => StorytellerRequestIds.RequestModId;
        public string OwnerModId => StorytellerRequestIds.OwnerModId;
        public int CooldownTicks => (int)(_settings.mtbDays * 60000f);
    }
}
