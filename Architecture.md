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
- **Declining to attack:** A player can pass instead of attacking too (`DeclareAttackIntent.Card` nullable, same as defense). Turn passes straight to the opponent, no defend phase.
- **Match end:** the match ends the instant a player's `CurrentHealth` hits 0 from a resolved attack.
- **Deck shuffling:** `PlayerState` shuffles its deck (Fisher-Yates) on construction. Seedable via an optional `Random` param for tests; `Random.Shared` otherwise.
- **Draw timing:** exactly one draw per attack-turn. The very first turn's draw happens in `GameState`'s constructor; every turn after that comes from `ReadyNewTurn`. The CLI never triggers a draw itself, only narrates one having happened.

## To Be Implemented

- `CardId`: Unique id tied to card instance. Interim: `BattleCard.Name` used as a unique identifier. Breaks with duplicate-named cards.
- `LeaderCard`: Special cards with a variety of effects. Currently `PlayerState` takes a raw `List<BattleCard>`, no `Leader` field.

## Assumptions

- Starting Health (10) and Energy (1) are global constants, not per-leader stats.
- Decks have 40 cards
- Both players currently use identical decks (`StarterDeck.Create()` called twice) — every match is a mirror match for now.
- No special effects or `LeaderCard` — holding off until playtesting the vanilla loop specifically signals a need, not building speculatively.

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
    StarterDeck.cs          — card templates + deck-expansion; currently Fire (`Scor`) only
tests/
  CardGame.Engine.Tests/    — xUnit, references CardGame.Engine
    CombatResolverTests.cs
    PlayerStateTests.cs
    GameStateTests.cs
    GameStateTestContext.cs
```

## Next steps

Roughly in priority order:

1. **Quick replay** — finishing a match currently requires relaunching (`dotnet run`) to play again. Highest-value change for actual playtesting volume.
2. **`GameState` rejection-path tests** — `DeclareDefense` has none; `DeclareAttack` only covers wrong-player.
3. **Expand `StarterDeck` beyond Fire** — only `Scor` cards exist right now; both players get identical decks.
4. **Same-element-halves-cost** — confirmed rule, unwired. Explicitly on hold for now.
5. **`CardInstanceId`** — once duplicate-named cards or networked play make the `Name`-as-identifier shortcut untenable.
6. **Match setup / mulligan** — draw 5, single mulligan. Deferred, low priority.
7. **`LeaderCard` + leader effects + data-driven effects schema** — deferred until playtesting signals the vanilla loop needs them, not before.
8. **Networking** — deferred; hotseat play covers same-room testing for now. If it happens, the console harness itself can be the networked client (two processes, shared authoritative `GameState`) — no GUI required first.
