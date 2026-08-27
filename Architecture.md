# Architecture

Document describing architecture of the project.

## Game Engine
The core of the program that runs the game and processes logic. Intended to be frontend agnostic.
- Core loop: Replenish -> Draw -> Attack -> Charge -> Replenish -> Defend. 
- Attack / Defend steps will eventually include special effect

## Cli
A simple console based UI for testing the engine and simulating games

## Web Client (in progress)
`CardGame.Web` — Blazor WebAssembly, references `CardGame.Engine` and `CardGame.DeckManagement`. Single page, hotseat (no separate routes per player); human-vs-human first, `CardGame.Ai` wiring deferred until that works end-to-end.
- `GameSessionService` — DI-registered, holds the live `PlayerState`s/`GameState`. `SetupPlayers`/`Mulligan`/`BeginGame`/`DeclareAttack`/`DeclareDefense`/`ConfirmReady` implemented and tested, each raising `Changed` on success.
- `PendingHandOffTo` (`PlayerId?`, `null` by default) — replaces the CLI's blocking `HandoffTo` calls. Set to `PlayerId.PlayerA` by `SetupPlayers`, then to whichever player should see the screen next: the opponent after a mulligan, the defender after a real attack, the attacker after a resolved (non-lethal) defense, the new active player after a decline, `null` once the match ends or via `ConfirmReady` (the "I'm ready" action). Only updates on success — a rejected action leaves it untouched.
- `event Action? Changed;` — raised at the end of every mutating method (`SetupPlayers` unconditionally; `BeginGame`/`ConfirmReady` past their guard; `Mulligan`/`DeclareAttack`/`DeclareDefense` only inside the success branch) so components know to re-render.
- `Mulligan(PlayerId id)` — throws if players aren't set up yet or if `Game` already exists (mulligan window's closed). "No second mulligan" enforcement lives on `PlayerState` itself (`HasMulliganed` flag, folded into `Mulligan()`'s existing bool return), not the service. On success sets `PendingHandOffTo = GameState.GetOpponentId(id)`.

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
- **Multiple attacks:** an attacker keeps `ActivePlayer` after a resolved exchange and may attack again — `DeclareDefense` no longer advances the turn itself, it just resets `Phase` to `ReadyToAttack`. Only an explicit pass (`DeclareAttackIntent.Card == null`) triggers `ReadyNewTurn` (swap `ActivePlayer`, replenish both players, draw). Energy doesn't replenish between attacks within the same turn, so a defender can run out of energy to defend with mid-turn and be forced to decline.
- **Simple AI:** `CardGame.Ai.Logic.DeclareAttack`/`DeclareDefense` pick from the AI's affordable cards only. Attack: highest `Attack`, tie-broken by lowest `Cost`, then highest `Charge`, then lowest `Defense`, then first-found. Defense: greatest damage reduction (`Defense` capped at the incoming attack value), tie-broken by lowest `Cost`. Either returns `null` (pass/decline) if nothing in hand is affordable.

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
  CardGame.Ai/              — simple AI opponent; references CardGame.Engine only
    Logic.cs                DeclareAttack/DeclareDefense card-selection
  CardGame.DeckManagement/  — deck/catalog concerns shared by every client; references CardGame.Engine only
    StarterDeck.cs          — card templates + deck-expansion; one themed set of 14 templates so far
  CardGame.Cli/             — playable console harness
    Program.cs
    TurnContext.cs
  CardGame.Web/             — Blazor WebAssembly front end; references CardGame.Engine + CardGame.DeckManagement
    Pages/                  Home.razor; Counter.razor/Weather.razor/NotFound.razor are unremoved template placeholders
    Layout/                 MainLayout.razor, NavMenu.razor
    Services/               GameSessionService.cs
tests/
  CardGame.Engine.Tests/    — xUnit, references CardGame.Engine
    CombatResolverTests.cs
    PlayerStateTests.cs
    GameStateTests.cs
    MatchSetupTests.cs
    TestCards.cs            — shared card-builder helper for tests
  CardGame.Ai.Tests/        — xUnit, references CardGame.Ai + CardGame.Engine.Tests (reuses TestCards)
    LogicTests.cs
  CardGame.Web.Tests/       — xUnit, references CardGame.Web + CardGame.Engine.Tests (reuses TestCards)
    GameSessionServiceTests.cs
```

## Roadmap

Toward a feature-complete MVP — rules/logic fully in place, remaining work after this is just cards, values, and card variety. Multi-week effort; in order:

1. **Blazor WebAssembly front end** — reuses `CardGame.Engine` unchanged (the zero-framework-dependency rule from day one pays off here); hotseat UI replacing the console's hide/reveal flow, plus a replay option.
2. **Deck constraints + catalog** — enforce max 4 copies of any card per 40-card deck (likely its own small `Deck`-validation type — this doesn't cleanly belong to `PlayerState` or `GameState`); expand the card catalog beyond the current 14 themed templates. `StarterDeck` itself will need renaming/reshaping here too — once it's the pool every deck gets built from, "starter deck" won't describe it anymore. Deferred to sit right before the deck builder below, since nothing produces a candidate deck to validate until then.
3. **Deck builder UI** — browse the catalog, assemble a legal deck, `localStorage` persistence across visits, import/export (copy/paste or download a deck list).
4. **Deploy to GitHub Pages** — static hosting, no backend needed for this scope.

Also still open, not blocking the sequence above:

- **`CardInstanceId`** — gets more relevant once real deck-building means duplicate-named cards are common; the `Name`-as-identifier interim hack gets shakier.
- **Real online multiplayer** — superseded for now by the Blazor shareable-demo plan above. If it happens later, the existing client/engine split makes it a networking change, not a rewrite.
