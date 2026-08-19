# Progress

Dated log of what's been done. `Architecture.md` holds current state only — this is the history.

## 2026-08-16

- Built `CombatResolver` (pure damage math), `PlayerState` (health/energy/charge/hand/draw/discard), and `GameState` (`DeclareAttack`/`DeclareDefense` orchestration, win detection, decline-to-defend) — full engine test suite, 24 tests.
- Built `CardGame.Cli` console harness: turn loop, event narration, color coding, pacing delays.
- Set up `.editorconfig` + analyzers, `.gitignore`, VS Code format-on-save, `CA1707` scoped-suppressed for test naming.

## 2026-08-17

- Implemented deck shuffling — `PlayerState` shuffles (Fisher-Yates) on construction, seedable via an optional `Random` param for tests. 25th test added to prove it.
- Added `StarterDeck` — tuple-list card templates expanded into a 40-card deck via `SelectMany`/`Enumerable.Repeat`. Mechanism built; card content still being authored.
- Added CSharpier (local dotnet tool + VS Code extension) for line-wrapping/layout formatting, alongside the existing analyzers.
- Added `README.md` Setup section — prerequisites + commands for a fresh clone.
- Fixed: `ProcessAttack`/`ProcessDefense` weren't checking `DeclareAttack`/`DeclareDefense`'s `Success` — an unaffordable/invalid card silently did nothing instead of being rejected. Both now retry-loop until a valid, accepted choice is made.
- Implemented "pass instead of attacking" (`DeclareAttackIntent.Card` nullable, mirrors defense) — simpler than the "draw instead" idea floated earlier; that was dropped in favor of this.
- Found and fixed a real, pre-existing bug in `ReadyNewTurn`: it drew a card for the wrong player (`opponent` instead of the actual next active player) — masked until now by test decks always running out and by the CLI's own redundant draw call. Renamed its parameters (`defender`/`defendingPlayerId` → `nextActivePlayer`/`nextActivePlayerId`) since the old names actively caused the confusion. Added a regression test.
- Found and fixed a genuine double-draw bug: `Loop()` had its own `DrawCard()` call duplicating `ReadyNewTurn`'s. Fixed by moving the match's very first draw into `GameState`'s constructor and deleting `Loop()`'s draw call (kept the narration message, which no longer triggers a draw itself).
- Built the hotseat handoff flow for playing on one shared screen — `HandoffTo()` ("hide your cards" / "pass the device"), three per turn: attacker → defender (privacy for the defend choice), defender → original attacker (so they see the outcome they missed while hidden), then → the new active player's fresh turn. Added `PrintDefenseSummary` (card used, damage, full status) shown at the middle handoff.
- Added `GameState.GetPendingAttackCard()`/`GetLastDefenseCard()` so the CLI can re-derive "what happened" after the screen clears; fixed `GetPendingAttackCard`'s return type, which was incorrectly non-nullable.
- Added `FormatCard` — full stat line now shown in every "attacks with"/"defends with" narration (previously just name, or name + one stat).
- Column-aligned `PrintCard`'s hand listing.
- Added player naming — prompt at match start, blank input keeps the "PlayerA"/"PlayerB" default. Purely presentation — `PlayerId` itself is untouched everywhere else.
- Authored `StarterDeck`'s first real card set — 10 Fire (`Scor`) templates, 4 copies each, 40-card deck. Only element built so far; both players still get identical decks.
- Played a full real match — confirmed it's fun. Discussed and decided: hold off on same-element-cost and on effects/leaders until playtesting specifically signals a need; hotseat play over networking for now; if networking happens later, the console harness itself can be the networked client, no GUI needed first.
- Planned the MVP roadmap toward a shareable portfolio piece: cut `Element` and same-element-cost entirely (not just deferred); locked the MVP card model to Cost/Charge/Attack/Defense only; added mulligan, a simple AI opponent, max-4-copies deck constraints, a Blazor WebAssembly front end (reusing `CardGame.Engine` unchanged), a deck builder with `localStorage` persistence + import/export, and GitHub Pages deployment to the plan. Full sequence recorded in `Architecture.md`'s Roadmap.
- Code-smell discussion: agreed not to restructure `Program.cs` given most of its console-specific code is disposable once Blazor lands. Flagged `GameState` as the real risk (permanent, about to grow with mulligan/deck-validation) and `StarterDeck` as needing a rename once it becomes a shared catalog rather than one hardcoded deck. Also identified the test suite's rigid `SetupTestContext`/`SetupGameEndContext` factories as already straining (two newer tests had to bypass them entirely) — a test-data-builder is the fix. Moved test fixtures + failure-path coverage to the top of the Roadmap.

## 2026-08-18

