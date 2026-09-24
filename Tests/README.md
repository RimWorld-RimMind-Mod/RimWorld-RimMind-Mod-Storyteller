# RimMind Storyteller contract tests

The project compiles only the active behavior contracts and explicitly linked
production seams. Legacy files remain on disk but are excluded.

## Active contract manifest

| Contract | Stable boundaries | Discovered cases |
|---|---|---:|
| `Contracts/StorytellerIncidentPolicyContracts.cs` | response parsing, real incident selector guards and bounded parameters, faction/strategy hints, pawn lookup, notification policy | 8 |
| `Contracts/StorytellerRequestContextContracts.cs` | context scoping/composition, tension math, request state | 1 |
| `Contracts/StorytellerSaveErrorContracts.cs` | persistence codec, malformed-load normalization, request failure isolation | 1 |
| `Contracts/StorytellerWorldProviderContracts.cs` | five registered provider callbacks for map/fallback NPC identities with PawnId=0, plus foreign-scenario isolation | 10 |

Current discovery count: 20 cases. Across all Storyteller test projects, allow at most 999 discovered cases (less than 1000), counting each parameterized data row. Do not combine unrelated scenarios merely to reduce the count.

Tests must exercise real behavior, failure boundaries, and module collaboration. Substitutes isolate external dependencies; do not test only mocks, copy production algorithms, or lock private implementation shape.

## Provider and incident coverage

The project links the real `StorytellerContextProviderRegistrar`, `StorytellerContextBuilder`, and `RimMindIncidentSelector`. Provider contracts invoke the callbacks registered by production code, not a separately reconstructed guard. World-backed memory data, translation, Core public services, Def databases, and incident workers are external fixtures. These tests prove routing/content selection and safe incident parameters, not complete RimWorld world loading or provider scheduling.

The selector returns no incident for malformed input, unknown definitions, or `CanFireNow=false`; valid response metadata is preserved when appropriate. Parameter tests verify clamping before worker validation, hostile-faction lookup, and unknown-strategy fallback. The old bool-to-enum policy truth table and registrar source-order assertions are not used as integration evidence.

## Persistence coverage level

`StorytellerSaveErrorContracts` executes the production persistence codec with a
behavior-capable Scribe recorder. It verifies stable keys/defaults/modes, null
collection recovery, request failure isolation and single-consumption success
state without compiling RimWorld adapters.

## Active project entry and run

`RimMindStoryteller.Tests.csproj` is the authoritative compile manifest. It uses `Contracts/**/*.cs`, explicit production-source links, `VerseStubs.cs`, and the shared `ContractCaseRunner`, with Core Domain/Application references.

From the repository root:

```powershell
dotnet test RimMind-Storyteller/Tests/RimMindStoryteller.Tests.csproj -c Release
```

This is not deployment or in-game verification.

Legacy compile categories superseded by these contracts are:

- incident response/parser, incident selector, pawn lookup, and notification tests;
- request envelope, context provider, prompt, tension, and decay tests;
- architecture-direction tests covered by the request/context contract.

## Retired legacy tests

Files outside `Contracts/` are retained on disk but excluded from compilation.
Their behavior mapping is recorded in the root contract mapping document.
Deletion requires explicit owner approval for each exact file path; directories are never deleted.
