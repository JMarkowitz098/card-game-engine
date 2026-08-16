namespace CardGame.Engine;

public record DeclareDefenseIntent(PlayerId DefendingPlayerId, BattleCard? Card);