# Architecture

Document describing architecture of the project.

## Game Engine
The core of the program that runs the game and processes logic. Intended to be frontend agnostic.
- Core loop: Replenish -> Draw -> Attack -> Charge -> Replenish -> Defend. 
- Attack / Defend steps will eventually include special effect

## Cli
A simple console based UI for testing the engine and simulating games

## Web Client (in progress)
`CardGame.Web` — Blazor WebAssembly, references `CardGame.Engine` and `CardGame.DeckManagement`. Single page, hotseat (no separate routes per player); human-vs-human first, `CardGame.Ai` wiring deferred until that works end-to-end. Core loop (setup → mulligan → attack/defend → victory) is built end-to-end; not yet covered by any automated UI test (see Conventions).
- `GameSessionService` — DI-registered (`AddScoped`, `Program.cs`), holds the live `PlayerState`s/`GameState`. `SetupPlayers`/`Mulligan`/`BeginGame`/`DeclareAttack`/`DeclareDefense`/`ConfirmReady` implemented and tested, each raising `Changed` on success.
- `PendingHandOffTo` (`PlayerId?`, `null` by default) — replaces the CLI's blocking `HandoffTo` calls. Set to `PlayerId.PlayerA` by `SetupPlayers`, then to whichever player should see the screen next: the opponent after a mulligan (accept or decline both advance it), the defender after a real attack, the attacker after a resolved (non-lethal) defense, the new active player after a decline, `null` once the match ends or via `ConfirmReady` (the "I'm ready" action). Only updates on success — a rejected action leaves it untouched.
- `event Action? Changed;` — raised at the end of every mutating method (`SetupPlayers` unconditionally; `BeginGame`/`ConfirmReady` past their guard; `Mulligan`/`DeclareAttack`/`DeclareDefense` only inside the success branch) so components know to re-render.
- `Mulligan(PlayerId id, bool willMulligan)` — throws if players aren't set up yet or if `Game` already exists (mulligan window's closed). "No second mulligan" enforcement lives on `PlayerState` itself (`HasMulliganed` flag, folded into `Mulligan()`'s existing bool return), not the service. Declining (`willMulligan: false`) always succeeds and advances `PendingHandOffTo`, without touching `PlayerState`; accepting still goes through the one-time guard. Both outcomes set `PendingHandOffTo = GameState.GetOpponentId(id)` on success.

### Page flow (`Home.razor` / `Home.razor.cs`)
A `Phase` enum (`Title`/`Setup`/`Mulligan`/`Play`/`GameOver`) drives an `@switch` in `Home.razor`'s markup; no component besides `Home` carries `@page`, everything else (`Setup`, `Mulligan`, etc.) is a plain embedded component. `Home.razor.cs` is a partial-class code-behind holding the phase state and handlers.
- `_activePlayerId` (`PlayerId?`) — tracks whichever player currently owns the interactive screen. Set only inside the hand-off "Ready" handler, nowhere else — deriving or reassigning it elsewhere (e.g. right after declaring an attack) gives it two conflicting meanings and produces a hand-off screen naming the wrong player.
- The hand-off screen (`PendingHandoffTo.razor`) renders whenever `Game.PendingHandOffTo != null`, checked ahead of the `Phase` switch, and reads the greeted player's name straight from `GameSessionService.PendingHandOffTo` — the authoritative value — not from `_activePlayerId`.
- `PostMulliganHand.razor` shows a player's post-mulligan hand before their hand-off, gated by a local `_showingPostMulliganHand` bool; also renders ahead of the `Phase` switch.
- Attack vs. defend routing: `OnCardClicked` compares `GameState.ActivePlayer` to `_activePlayerId` to decide `DeclareAttack` vs. `DeclareDefense`. `Hand.razor`'s "Pass" button invokes the same callback with a `null` card, reusing the engine's existing null-card-means-decline semantics for both roles.
- `Phase.GameOver` is entered from `OnCardClicked`'s defense branch by checking `GameState.Phase == TurnPhase.MatchEnded` right after a successful `DeclareDefense`; the winner is derived from whichever player's `CurrentHealth` is 0 — nothing stores a `Winner` anywhere else.

**Components** (`Shared/`), all presentational — no rules logic; only `Home.razor.cs` talks to `GameSessionService` directly, everything else takes what it needs as `[Parameter]`s:
- `Card.razor` — one `BattleCard`'s stats. `GameCard` (required), `IsPlayable` (styling hook, not yet wired to any visual difference), `OnClick` (`EventCallback<BattleCard>`, optional).
- `Hand.razor` — loops a card list through `Card`, forwarding `OnCardClicked` and calling `IsCardPlayable(card)` per card; also renders a "Pass" button (see Page flow).
- `PlayerStats.razor` — health/energy readout from a `PlayerState`.
- `Pile.razor` — a card-back count (`Name`, `NumCards`); one component reused for both draw and discard piles rather than separate `DrawPile`/`DiscardPile` files.
- `PlayerBoardInformation.razor` — a player's name + stats + both piles, plus an optional `AttackCard` (`BattleCard?`) shown while the viewer is defending.
- `PlayerBoard.razor` — `PlayerBoardInformation` + `Hand`, plus an `IsAttacking` label. `IsCardPlayable(card)` checks `card.Cost <= Stats.CurrentEnergy` (no longer a hardcoded `true`).
- `Setup.razor` — player-name entry (`<form>`/`<fieldset>`/`<label>`), no `@page` directive; `OnSubmit` carries both names up as `EventCallback<(string, string)>`.
- `Mulligan.razor` — a player's hand + yes/no; `OnSubmit` is `EventCallback<bool>`.
- `PostMulliganHand.razor`, `PendingHandoffTo.razor`, `VictoryScreen.razor` — see Page flow above.

### Styling
CSS isolation (`<Component>.razor.css`, e.g. `Card.razor.css`) is the primary styling mechanism — scoped to just that component's rendered markup, no setup needed beyond the file naming convention. Global `wwwroot/css/app.css` is reserved for genuinely site-wide rules (fonts, resets); inline `style="..."` only for rare one-offs. Bootstrap's stylesheet has been removed from `index.html` — writing custom CSS throughout rather than leaning on its default theme.
- An interactive wrapper element (`<button>`, etc.) around real visual content should be reset, not styled directly — `all: unset;` (plus re-adding whatever `unset` also strips that's still wanted, e.g. `cursor: pointer;`) strips the browser's default control chrome so the actual visual styling lives on the content element inside it (`.card-article`, not `.card-button`).
- Form controls (`button`, `input`, `select`, `textarea`) don't inherit page typography by default — browsers give them their own native control font via the UA stylesheet, typically smaller than surrounding text. `font-size: medium` isn't a no-op here despite `medium` being the CSS spec's own default value for that property elsewhere; `font: inherit;` is the general fix.
- `<dl>`/`<dt>`/`<dd>` is the semantic element for label→value pairs (a card's Cost/Charge, Attack/Defense) — more correct than two `<p>` tags, but note `<dd>` carries its own default left-margin (~40px indent) that a CSS reset needs to explicitly include (`dl, dt, dd { margin: 0; }`), easy to miss since `<p>` never had one.
- `align-items` on a flex container controls whether its children stretch to fill the cross axis (default) or shrink to their own content size (`center`, etc.) — a child that needs to stay full-width regardless of a `center`d parent should set `align-self: stretch` on itself, overriding the parent just for that one element.
- Static assets (images, etc.) must live under `wwwroot` (e.g. `wwwroot/images/`) to be reachable at runtime — anywhere else in the repo isn't served, build or no build.
- `dotnet watch` not picking up `.razor.css` changes is the same known macOS SDK 10.0.300+ bug as the general watch breakage (still present on the currently-installed 10.0.400) — it can't find `staticwebassets.development.json`, which scoped-CSS bundling depends on. Workaround: an external file-watcher (`fswatch`) invoking a plain `dotnet build` directly sidesteps watch's own broken rebuild-detection.

**Conventions:** every required parameter gets both `[EditorRequired]` (Razor-level warning if the call site omits it) and C#'s `required` modifier (satisfies nullable analysis — `EditorRequired` alone does not; note Razor does *not* enforce `required` at component call sites the way C# enforces it on object initializers, so a missing required parameter compiles and silently defaults); no `Engine.`-qualified types when `@using CardGame.Engine` is already in scope; `@` prefix on every parameter-binding attribute value, including bare variables, but not plain string literals; a component's own file name can never also be a member name inside it (`CS0542` — e.g. `Card.razor` can't have a `Card` parameter, must be `GameCard`); `<h3>` for a component's own section title, `<p>`/`<span>` for data.
- A component only gets data through its own `[Parameter]`s — never by reading a parent's private fields or injected services directly; those aren't in scope for a child component.
- `T!` (null-forgiving) does not unwrap `Nullable<T>` — it only suppresses nullable-*reference*-type warnings. On a nullable value type (`PlayerId?`, etc.) it's a no-op and the expression stays `T?`; use `.Value` to get `T`.
- An attribute value chaining an operator right after an `@`-prefixed member access (`@Game.PlayerB!`, `!.Value`, etc.) can trip Razor's parser (`RZ9986`, "complex content") — wrap it as an explicit expression instead: `@(Game.PlayerB!)`.
- No bUnit/component-level UI tests exist or are currently planned — `GameSessionService`/`GameState` carry all real logic and are fully unit-tested; UI-layer tests would mostly re-verify wiring. Revisit only if `Home`'s phase/hand-off orchestration keeps growing in complexity.

## Deployment
`.github/workflows/deploy.yml` — on every push to `main`: builds, runs `dotnet test`, publishes `CardGame.Web` (Release), then deploys to GitHub Pages via `actions/upload-pages-artifact`/`actions/deploy-pages`. Rewrites the `<base href>` in the *published* `index.html` only (to `/card-game-engine/`, the GitHub Pages project-site path) so `wwwroot/index.html` itself is untouched and local dev keeps serving from `/`; adds `.nojekyll` since GitHub Pages otherwise ignores the underscore-prefixed `_framework` folder. Requires repo Settings → Pages → Source set to "GitHub Actions" (one-time), and a PAT with the `workflow` scope to push any change to a workflow file at all.

## Confirmed rule interpretations

- **Energy replenish:** at the start of *every* turn, **both** players' energy refills to their own current max — not just the active player's.
- **Card lifecycle:** Attacking player draws card at start of turn. Cards are discarded right when used.
- **Max copies:** a deck may contain at most 4 copies of any single card. **Not yet implemented.**
- **Empty deck:** drawing from an empty deck is not a loss condition (for now). Drawing multiple cards at once draws as many as are available and reports whether it completed the full requested count, without rolling back any partial draw.
- **Charge timing:** Charge raises MaxEnergy, not current energy
- **Declining to defend:** A player can pass instead of defending, taking full damage from the attack card
- **Declining to attack:** A player can pass instead of attacking too (`DeclareAttackIntent.Card` nullable, same as defense). Turn passes straight to the opponent, no defend phase.
- **Match end:** the match ends the instant a player's `CurrentHealth` hits 0 from a resolved attack.
- **Pending attack card:** `GameState._pendingAttackCard` clears back to `null` immediately after `DeclareDefense` resolves it (right after `CombatResolver.ResolveBattle` consumes it), whether or not the match ends. `GetPendingAttackCard()` only ever reflects the *current* exchange, never a stale one from a prior round.
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
    Pages/                  Home.razor + Home.razor.cs (page-level phase state machine); NotFound.razor is an unremoved template placeholder
    Layout/                 MainLayout.razor, NavMenu.razor
    Shared/                 Card.razor, Hand.razor, PlayerStats.razor, Pile.razor, PlayerBoardInformation.razor,
                            PlayerBoard.razor, Setup.razor, Mulligan.razor, PostMulliganHand.razor,
                            PendingHandoffTo.razor, VictoryScreen.razor
    Services/               GameSessionService.cs
.github/
  workflows/deploy.yml     — build/test/publish CardGame.Web + deploy to GitHub Pages on push to main (see Deployment)
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

1. **Blazor WebAssembly front end** — core loop built end-to-end: setup → mulligan → attack/defend (hotseat hand-off replacing the console's hide/reveal flow) → victory screen. Still open: a replay/rematch option, and `Card.razor`'s `IsPlayable` isn't visually distinguished yet (the data's there, no styling difference).
2. **Deck constraints + catalog** — enforce max 4 copies of any card per 40-card deck (likely its own small `Deck`-validation type — this doesn't cleanly belong to `PlayerState` or `GameState`); expand the card catalog beyond the current 14 themed templates. `StarterDeck` itself will need renaming/reshaping here too — once it's the pool every deck gets built from, "starter deck" won't describe it anymore. Deferred to sit right before the deck builder below, since nothing produces a candidate deck to validate until then.
3. **Deck builder UI** — browse the catalog, assemble a legal deck, `localStorage` persistence across visits, import/export (copy/paste or download a deck list).
4. **Deploy to GitHub Pages** — pipeline built (`.github/workflows/deploy.yml`, see Deployment); not yet confirmed live — first push was rejected for a PAT missing the `workflow` scope.

Also still open, not blocking the sequence above:

- **`CardInstanceId`** — gets more relevant once real deck-building means duplicate-named cards are common; the `Name`-as-identifier interim hack gets shakier.
- **Real online multiplayer** — superseded for now by the Blazor shareable-demo plan above. If it happens later, the existing client/engine split makes it a networking change, not a rewrite.
