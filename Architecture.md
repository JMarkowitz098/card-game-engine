# Architecture: core attack/defend/energy loop

Concrete engine design for the game mechanics described in `CLAUDE.md`'s "Card Game Mechanics" section, following the principles laid out in that file's "Architecture" section (intents in / events out, data-driven cards, event bus + state-based actions, explicit turn/phase state machine).

Scope: the **core attack/defend/energy/charge loop only**. Special effects (the data-driven trigger→condition→effect schema for things like "unblockable" or leader effects) are deliberately deferred to a follow-up design pass — this doc covers vanilla cards with no special effects.

**Status: this loop is fully built, tested, and playable** via the `CardGame.Cli` console harness. See "Progress" and "Next steps" at the bottom.

## Confirmed rule interpretations

- **Energy replenish:** at the start of *every* turn, **both** players' energy refills to their own current max — not just the active player's.
- **Card lifecycle:** the attacker's card is discarded immediately when the attack is declared; the defender's card (if any) is discarded when the defense is declared, before damage is applied. The active player draws 1 card at the start of their own attack-turn.
- **Half-cost rounding:** defending with the same element halves the card's Cost, rounding down (integer division) — e.g. Cost 3 → 1, Cost 4 → 2, Cost 5 → 2. **Confirmed but not yet implemented** — see "Next steps."
- **Empty deck:** drawing from an empty deck is not a loss condition (for now). Drawing multiple cards at once draws as many as are available and reports whether it completed the full requested count, without rolling back any partial draw.
- **Charge timing:** a Charge-driven MaxEnergy increase does *not* unlock extra current energy the same turn — it only raises the ceiling that the next `ReplenishEnergy` call fills to. (Resolved implicitly by how `IncreaseMaxEnergy`/`ReplenishEnergy` ended up implemented — `IncreaseMaxEnergy` never touches `CurrentEnergy`.)
- **Declining to defend:** a player may choose not to defend (`DeclareDefenseIntent.Card = null`), taking the attacker's full, unmitigated Attack value as damage.
- **Match end:** the match ends the instant a player's `CurrentHealth` hits 0 from a resolved attack. No leader-specific defeat condition exists yet (no `LeaderCard` in play).

## Open details (not architecture blockers)

- **`CardId` / card-instance identity:** still undesigned. **Interim:** `BattleCard.Name` is treated as a de facto unique identifier for equality/lookup purposes (e.g. `DiscardCard`'s `List.Remove`, which relies on `BattleCard`'s record structural equality). Not a real solution — breaks if two cards ever share a name, or once a hand can hold duplicate copies of the same named card. Superseded once `CardInstanceId` is designed.

## Assumptions

- Starting Health (10) and starting Energy (1) are **global match constants**, not per-leader stats. Easy to move onto `LeaderCard` later if a leader ever needs to override them.
- `Element` is a pure engine/rules concept. Color is a **presentation-layer concern only** and does not belong in the engine type. The client maps `Element` → color for display, keeping the "no presentation coupling in the engine" rule intact.
- `LeaderCard`/`Deck` are still just sketches — never implemented. `PlayerState` has no `Leader` field; a `PlayerState` is constructed directly from a `List<BattleCard>` deck.

## Domain model (as built)

Project layout — `src/CardGame.Engine/` is organized into subfolders (all still the single flat `CardGame.Engine` namespace, folders are pure file organization, not namespace boundaries):

```
Cards/      Element.cs, BattleCard.cs
Combat/     CombatResolver.cs
Events/     IGameEvent.cs (the marker interface + every event record)
Gameflow/   GameState.cs, PlayerId.cs, TurnPhase.cs
Intents/    DeclareAttackIntent.cs, DeclareDefenseIntent.cs, IntentResults.cs
Players/    PlayerState.cs
```

```csharp
enum Element { Enh, Scor, Eth, Shor } // Earth, Fire, Wind, Water
enum PlayerId { PlayerA, PlayerB }
enum TurnPhase { ReadyToAttack, ReadyToDefend, MatchEnded }

record BattleCard(string Name, Element Element, int Cost, int Charge, int Attack, int Defense);
```

