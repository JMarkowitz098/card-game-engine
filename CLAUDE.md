# CardGame

A digital video game version of a TCG (trading card game). Starting as local single-player vs. AI; may expand to online vs. humans later. Architecture is chosen up front to keep that expansion cheap if it happens, without over-building for it now.

## Working with Claude

The user is learning C#/.NET on this project. **Do not write code or edit/create any files unless explicitly told to.** Default mode is advice, explanation, and answering questions — the user wants to write the code themselves. Only make file edits when the user explicitly asks for them in that turn.

## Stack

- **Language:** C# throughout — both the rules engine and the game client.
- **Client engine:** Godot or Unity (not yet finalized).
- **Editor:** VS Code + C# Dev Kit extension.
- **.NET SDK:** installed via the official Microsoft installer (not Homebrew).
- **Testing:** xUnit, run via `dotnet test` and VS Code's Testing panel.

## Why C# for the engine specifically

The rules engine must stay a plain C# class library with **zero dependencies on the game framework** — no `MonoBehaviour`/`Node` types, no scene references, nothing engine-specific. That's what allows it to:

1. Run headless later in a standalone `dotnet` server process (for online play), without dragging in the full game engine runtime.
2. Be unit-tested via `dotnet test`/xUnit entirely outside the game editor — fast, CI-friendly, no editor boot required.
3. Stay reusable regardless of whether the client ends up being Godot or Unity.

If Unity is the eventual client choice, this is moot anyway — Unity is C#-only, so engine and client share a language by default. If Godot, the same discipline (no `Node` references in engine code) is what preserves the option to swap presentation layers or go headless later.

## Architecture

- **Simulation/presentation split:** the rules engine is a pure state machine — it takes player **intents** in and emits **events** out. The client only renders what the engine confirms; it never resolves game logic itself. This split is what makes an eventual online mode a networking change, not a rewrite.
- **Data-driven cards:** cards are *not* hardcoded as bespoke per-card logic. Each card is defined as data (JSON/YAML) composing small reusable primitives in a **trigger → condition → effect** shape (e.g. "on play" → "if target has ≤2 health" → "deal 3 damage"). A small escape-hatch scripting path exists for the rare card that can't be expressed this way.
- **Event bus + state-based actions:** effects emit events (e.g. `CreatureDied`, `DamageDealt`); triggered abilities are listeners on those events. After each resolved action, a state-based-action pass checks board-wide conditions (deaths, expired buffs, etc.) — mirrors how MTG's rules engine avoids special-casing everywhere.
- **Turn/phase flow:** modeled as an explicit state machine (mulligan → draw → main → combat → end), not scattered conditionals.
- **Intents/events as data, from day one:** even in local single-player, intents and events are kept as plain serializable structs/records rather than direct method calls into engine internals. Costs nothing now; it's what makes a later network boundary a drop-in rather than a redesign.
- **Trust model for later online play:** server (same engine assembly, hosted standalone) is authoritative — it validates and resolves every intent; clients are never trusted with rules logic. Not implemented yet, but the intent/event separation above is what makes it possible without a rewrite.

## Project layout

```
CardGame.slnx
src/
  CardGame.Engine/         # plain C# classlib — the rules engine, no game-framework deps
tests/
  CardGame.Engine.Tests/   # xUnit, references CardGame.Engine
```

## Status / next steps

- [x] .NET SDK + VS Code C# Dev Kit installed
- [x] Solution scaffolded: `CardGame.Engine` classlib + `CardGame.Engine.Tests` xUnit project, wired together
- [ ] Get the scaffolded placeholder test passing via `dotnet test` and the VS Code Testing panel (current checkpoint — gate before writing any real engine code)
- [ ] Design the actual card/effect data schema
- [ ] Define the concrete intent/event contract
- [ ] Finalize Godot vs. Unity for the client