- Replaced `GameStateTestContext`/`SetupTestContext`/`SetupGameEndContext` with explicit per-test setup: a shared `TestCards.Card(...)` builder (optional named params, sensible defaults) and a `GameStateTests.CreateGameState(...)` helper (optional per-player decks + starting player). `GameStateTestContext.cs` deleted.
- Filled the failure-path gap: `DeclareDefense` now has rejection tests (wrong phase, active player defending, unaffordable card); `DeclareAttack` gained wrong-phase and unaffordable-card cases alongside the existing wrong-player one.
- Removed `Element` from the engine — `BattleCard` is down to `Name`/`Cost`/`Charge`/`Attack`/`Defense`, `Element.cs` deleted, all call sites (`StarterDeck`, `TestCards`, `CombatResolverTests`) updated. MVP card model is now fully in place as scoped.
- Roadmap items 1 (test fixtures) and 2 (remove `Element`) complete; remaining roadmap renumbered.
- Decided match setup (opening hands + mulligan) should be its own `MatchSetup` type rather than folded into `GameState` — expected to grow, and keeps `GameState` scoped to turn resolution only. `GameState` itself doesn't change; `MatchSetup` builds both `PlayerState`s, deals the opening hands, mediates mulligan, then hands off a ready `GameState`. Next: TDD `MatchSetup` starting with tests, migrate the existing setup logic/tests into it, then add mulligan with its own tests.
- Built `MatchSetup.StartGame` and `PlayerState.ReturnCard`/`ReturnCards` (undo a draw — the inverse of `DiscardCard`, card(s) back to the top of the draw pile). Caught a real sequencing bug while wiring up mulligan: `GameState`'s constructor already draws a "turn 1" card for the starting player, so if mulligan happens after `GameState` exists, that bonus card is indistinguishable from the opening hand and gets mulliganed too. Fixed by moving `Mulligan()` onto `PlayerState` itself (return hand, reshuffle, redraw 5) and changing `MatchSetup.StartGame` to take already-dealt, already-mulligan-resolved `PlayerState`s rather than raw decks — dealing is now the caller's job, `StartGame` is purely the pregame-to-`GameState` hand-off. `PlayerState` also now keeps the `Random` it was constructed with (was previously used once and discarded) so `Mulligan` can reshuffle deterministically in tests.
- Updated the CLI to run the full mulligan flow at match start: handoff to Player A, show hand, y/n prompt, show new hand if mulliganed, handoff to Player B, repeat, handoff back to Player A before the match loop begins.
- Discussed whether `MatchSetup` still earns its keep now that it's a one-line wrapper around `new GameState(...)` — kept it, since a planned coin-flip-for-starting-player feature is a natural second responsibility for it to own.
- Roadmap item "Mulligan" complete; remaining roadmap renumbered.

## 2026-08-19

- Implemented Multiple Attacks: `DeclareDefense` no longer calls `ReadyNewTurn` on non-lethal resolution — it just resets `Phase` back to `ReadyToAttack`, leaving `ActivePlayer` on the same attacker. `ReadyNewTurn` (replenish both, draw, swap) now fires only through `DeclareAttack`'s existing decline-to-attack path, which already had this behavior — no new intent or method needed to end a turn.
- TDD'd `DeclareAttack_PlayerCanAttackMultipleTimes_ReturnsTrue` through several real bugs before it was solid: identical test cards masking attacker/defender mixups (fixed with uniquely-named cards), attacker ids that didn't match their own comments, a missing explicit pass, and a genuine energy-affordability edge case — a defender who spends energy blocking one attack can't afford to block a second one in the same turn, since energy only replenishes on a pass. Resolved by having the defender decline rather than special-casing card costs.
- `ReadyNewTurn`'s `Phase = TurnPhase.ReadyToAttack` line was briefly deleted as cleanup, then restored as real code rather than left implicit — makes the method fully self-contained (sets up everything a new turn needs) instead of depending on its one caller already having the phase right.
- Restructured the CLI's `Loop()`: turn header/status/draw-narration now print once per real turn; an inner loop lets the same attacker keep attacking (reprinting their current hand each time) until they explicitly pass, only then running the final handoff to the next player.
- Found and fixed a bug from that restructuring: a match ending mid-turn (a later attack in a multi-attack turn lands the killing blow) left `Loop()`'s inner loop stuck asking for another attack forever — `DeclareAttack` rejects everything once `Phase` is `MatchEnded`, including a pass, so there was no valid input that could break the loop. Fixed by checking `Phase == TurnPhase.MatchEnded` right after each resolved exchange and returning immediately instead of looping back.
- Roadmap item "Multiple Attacks" complete; remaining roadmap renumbered.
