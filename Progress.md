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
