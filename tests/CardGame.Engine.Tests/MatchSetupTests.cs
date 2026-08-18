namespace CardGame.Engine.Tests;

public class MatchSetupTests
{
    private static List<BattleCard> CreateDeck()
    {
        return Enumerable.Range(0, 40).Select(i => TestCards.Card(name: $"card{i}")).ToList();
    }

    [Fact]
    public void StartGame_WiresPlayersIntoGameState_StartingPlayerHasDrawnTurnOneCard()
    {
        // Arrange
        var playerA = new PlayerState(CreateDeck());
        var playerB = new PlayerState(CreateDeck());
        playerA.DrawCards(5);
        playerB.DrawCards(5);

        // Act
        var gameState = MatchSetup.StartGame(playerA, playerB, PlayerId.PlayerA);

        // Assert
        Assert.Same(playerA, gameState.PlayerA);
        Assert.Same(playerB, gameState.PlayerB);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(6, gameState.PlayerA.Hand.Count); // opening 5 + turn-1 draw
        Assert.Equal(5, gameState.PlayerB.Hand.Count);
    }

    [Fact]
    public void Mulligan_ResolvesBeforeStartGame_TurnOneDrawDoesNotAffectMulliganedHand()
    {
        // Arrange
        var playerA = new PlayerState(CreateDeck());
        var playerB = new PlayerState(CreateDeck());
        playerA.DrawCards(5);
        playerB.DrawCards(5);
        var openingHand = playerA.Hand.ToList();

        // Act
        playerA.Mulligan();
        var mulliganedHand = playerA.Hand.ToList();
        var gameState = MatchSetup.StartGame(playerA, playerB, PlayerId.PlayerA);

        // Assert
        Assert.NotEqual(openingHand, mulliganedHand);
        Assert.Equal(5, mulliganedHand.Count); // mulligan resolved on exactly 5 cards, not 6
        Assert.Equal(6, gameState.PlayerA.Hand.Count); // turn-1 draw happens only after mulligan
        Assert.Equal(34, gameState.PlayerA.DrawPile.Count);
    }
}
