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
- **Max copies:** a deck may contain at most 4 copies of any single card. **Not yet implemented.**
- **Empty deck:** drawing from an empty deck is not a loss condition (for now). Drawing multiple cards at once draws as many as are available and reports whether it completed the full requested count, without rolling back any partial draw.
- **Charge timing:** Charge raises MaxEnergy, not current energy
- **Declining to defend:** A player can pass instead of defending, taking full damage from the attack card
- **Declining to attack:** A player can pass instead of attacking too (`DeclareAttackIntent.Card` nullable, same as defense). Turn passes straight to the opponent, no defend phase.
- **Match end:** the match ends the instant a player's `CurrentHealth` hits 0 from a resolved attack.
- **Deck shuffling:** `PlayerState` shuffles its deck (Fisher-Yates) on construction. Seedable via an optional `Random` param for tests; `Random.Shared` otherwise.
- **Draw timing:** exactly one draw per attack-turn. The very first turn's draw happens in `GameState`'s constructor; every turn after that comes from `ReadyNewTurn`. The CLI never triggers a draw itself, only narrates one having happened.

## To Be Implemented

- `CardId`: Unique id tied to card instance. Interim: `BattleCard.Name` used as a unique identifier. Breaks with duplicate-named cards.

## Assumptions

- Starting Health (10) and Energy (1) are global constants.
- Decks have 40 cards, max 4 copies of any single card.
- Both players currently use identical decks (`StarterDeck.Create()` called twice) — every match is a mirror match until the deck builder ships.
- **MVP card model is Cost/Charge/Attack/Defense only** — no `Element`, no special effects, no `LeaderCard`. This is a firm scope decision for this MVP, not a "waiting on playtesting" placeholder.

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

## Roadmap

Toward a feature-complete MVP — rules/logic fully in place, remaining work after this is just cards, values, and card variety. Multi-week effort; in order:

1. **Test fixtures + failure-path coverage** — replace the rigid `SetupTestContext`/`SetupGameEndContext` factory methods with a proper test-data-builder: a shared card-builder usable across every test file, plus a flexible `GameState` scenario builder with sensible defaults. (The current pair already had to be bypassed by `TwoConsecutiveTurns` and the decline-to-attack test, which needed shapes they couldn't provide — that's the sign this doesn't scale as-is.) Then fill the actual gap: `DeclareDefense` has zero rejection tests; `DeclareAttack` only covers wrong-player.
2. **Remove `Element` from the engine** — `BattleCard` loses the field, `Element.cs` deleted, every card-construction call site (including tests) updated.
3. **Mulligan** — draw 5, one optional mulligan, as part of match setup. Worth deciding here whether match-setup logic lives in `GameState` itself or becomes its own type — this is the moment before `GameState` also takes on pre-game responsibilities on top of turn resolution.
4. **Simple AI opponent** — just a different intent source feeding the same `DeclareAttack`/`DeclareDefense` flow; cheap given the existing architecture.
5. **Deck constraints + catalog** — enforce max 4 copies of any card per 40-card deck (likely its own small `Deck`-validation type — this doesn't cleanly belong to `PlayerState` or `GameState`); expand the card catalog beyond the current 10 Fire-flavored templates (removing `Element` makes this easier, not harder). `StarterDeck` itself will need renaming/reshaping here too — once it's the pool every deck gets built from, "starter deck" won't describe it anymore.
6. **Blazor WebAssembly front end** — reuses `CardGame.Engine` unchanged (the zero-framework-dependency rule from day one pays off here); hotseat UI replacing the console's hide/reveal flow, plus a replay option.
7. **Deck builder UI** — browse the catalog, assemble a legal deck, `localStorage` persistence across visits, import/export (copy/paste or download a deck list).
8. **Deploy to GitHub Pages** — static hosting, no backend needed for this scope.

Also still open, not blocking the sequence above:

- **`CardInstanceId`** — gets more relevant once real deck-building means duplicate-named cards are common; the `Name`-as-identifier interim hack gets shakier.
- **Real online multiplayer** — superseded for now by the Blazor shareable-demo plan above. If it happens later, the existing client/engine split makes it a networking change, not a rewrite.
