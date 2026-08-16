namespace CardGame.Engine;

public record GameStateTestContext(
    BattleCard AttackCard,
    BattleCard DefenseCard,
    PlayerState PlayerA,
    PlayerState PlayerB,
    GameState GameState,
    DeclareAttackIntent AttackIntent,
    DeclareDefenseIntent DefenseIntent
);