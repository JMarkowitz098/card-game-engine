namespace CardGame.Engine;

public record DeclareAttackIntent(PlayerId AttackingPlayerId, BattleCard Card);