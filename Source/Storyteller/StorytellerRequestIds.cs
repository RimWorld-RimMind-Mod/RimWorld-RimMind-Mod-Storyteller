using RimMind.Application.Common.Constants;
using RimMind.Application.Features.Llm;
using RimMind.Domain.Llm;

namespace RimMind.Storyteller
{
    /// <summary>
    /// Storyteller-owned identities and schema metadata shared across request queues,
    /// context providers, altar chat, notifications, and scoped agents.
    /// </summary>
    public static class StorytellerRequestIds
    {
        /// <summary>Canonical extension owner ID for RimMind-Storyteller.</summary>
        public const string OwnerModId = RimMindOwnerConsts.StorytellerModId;

        /// <summary>Envelope ModId and Core queue cooldown key for automatic/forced incident selection.</summary>
        public const string RequestModId = "RimMind.Storyteller";

        /// <summary>
        /// Dedicated ModId for interactive Altar chat. Does not prefix-match <see cref="RequestModId"/>
        /// under Core's <c>QueueCircuitBreaker.MatchesModId</c>, so interactive player turns are never
        /// blocked by the 1.5-day incident MTB cooldown.
        /// </summary>
        public const string ChatModId = "RimMind.StorytellerChat";

        /// <summary>Scenario ID for interactive Storyteller Altar dialogue.</summary>
        public const string ChatScenarioId = "StorytellerChat";

        /// <summary>Fallback world-level NPC identity when no map proxy NPC exists.</summary>
        public const string DefaultNpcId = "NPC-storyteller";

        /// <summary>Scoped agent type identifier for the Storyteller agent.</summary>
        public const string AgentScopeType = "storyteller";

        /// <summary>Pending-request notification source identifier.</summary>
        public const string NotificationSource = "storyteller";

        /// <summary>Schema registry key for structured incident selection responses.</summary>
        public const string IncidentOutputSchemaKey = "IncidentOutput";

        /// <summary>JSON Schema matching <see cref="IncidentResponse"/> registered into Core's SchemaRegistry.</summary>
        public const string IncidentOutputSchemaJson =
            "{\"type\":\"object\",\"properties\":{" +
            "\"defName\":{\"type\":\"string\"}," +
            "\"reason\":{\"type\":\"string\"}," +
            "\"announce\":{\"type\":\"string\"}," +
            "\"params\":{\"type\":\"object\",\"properties\":{" +
            "\"points_multiplier\":{\"type\":\"number\"}," +
            "\"faction_hint\":{\"type\":\"string\"}," +
            "\"raid_strategy_hint\":{\"type\":\"string\"}" +
            "}}," +
            "\"chain\":{\"type\":\"object\",\"properties\":{" +
            "\"chain_id\":{\"type\":\"string\"}," +
            "\"chain_step\":{\"type\":\"integer\"}," +
            "\"chain_total\":{\"type\":\"integer\"}," +
            "\"next_hint\":{\"type\":\"string\"}" +
            "}}" +
            "},\"required\":[\"defName\",\"reason\"]}";
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
                .WithRequireAliveColonist(false)
                .Build();
    }
}
