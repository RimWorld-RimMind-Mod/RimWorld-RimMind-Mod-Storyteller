using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Presentation.Api;

namespace RimMind.Storyteller.Agent
{
    public static class StorytellerAgentController
    {
        public const string ScopeType = StorytellerRequestIds.AgentScopeType;
        public const string ScopeId = StorytellerRequestIds.DefaultNpcId;

        public static IScopedAgent? Find()
            => RimMindAPI.Agents.FindScoped(ScopeType, ScopeId);

        public static IScopedAgent? GetOrCreate()
            => RimMindAPI.Agents.GetOrCreateScoped(ScopeType, ScopeId, Verse.Find.CurrentMap?.Index);

        public static bool Start()
            => RimMindAPI.Agents.StartScoped(ScopeType, ScopeId, Verse.Find.CurrentMap?.Index);

        public static bool Pause()
            => RimMindAPI.Agents.PauseScoped(ScopeType, ScopeId);

        public static bool ForceThink()
            => RimMindAPI.Agents.ForceThinkScoped(ScopeType, ScopeId);
    }
}
