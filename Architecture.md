# Architecture

Document describing architecture of the project.

## Game Engine
The core of the program that runs the game and processes logic. Intended to be frontend agnostic.
- Core loop: Replenish -> Draw -> Attack -> Charge -> Replenish -> Defend. 
- Attack / Defend steps will eventually include special effect

## Cli
A simple console based UI for testing the engine and simulating games

## Confirmed rule interpretations

- **Energy replenish:** at the start of *every* turn, **both** players' energy refills to their own current max — not just the active player's.
- **Card lifecycle:** Attacking player draws card at start of turn. Cards are discarded right when used.
- **Half-cost rounding:** defending with the same element halves the card's Cost, rounding down (integer division) — e.g. Cost 3 → 1, Cost 4 → 2, Cost 5 → 2. **Not yet implemented**
- **Empty deck:** drawing from an empty deck is not a loss condition (for now). Drawing multiple cards at once draws as many as are available and reports whether it completed the full requested count, without rolling back any partial draw.
- **Charge timing:** Charge raises MaxEnergy, not current energy
- **Declining to defend:** A player can pass instead of defending, taking full damage from the attack card
- **Match end:** the match ends the instant a player's `CurrentHealth` hits 0 from a resolved attack.
- **Deck shuffling:** `PlayerState` shuffles its deck (Fisher-Yates) on construction. Seedable via an optional `Random` param for tests; `Random.Shared` otherwise.

## To Be Implemented

- `CardId`: Unique id tied to card instance. Interim: `BattleCard.Name` used as a unique identifier. Breaks with duplicate-named cards.
- `LeaderCard`: Special cards with a variety of effects. Currently `PlayerState` takes a raw `List<BattleCard>`, no `Leader` field.

## Assumptions

- Starting Health (10) and Energy (1) are global constants, not per-leader stats.
- Decks have 40 cards

## Project Structure

```
src/
  CardGame.Engine/          — rules engine (single flat `CardGame.Engine` namespace; folders are just organization)
    Cards/                  Element.cs, BattleCard.cs
    Combat/                 CombatResolver.cs
    Events/                 IGameEvent.cs (marker interface + every event record)
    Gameflow/               GameState.cs, PlayerId.cs, TurnPhase.cs
    Intents/                DeclareAttackIntent.cs, DeclareDefenseIntent.cs, IntentResults.cs
    Players/                PlayerState.cs
  CardGame.Cli/             — playable console harness
    Program.cs
    TurnContext.cs
tests/
  CardGame.Engine.Tests/    — xUnit, references CardGame.Engine
    CombatResolverTests.cs
    PlayerStateTests.cs
    GameStateTests.cs
    GameStateTestContext.cs
```

## Progress

- `CombatResolver` — pure damage math, fully tested.
- `PlayerState` — health/energy/charge/hand/draw/discard, fully tested. Now shuffles on construction.
- `GameState` — `DeclareAttack`/`DeclareDefense` orchestration, win detection, decline-to-defend. 25 tests. Gap: rejection-path coverage is thin (see Next steps).
- `CardGame.Cli` — playable console harness: turn loop, event narration, color coding, pacing delays.
- `StarterDeck` — tuple-list card templates → `SelectMany`/`Enumerable.Repeat` expansion into a 40-card deck. Mechanism built; card content still being authored.
- Fixed: `ProcessAttack`/`ProcessDefense` weren't checking `DeclareAttack`/`DeclareDefense`'s `Success` — an unaffordable/invalid card silently did nothing instead of being rejected. Both now retry-loop until a valid, accepted choice is made.
- Tooling: `.editorconfig` + analyzers, `.gitignore`, VS Code format-on-save, `CA1707` scoped-suppressed for test naming, CSharpier (local dotnet tool + VS Code extension) for layout/line-wrapping on top of the analyzers.
- `README.md` Setup section — prerequisites + commands for a fresh clone.

## Next steps

Roughly in priority order:

1. **"Draw instead of attack"** — new rule, in design. Needs its own intent (not a nullable `Card` on `DeclareAttackIntent` — declining to defend keeps the same phase transition either way, but skipping the attack skips `ReadyToDefend` entirely, so it's a different state-machine outcome). Also needs `ReadyNewTurn` generalized to take a plain `PlayerId` instead of a `DeclareDefenseIntent` so both paths can call it, and a decision on whether it needs its own event or reuses `CardDrawn`.
2. **Same-element-halves-cost** — confirmed rule, unwired.
3. **`GameState` rejection-path tests** — `DeclareDefense` has none; `DeclareAttack` only covers wrong-player.
4. **Finish `StarterDeck` card content** — in progress.
5. **`CardInstanceId`** — once duplicate-named cards or networked play make the `Name`-as-identifier shortcut untenable.
6. **Match setup / mulligan** — draw 5, single mulligan. Deferred, low priority.
7. **`LeaderCard` + leader effects + data-driven effects schema** — deferred since the start.
8. **Unity integration** — deferred until the harness is solid.
