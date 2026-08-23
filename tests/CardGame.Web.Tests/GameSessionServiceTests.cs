namespace CardGame.Web.Tests;

public class GameSessionServiceTests
{
    [Fact]
    public void StartMatch_CreatesPlayerStatesAndDrawsFiveCardsEach()
    {
        // Arrange
        var gameSession = new GameSessionService();

        // Act
        gameSession.StartMatch();

        // Assert
        Assert.NotNull(gameSession.PlayerA);
        Assert.NotNull(gameSession.PlayerB);
        Assert.Equal(5, gameSession.PlayerA.Hand.Count);
        Assert.Equal(5, gameSession.PlayerB.Hand.Count);
    }
}
