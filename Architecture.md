# Architecture: core attack/defend/energy loop

Concrete engine design for the game mechanics described in `CLAUDE.md`'s "Card Game Mechanics" section, following the principles laid out in that file's "Architecture" section (intents in / events out, data-driven cards, event bus + state-based actions, explicit turn/phase state machine).

Scope: the **core attack/defend/energy/charge loop only**. Special effects (the data-driven trigger→condition→effect schema for things like "unblockable" or leader effects) are deliberately deferred to a follow-up design pass — this doc covers vanilla cards with no special effects.

## Confirmed rule interpretations

- **Energy replenish:** at the start of *every* turn, **both** players' energy refills to their own current max — not just the active player's.
- **Card lifecycle:** a played card (attack or defense) is discarded after use; the active player draws 1 card at the start of their own attack-turn.
- **Half-cost rounding:** defending with the same element halves the card's Cost, rounding down (integer division) — e.g. Cost 3 → 1, Cost 4 → 2, Cost 5 → 2.
- **Empty deck:** drawing from an empty deck is not a loss condition (for now). Drawing multiple cards at once draws as many as are available and reports whether it completed the full requested count, without rolling back any partial draw.

## Open details (not architecture blockers — pin down when writing the relevant test)

- Whether a Charge-driven MaxEnergy increase makes extra *current* energy available immediately that same turn, or only takes effect starting next turn's replenish.

## Assumptions

- Starting Health (10) and starting Energy (1) are **global match constants**, not per-leader stats. Easy to move onto `LeaderCard` later if a leader ever needs to override them.
- `Element` is a pure engine/rules concept. Color (orange/red/green/blue) is a **presentation-layer concern only** — a placeholder for card art — and does not belong in the engine type. The client maps `Element` → color for display, keeping the "no presentation coupling in the engine" rule intact.

## Domain model

```csharp
enum Element { Enh, Scor, Eth, Shor } // Earth, Fire, Wind, Water

record LeaderCard(string Name, IReadOnlyList<Element> Elements /* 1 or 2 */);
record BattleCard(string Name, Element Element, int Cost, int Charge, int Attack, int Defense);
record Deck(LeaderCard Leader, IReadOnlyList<BattleCard> Cards); // 40 cards; validated against Leader.Elements
```

Special-effect fields are intentionally omitted from `LeaderCard`/`BattleCard` for this slice (YAGNI until the effects schema is designed).

Mutable runtime state, kept separate from the immutable card-definition data above:

```csharp
class PlayerState
{
    LeaderCard Leader;
    int Health;
    int Energy;
    int MaxEnergy;
    List<BattleCard> Hand;
    List<BattleCard> DrawPile;
    List<BattleCard> DiscardPile;
}

class GameState
{
    PlayerState PlayerA, PlayerB;
    PlayerId ActivePlayer;   // whose attack-turn it is
    TurnPhase Phase;
}
```

## Turn/phase state machine

```
MatchSetup        — both leaders placed, both draw 5, each player may mulligan once
  ↓
StartOfTurn       — both players' energy → their MaxEnergy; active player draws 1
  ↓
AttackDeclared    — active player commits a battle card as the attack (pays Cost)
  ↓
DefenseDeclared   — defending player commits a battle card to block (pays Cost, halved if
                     same Element as the attack) or passes (takes full damage)
  ↓
Resolution        — damage = max(0, Attack − Defense); apply to defender's Health;
                     apply attacker's Charge to attacker's MaxEnergy (defender's Charge does
                     NOT apply — charge is attack-only); both used cards → discard
  ↓
StateBasedCheck   — either Health ≤ 0? → MatchEnded
  ↓ (else)
swap ActivePlayer → back to StartOfTurn
```

## Intents (data in)

```csharp
record MulliganIntent(PlayerId Player, bool Mulligan);
record DeclareAttackIntent(PlayerId Player, CardId Card);
record DeclareDefenseIntent(PlayerId Player, CardId? Card); // null = pass
```

## Events (data out)

```csharp
record MatchStarted(...);
record MulliganResolved(PlayerId Player, bool Mulliganed);
record TurnStarted(PlayerId ActivePlayer);
record EnergyReplenished(PlayerId Player, int NewEnergy);
record CardDrawn(PlayerId Player, CardId Card);
record AttackDeclared(PlayerId Attacker, CardId Card, int AttackValue);
record DefenseDeclared(PlayerId Defender, CardId? Card, int DefenseValue);
record DamageDealt(PlayerId Target, int Amount);
record MaxEnergyIncreased(PlayerId Player, int NewMax);
record CardDiscarded(PlayerId Player, CardId Card);
record LeaderDefeated(PlayerId Player);
record MatchEnded(PlayerId Winner);
```

Every intent/event is a plain record — consistent with the "intents/events as data from day one" rule, and the shape that later makes a network boundary a drop-in.

## Suggested TDD order

Ground-up, cheapest-to-test-first:

1. **Pure combat math** — a small function/class (e.g. `CombatResolver`) that takes attack value, defense value (or none), and element-match, and returns damage + effective defense cost. No state machine, no mutation — the easiest, fastest thing to test-drive first.
2. **`PlayerState` mutations** — spend/replenish energy, MaxEnergy increase via charge, health reduction, hand/draw/discard transitions — each in isolation.
3. **Turn state machine orchestration** — wire intents → validation → the pieces above → events.
4. **Match setup/mulligan flow** — last, since it's least central to the core loop.

## Worked example (verification)

Walking the `Spark` vs. `Breeze` exchange from CLAUDE.md's Match Example through this model, to confirm the numbers match before writing the first test.

Setup: P1 & P2 both start Health 10, Energy 1, MaxEnergy 1.

**Turn 1 (active = P1):**
- *StartOfTurn:* both energies replenish to their MaxEnergy (no change, already at 1/1). P1 draws 1.
- *AttackDeclared:* P1 plays **Spark** (Scor, Cost 1, Charge 1, Atk 2, Def 1) → pays Cost 1 → P1 energy `1→0`.
- *DefenseDeclared:* P2 blocks with **Breeze** (Eth, Cost 1, Charge 1, Atk 2, Def 1). Different element than Spark, so no cost halving → pays Cost 1 → P2 energy `1→0`.
- *Resolution:* damage = max(0, Atk 2 − Def 1) = **1**. P2 Health `10→9` — matches "Player 2 takes 1 damage."
  Attacker's Charge (1) applies to P1's MaxEnergy → `1→2` — matches "Player 1... now has a max of 2." Defender's Charge does *not* apply (attack-only rule).
- P1 ends the turn with 0 energy — matches "Player 1 is out of energy" (true in the moment; refills next turn since replenish is universal).

**Turn 2 (active = P2):** StartOfTurn refills *both* — P1 energy `0→2` (new max), P2 energy `0→1` — then P2 attacks, P1 defends, same shape as Turn 1. That's the "keep going back and forth."

Numbers match the example exactly — no gaps found in this model.
