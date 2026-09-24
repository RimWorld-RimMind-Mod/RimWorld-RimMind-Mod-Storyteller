# Storyteller incident request slice

Start with `StorytellerComp_RimMindDirector.cs`. It is the RimWorld adapter and keeps the interval gates visible. Open only the next file that owns the behavior you need.

## Reading order

1. `StorytellerComp_RimMindDirector.cs` — target checks, interval gates, diagnostics, and forced-request forwarding.
2. `StorytellerRequestCoordinator.cs` — request dispatch, callback handling, terminal transitions, and chain recording.
3. `StorytellerRequestState.cs` — request token and pending-result semantics.
4. `RimMindIncidentSelector.cs` — parsed-response, known-definition, and `CanFireNow` guards; `IncidentSelectionPolicy.cs` retains points-multiplier bounds and threat-notification policy.
5. `StorytellerNotificationService.cs` — player reaction request and tension callback.
6. `../Memory/StorytellerMemory.cs` — persistent incidents, dialogue, reactions, tension, and chains.

## Context Provider path

Start at `../RimMindStorytellerMod.cs` to see the module composition entry. It delegates all five Context Provider registrations to `StorytellerContextProviderRegistrar.cs`. When you need provider details, read `StorytellerContextProviderRegistrar.cs`, then `../Extensions/StorytellerContextPolicy.cs`, `../Extensions/StorytellerContextBuilder.cs`, `../Memory/StorytellerMemory.cs`, and `RimMindAPI.Memory` in that order. The Mod entry composes the module; it does not own provider content.

All five providers are world-scoped: the only identity guard is `ScenarioStoryteller`. Map NPCs and the `NPC-storyteller` fallback legitimately have `PawnId=0`; no Pawn lookup is needed. `StorytellerContextPolicy` only composes the custom instruction with the generated task instruction. Missing world memory or empty histories still produce no optional content.

## Automatic flow

```text
MakeIntervalIncidents
  -> player-home map check
  -> Memory maintenance
  -> pending incident consumption
  -> API / setting / skip / MTB gates
  -> StorytellerRequestCoordinator.TryDispatch
  -> RimMindAPI.Request.Send
  -> parse and publish
  -> next interval yields the incident
```

`ForceRequest` enters the same coordinator after cancelling the active token and clearing the Storyteller cooldown.

## Boundaries

- The Director owns RimWorld hook order, not LLM callback logic.
- The coordinator owns one request lifecycle, not interval policy or persistent data.
- The notification service owns `RequestEntry` construction, not request terminal state.
- `StorytellerRequestState<TIncident>` is the only token and pending-result state.
- Storyteller reads and writes cross-mod narrator memory through `RimMindAPI.Memory`.
- Background callbacks must not add new Verse or Unity side effects outside the established Core callback handoff.

## Verification

Run `dotnet test RimMind-Storyteller/Tests/RimMindStoryteller.Tests.csproj -c Release` from the repository root. `StorytellerWorldProviderContracts` executes every registered provider for map/fallback identities and checks foreign-scenario isolation. `StorytellerIncidentPolicyContracts` executes the real selector and validates rejection, bounded parameters, faction hints, and notification policy. See `../../Tests/README.md` for fixture boundaries.

Across all Storyteller test projects, allow at most 999 discovered cases (less than 1000), counting each parameterized row. Prefer real behavior and failure/collaboration boundaries; do not test only substitutes, duplicate production algorithms, constrain private implementation shape, or merge unrelated scenarios to reduce discovery count.
