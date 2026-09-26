<div align="center">

# RimMind-Storyteller 📜
### AI Story Director, Dramatic Tension Tracking & Narrative Incidents for RimWorld 1.6

**English** | [简体中文](README_zh.md)

<p>
  <a href="https://rimworldgame.com/"><img src="https://img.shields.io/badge/RimWorld-1.6-brightgreen.svg" alt="RimWorld 1.6"></a>
  <a href="https://github.com/mcocdaa/RimWorld-RimMind-Mod-Core"><img src="https://img.shields.io/badge/Dependency-RimMind--Core-blue.svg" alt="Dependency: RimMind-Core"></a>
  <a href="#"><img src="https://img.shields.io/badge/Unit%20Tests-20%2B%20Passing-success.svg" alt="Unit Tests"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
</p>

<p><em>Experience a living RimWorld storyteller that crafts consequential, multi-chapter narrative drama based on your colony's real history.</em></p>

</div>

---

## 📖 Overview

**RimMind-Storyteller** reimagines RimWorld's classic storyteller AI. Instead of firing arbitrary raid dice based strictly on colony wealth graphs, the AI Storyteller acts as a genuine narrative director, assessing emotional pacing, past colony deeds, and unresolved conflicts to orchestrate coherent, multi-part sagas.

### Core Features
- **Dramatic Tension Curve**: Continuously monitors the colony's emotional equilibrium, balancing hardship, recovery, and sudden twists.
- **Contextual Narrative Incident Chains**: Connects incidents logically—e.g., a rescue of an escaped refugee might provoke a targeted retaliatory manhunt days later.
- **Custom Literary Incident Letters**: Incident notifications feature evocative, AI-composed storytelling prose that explicitly names colonists, past milestones, and geographic context.

---

## 🎮 In-Game Showcase

![RimMind-Storyteller Showcase](docs/images/showcase.jpg)
*Custom narrative incident letter: An AI storyteller presents an impending crisis with bespoke literary lore reflecting the colony's past choices.*

---

## 🏛️ Storyteller Director Architecture

```mermaid
flowchart TD
    State["Colony History & Wealth Metrics"] --> Tension["Dramatic Tension Curve Evaluator"]
    Tension --> Pacing{"Is Pacing Appropriate?"}
    Pacing -- Needs Climax --> IncidentGen["Select & Chain Narrative Incident"]
    Pacing -- Needs Respite --> Relief["Select Opportunity or Peace Event"]
    IncidentGen --> Lore["Generate Custom Incident Letter Text"]
    Lore --> Letter["Display Narrative Incident to Player"]
```

---

## 🛠️ Installation & Load Order

```text
1. Harmony
2. Core (Vanilla RimWorld)
3. RimMind-Core
4. RimMind-Storyteller
```

---

## 🧪 Developer Guide & Testing

Run unit tests directly:

```powershell
dotnet test RimMind-Storyteller/Tests/RimMindStoryteller.Tests.csproj -c Release
```

---

## 📜 License

Licensed under the [MIT License](LICENSE).