```csharp
class PlayerState
{
    int CurrentHealth { get; }      // private set; clamps at 0 via TakeDamage
    int MaxEnergy { get; }          // private set; grows via IncreaseMaxEnergy (Charge)
    int CurrentEnergy { get; }      // private set
    IReadOnlyList<BattleCard> Hand { get; }
    IReadOnlyList<BattleCard> DrawPile { get; }
    IReadOnlyList<BattleCard> DiscardPile { get; }

    PlayerState(List<BattleCard> deck);   // defensive copy into DrawPile; Hand/DiscardPile start empty

    int TakeDamage(int rawDamage);          // clamps at CurrentHealth, returns actual damage applied
    int IncreaseMaxEnergy(int rawEnergy);   // Charge → MaxEnergy; never touches CurrentEnergy
    bool TryUseEnergy(int rawEnergy);       // fails clean (no mutation) if unaffordable
    int GainEnergy(int rawEnergy);          // clamps at MaxEnergy, returns actual amount gained
    void ReplenishEnergy();                 // CurrentEnergy = MaxEnergy
    bool DrawCard();                        // false (no-op) if DrawPile empty
    bool DrawCards(int count);              // draws as many as available; returns false if it fell short
    bool DiscardCard(BattleCard card);      // false if card isn't in Hand
}
```

```csharp
class GameState
{
    PlayerState PlayerA { get; }
    PlayerState PlayerB { get; }
    PlayerId ActivePlayer { get; }   // whose attack-turn it is
    TurnPhase Phase { get; }
    // private BattleCard? _pendingAttackCard — the card declared by DeclareAttack, consumed by DeclareDefense

    GameState(PlayerState playerA, PlayerState playerB, PlayerId startingPlayer);

    IntentResult DeclareAttack(DeclareAttackIntent intent);
    IntentResult DeclareDefense(DeclareDefenseIntent intent);

    PlayerState GetPlayer(PlayerId id);              // public — needed by the CLI harness
    static PlayerId GetOpponentId(PlayerId id);       // public, static
}
```

## Turn/phase state machine (as built)

Only phases that actually pause for player input get a persisted `TurnPhase` value — everything else (energy replenish, drawing, discarding, the win check) runs as plain synchronous steps inside `DeclareAttack`/`DeclareDefense`, per the "only phases that wait get tracked" decision made when designing `GameState`.

```
ReadyToAttack
  ↓ DeclareAttack(intent) — validates turn/phase + hand + affordability
  |   pays Cost → charges attacker's MaxEnergy (Charge) → discards attacker's card
  |   emits: EnergySpent, AttackDeclared, MaxEnergyIncreased, CardDiscarded
  ↓
ReadyToDefend
  ↓ DeclareDefense(intent) — validates turn/phase *unconditionally* (even when declining)
  |   if a card is offered: pays Cost (halving not yet implemented) → discards defender's card
  |   emits (if defending): EnergySpent, DefenseDeclared, CardDiscarded
  |
  |   damage = CombatResolver.ResolveBattle(pendingAttackCard, intent.Card) — Card may be null
  |   applies damage to defender → emits DamageDealt
  |
  |   if defender's CurrentHealth == 0:
  |       Phase → MatchEnded, emits MatchEnded(winner: the attacker) — loop stops here
  |   else:
  |       both players ReplenishEnergy, new active player draws 1, ActivePlayer swaps
  |       emits EnergyReplenishedToFull ×2, CardDrawn
  |       Phase → ReadyToAttack (loop continues)
```

Note the divergence from the original sketch: **Charge and the attacker's discard happen at `DeclareAttack` time**, not bundled into a separate "Resolution" step — there wasn't a good reason to defer them, and nothing about them depends on the defender's response. Only the defender's card and the damage math wait for `DeclareDefense`.

## Intents (data in)

```csharp
record DeclareAttackIntent(PlayerId AttackingPlayerId, BattleCard Card);
record DeclareDefenseIntent(PlayerId DefendingPlayerId, BattleCard? Card); // null = decline to defend
```

`MulliganIntent` was never built — mulligan/match setup is still deferred (see "Next steps").

## Events (data out)

