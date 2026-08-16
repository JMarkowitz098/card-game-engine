namespace CardGame.Engine;
// Arrange
        // var attackCard = new BattleCard("spark", Element.Scor, 1, 1, 2, 1);
        // var playerA = new PlayerState(new List<BattleCard> { attackCard });
        // var playerB = new PlayerState(new List<BattleCard>());
        // var gameState = new GameState(playerA, playerB, PlayerId.PlayerA);
        // playerA.DrawCard();
        // var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, attackCard);

public record GameStateTestContext(
    BattleCard AttackCard,
    BattleCard DefenseCard,
    PlayerState PlayerA,
    PlayerState PlayerB,
    GameState GameState,
    DeclareAttackIntent AttackIntent,
    DeclareDefenseIntent DefenseIntent
);