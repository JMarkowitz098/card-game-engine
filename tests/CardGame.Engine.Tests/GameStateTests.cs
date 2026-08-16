using System.IO.Pipelines;

namespace CardGame.Engine.Tests;

public class GameStateTests
{
    private static GameStateTestContext SetupTestContext()
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
        return new GameStateTestContext(
            attackCard, defenseCard, playerA, playerB, gameState, attackIntent, defenseIntent);
    }

    private static GameStateTestContext SetupGameEndContext()
    {
        // Arrange
        var attackCard = new BattleCard("spark", Element.Scor, 1, 1, 20, 1);
        var defenseCard = new BattleCard("breeze", Element.Eth, 1, 1, 2, 1);
        var playerA = new PlayerState(new List<BattleCard> { attackCard });
        var playerB = new PlayerState(new List<BattleCard> { defenseCard });
        var gameState = new GameState(playerA, playerB, PlayerId.PlayerA);
        playerA.DrawCard();
        playerB.DrawCard();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, attackCard);
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerB, defenseCard);
        return new GameStateTestContext(
            attackCard, defenseCard, playerA, playerB, gameState, attackIntent, defenseIntent);
    }

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
        var context = SetupTestContext();
        var gameState = context.GameState;

        // Act
        var result = gameState.DeclareAttack(context.AttackIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToDefend, gameState.Phase);
        Assert.Empty(context.PlayerA.Hand);
        Assert.Equal(0, context.PlayerA.CurrentEnergy);
        Assert.Equal(2, context.PlayerA.MaxEnergy);
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
        var context = SetupTestContext();
        var gameState = context.GameState;

        // Act
        var result = gameState.DeclareAttack(context.AttackIntent);
        result = gameState.DeclareDefense(context.DefenseIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(2, context.PlayerA.CurrentEnergy);
        Assert.Equal(1, context.PlayerB.CurrentEnergy);
        Assert.Equal(7, result.Events.Count);
        Assert.Equal(9, context.PlayerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerB, gameState.ActivePlayer);
        var defenseEvent = Assert.IsType<DefenseDeclared>(result.Events[0]);
        Assert.Equal(PlayerId.PlayerB, defenseEvent.Defender);
        Assert.Equal(1, defenseEvent.DefendValue);
    }

    [Fact]
    public void DeclareAttack_WrongPlayerAttacks_ReturnsFalse()
    {
        // Arrange
        var context = SetupTestContext();
        var gameState = context.GameState;
        var failingIntent = new DeclareAttackIntent(PlayerId.PlayerB, context.AttackCard);

        // Act
        var result = gameState.DeclareAttack(failingIntent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(1, context.PlayerA.CurrentEnergy);
        Assert.Equal(1, context.PlayerA.MaxEnergy);
        Assert.Single(context.PlayerA.Hand);
    }

    [Fact]
    public void DeclareDefense_PlayerHealthReducedToZero_SetsMatchEndedPhase()
    {
        // Arrange
        var context = SetupGameEndContext();
        var gameState = context.GameState;

        // Act
        var result = gameState.DeclareAttack(context.AttackIntent);
        result = gameState.DeclareDefense(context.DefenseIntent);

        // Assert
        Assert.Equal(0, context.PlayerB.CurrentHealth);
        Assert.Equal(TurnPhase.MatchEnded, gameState.Phase);
        Assert.True(result.Success);
        var matchEndedEvent = Assert.IsType<MatchEnded>(result.Events[^1]);
        Assert.Equal(PlayerId.PlayerA, matchEndedEvent.Winner);
    }
}
