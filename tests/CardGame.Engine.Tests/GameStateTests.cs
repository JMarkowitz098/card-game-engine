namespace CardGame.Engine.Tests;

public class GameStateTests
{
    [Fact]
    public void Constructor_SetsActivePlayerAndAwaitingAttackPhase()
    {
        var playerA = new PlayerState(new List<BattleCard>());
        var playerB = new PlayerState(new List<BattleCard>());

        var gameState = new GameState(playerA, playerB, PlayerId.PlayerA);

        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
    }

    [Fact]
    public void DeclareAttack_ValidatesAttack_ReturnsTrueAndSetsAwaitingDefensePhase()
    {
        // Arrange
        var attackCard = new BattleCard("spark", Element.Scor, 1, 1, 2, 1);
        var playerA = new PlayerState(new List<BattleCard> { attackCard });
        var playerB = new PlayerState(new List<BattleCard>());
        var gameState = new GameState(playerA, playerB, PlayerId.PlayerA);
        playerA.DrawCard();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, attackCard);

        // Act
        var result = gameState.DeclareAttack(attackIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToDefend, gameState.Phase);
        Assert.Empty(playerA.Hand);
        Assert.Equal(0, playerA.CurrentEnergy);
        Assert.Equal(2, playerA.MaxEnergy);
        Assert.Equal(4, result.Events.Count);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        var attackEvent = Assert.IsType<AttackDeclared>(result.Events[0]);
        Assert.Equal(PlayerId.PlayerA, attackEvent.Attacker);
        Assert.Equal(2, attackEvent.AttackValue);
    }

    [Fact]
    public void DeclareDefense_ValidatesDefenseAndUpdatesHealth_ReturnsTrueAndSetsAwaitingAttackPhase()
    {
        // Arrange
        var attackCard = new BattleCard("spark", Element.Scor, 1, 1, 2, 1);
        var defenseCard = new BattleCard("breeze", Element.Eth, 1, 1, 2, 1);
        var playerA = new PlayerState(new List<BattleCard> { attackCard });
        var playerB = new PlayerState(new List<BattleCard> { defenseCard });
        var gameState = new GameState(playerA, playerB, PlayerId.PlayerA);
        playerA.DrawCard();
        playerB.DrawCard();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, attackCard);
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerB, defenseCard);

        // Act
        var result = gameState.DeclareAttack(attackIntent);
        result = gameState.DeclareDefense(defenseIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(2, playerA.CurrentEnergy);
        Assert.Equal(1, playerB.CurrentEnergy);
        Assert.Equal(7, result.Events.Count);
        Assert.Equal(9, playerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerB, gameState.ActivePlayer);
        var defenseEvent = Assert.IsType<DefenseDeclared>(result.Events[0]);
        Assert.Equal(PlayerId.PlayerB, defenseEvent.Defender);
        Assert.Equal(1, defenseEvent.DefendValue);
    }
}
