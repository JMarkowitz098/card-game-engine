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
- Started designing "draw instead of attack" as a new rule — not yet implemented (see `Architecture.md` Next steps).
