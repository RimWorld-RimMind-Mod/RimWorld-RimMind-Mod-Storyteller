using RimMind.Application.Features.Llm;
using RimMind.Domain.Llm;

namespace RimMind.Storyteller
{
    /// <summary>
    /// Storyteller-owned identity used by Core's request queue.
    /// The same id must be passed to <c>LlmRequestEnvelopeBuilder.WithModId</c>, to
    /// <c>RimMindAPI.ClearModCooldown</c> / <c>GetModCooldownTicksLeft</c>, and exposed by
    /// <c>StorytellerModCooldown</c>: Core stores per-mod cooldowns under the envelope ModId,
    /// so any other spelling silently targets a cooldown key that never exists.
    /// </summary>
    public static class StorytellerRequestIds
    {
        /// <summary>Envelope ModId and Core queue cooldown key for every Storyteller request.</summary>
        public const string RequestModId = "RimMind.Storyteller";
    }

    /// <summary>
    /// Builds the incident-selection envelope. Kept free of RimWorld state so the queue identity
    /// can be verified outside the game.
    /// </summary>
    internal static class StorytellerIncidentRequestFactory
    {
        internal const int MaxTokens = 400;
        internal const float Temperature = 0.8f;

        internal static LlmRequestEnvelope Create(string scenarioId, string? schema, string npcId)
            => LlmRequestEnvelopeBuilder
                .ForScenario(scenarioId)
                .WithModId(StorytellerRequestIds.RequestModId)
                .WithSchema(schema)
                .WithMaxTokens(MaxTokens)
                .WithTemperature(Temperature)
                .WithNpcId(npcId)
                .Build();
    }
}
