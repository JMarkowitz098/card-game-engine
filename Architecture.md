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
- **Mulligan ordering:** mulligan must fully resolve before `GameState` is constructed — its constructor's turn-1 draw would otherwise land inside the hand being mulliganed, since both live in the same `Hand` list with nothing to tell them apart. `MatchSetup.StartGame` takes already-dealt, already-mulligan-resolved `PlayerState`s for this reason; it doesn't deal cards itself.

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
    Cards/                  BattleCard.cs
    Combat/                 CombatResolver.cs
    Events/                 IGameEvent.cs (marker interface + every event record)
    Gameflow/               GameState.cs, MatchSetup.cs, PlayerId.cs, TurnPhase.cs
    Intents/                DeclareAttackIntent.cs, DeclareDefenseIntent.cs, IntentResults.cs
    Players/                PlayerState.cs
  CardGame.Cli/             — playable console harness
    Program.cs
    TurnContext.cs
    StarterDeck.cs          — card templates + deck-expansion; one themed set of 14 templates so far
tests/
  CardGame.Engine.Tests/    — xUnit, references CardGame.Engine
    CombatResolverTests.cs
    PlayerStateTests.cs
    GameStateTests.cs
    MatchSetupTests.cs
    TestCards.cs            — shared card-builder helper for tests
```

## Roadmap

Toward a feature-complete MVP — rules/logic fully in place, remaining work after this is just cards, values, and card variety. Multi-week effort; in order:

1. **Multiple Attacks** - If the attacker still has energy, they can attack multiple times before ending their turn and passing to the other player. The opponent still gets a chance to defend each attack. Because of this, even at 0 energy the attacker should confirm they are ending their turn with a pass
2. **Simple AI opponent** — just a different intent source feeding the same `DeclareAttack`/`DeclareDefense` flow; cheap given the existing architecture.
3. **Deck constraints + catalog** — enforce max 4 copies of any card per 40-card deck (likely its own small `Deck`-validation type — this doesn't cleanly belong to `PlayerState` or `GameState`); expand the card catalog beyond the current 14 themed templates. `StarterDeck` itself will need renaming/reshaping here too — once it's the pool every deck gets built from, "starter deck" won't describe it anymore.
4. **Blazor WebAssembly front end** — reuses `CardGame.Engine` unchanged (the zero-framework-dependency rule from day one pays off here); hotseat UI replacing the console's hide/reveal flow, plus a replay option.
5. **Deck builder UI** — browse the catalog, assemble a legal deck, `localStorage` persistence across visits, import/export (copy/paste or download a deck list).
6. **Deploy to GitHub Pages** — static hosting, no backend needed for this scope.

Also still open, not blocking the sequence above:

- **`CardInstanceId`** — gets more relevant once real deck-building means duplicate-named cards are common; the `Name`-as-identifier interim hack gets shakier.
- **Real online multiplayer** — superseded for now by the Blazor shareable-demo plan above. If it happens later, the existing client/engine split makes it a networking change, not a rewrite.