```csharp
interface IGameEvent { }

record AttackDeclared(PlayerId Attacker, int AttackValue) : IGameEvent;
record DefenseDeclared(PlayerId Defender, int DefendValue) : IGameEvent;
record EnergySpent(PlayerId Player, int EnergyValue) : IGameEvent;
record CardDiscarded(PlayerId Player, string Name) : IGameEvent;
record DamageDealt(PlayerId Player, int DamageValue) : IGameEvent;
record MaxEnergyIncreased(PlayerId Player, int AdditionalEnergyValue) : IGameEvent;
record CardDrawn(PlayerId Player) : IGameEvent;
record EnergyReplenishedToFull(PlayerId Player) : IGameEvent;
record MatchEnded(PlayerId Winner) : IGameEvent;

record IntentResult(bool Success, IReadOnlyList<IGameEvent> Events);
```

`IGameEvent` is a marker interface — the C# stand-in for a TypeScript-style union type, since events need to travel together in one typed list without a shared base of real behavior. `IntentResult` bundles success + events together rather than an `out` parameter or an "empty list means failure" convention. Rejected intents always return `IntentResult(false, [])` — no exceptions for expected rejections (wrong turn, can't afford, card not in hand); exceptions are reserved for states that should be provably impossible (e.g. `DeclareDefense` finding no pending attack card while `CanDefend` already confirmed the phase is `ReadyToDefend`).

`EnergySpent` and `CardDiscarded` weren't in the original sketch — added during implementation once it became clear paying a cost and discarding a card each needed their own event, and (for `EnergySpent` specifically) needed to generalize beyond attack/defense once effects can spend energy too. `MatchStarted`, `MulliganResolved`, `TurnStarted`, and `LeaderDefeated` were never built — all tied to still-deferred features (match setup/mulligan, `LeaderCard`).

## Worked example (verification)

Still holds — see the Spark/Breeze exchange originally verified against this model; the numbers are unaffected by the charge/discard-timing change, since nothing observable depends on *when within the exchange* those side effects happen, only that they happen once per exchange.

## Progress (this session)

- `CombatResolver` — pure damage math, fully tested.
- `PlayerState` — all mutation methods built and tested (health, energy, charge, hand/draw/discard).
- `GameState` — full `DeclareAttack`/`DeclareDefense` orchestration: turn/phase validation, win detection, decline-to-defend, event emission. 24 passing tests, including a two-turn role-reversal regression test. **Coverage gap:** only one rejection path is tested (wrong-player attack) — most of the rejection matrix (wrong phase, card not in hand, can't afford, for both `DeclareAttack` and `DeclareDefense`) is still untested. See "Next steps."
- `CardGame.Cli` — a real, playable console harness: full turn loop, human-readable event narration, per-player color coding, pacing delays, win announcement.
- Tooling: `.editorconfig` + analyzers, `.gitignore`, VS Code format-on-save, `CA1707` scoped-suppressed for test naming.

## Next steps

Roughly in priority order:

1. **Implement same-element-halves-cost** — confirmed rule, not yet wired into `CanUseCard`/`DeclareDefense`.
2. **Fill out `GameState`'s rejection-path test coverage** — `DeclareDefense` currently has zero rejection tests (wrong phase, wrong player, card not in hand, can't afford), and `DeclareAttack` only has "wrong player." Same reasoning as the two-turn regression test: these guard clauses have already broken silently once this session (the missing-`return` bug), so untested guard clauses are a real, demonstrated risk here, not just theoretical.
3. **Real, varied decks for the CLI harness** — replace the 40-identical-card dummy deck with a small hand-authored set of distinct `BattleCard`s spread across all four elements, so the harness is actually worth playtesting.
4. **`CardInstanceId`** — once duplicate-named cards or networked play make the `Name`-as-identifier shortcut untenable.
5. **Match setup / mulligan** — draw 5, single mulligan option. Still deferred; low complexity, low priority relative to the above.
6. **`LeaderCard` + leader effects, and the data-driven trigger→condition→effect schema** for special effects generally — both intentionally deferred since the start.
7. **Unity integration** — client framework decision leans Unity; deferred until the harness is in good enough shape to be worth connecting to a real client.
