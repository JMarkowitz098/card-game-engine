using CardGame.Engine.Tests;

namespace CardGame.Web.Tests;

public class GameSessionServiceTests
{
    [Fact]
    public void SetupPlayers_CreatesPlayerStatesAndDrawsFiveCardsEach()
    {
        // Arrange
        var gameSession = new GameSessionService();

        // Act
        gameSession.SetupPlayers();

        // Assert
        Assert.NotNull(gameSession.PlayerA);
        Assert.NotNull(gameSession.PlayerB);
        Assert.Equal(5, gameSession.PlayerA.Hand.Count);
        Assert.Equal(5, gameSession.PlayerB.Hand.Count);
    }

    [Fact]
    public void BeginGame_CreatesGameAndSetsActivePlayer()
    {
        // Arrange
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers();

        // Act
        gameSession.BeginGame(Engine.PlayerId.PlayerA);

        // Assert
        Assert.NotNull(gameSession.Game);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.PendingHandOffTo);
    }

    [Fact]
    public void BeginGame_ThrowsIfPlayersAreNotCreated()
    {
        // Arrange
        var gameSession = new GameSessionService();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            gameSession.BeginGame(Engine.PlayerId.PlayerA)
        );

        // Assert
        Assert.Equal("Cannot begin the game before both players have been set up.", ex.Message);
    }

    [Fact]
    public void DeclareAttack_ActivePlayerChanges_ReturnsTrue()
    {
        // Arrange
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers();

        // Act
        gameSession.BeginGame(Engine.PlayerId.PlayerA);
        var result = gameSession.DeclareAttack(null);

        // Assert
        Assert.True(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(Engine.PlayerId.PlayerB, gameSession.Game.ActivePlayer);
        Assert.Equal(Engine.PlayerId.PlayerB, gameSession.PendingHandOffTo);
    }

    [Fact]
    public void DeclareAttack_InvalidChoice_ReturnsFalse()
    {
        // Arrange
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers();
        var invalidCard = TestCards.Card();

        // Act
        gameSession.BeginGame(Engine.PlayerId.PlayerA);
        var result = gameSession.DeclareAttack(invalidCard);

        // Assert
        Assert.False(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.PendingHandOffTo);
    }

    [Fact]
    public void DeclareAttack_RealCard_HandsOffToDefender()
    {
        // Arrange
        var seededDeckA = new List<Engine.BattleCard>
        {
            TestCards.Card(),
            TestCards.Card(),
            TestCards.Card(),
            TestCards.Card(),
            TestCards.Card(),
        };
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers(seededDeckA);
        Assert.NotNull(gameSession.PlayerA);
        var playerA = gameSession.PlayerA;

        // Act
        gameSession.BeginGame(Engine.PlayerId.PlayerA);
        var result = gameSession.DeclareAttack(playerA.Hand[0]);

        // Assert
        Assert.True(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(Engine.PlayerId.PlayerB, gameSession.PendingHandOffTo);
    }

    [Fact]
    public void DeclareDefense_InvalidChoiceAfterValidAttack_StaysOnDefender()
    {
        // Arrange
        var seededDeckA = new List<Engine.BattleCard>
        {
            TestCards.Card(),
            TestCards.Card(),
            TestCards.Card(),
            TestCards.Card(),
            TestCards.Card(),
        };
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers(seededDeckA);
        Assert.NotNull(gameSession.PlayerA);
        var playerA = gameSession.PlayerA;
        var invalidDefenseCard = TestCards.Card(name: "not in hand");

        // Act
        gameSession.BeginGame(Engine.PlayerId.PlayerA);
        gameSession.DeclareAttack(playerA.Hand[0]);
        var result = gameSession.DeclareDefense(invalidDefenseCard);

        // Assert
        Assert.False(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(Engine.PlayerId.PlayerB, gameSession.PendingHandOffTo);
    }

    [Fact]
    public void DeclareDefense_ActivePlayerStaysSame_ReturnsTrue()
    {
        // Arrange
        var seededDeckA = new List<Engine.BattleCard>
        {
            TestCards.Card(),
            TestCards.Card(),
            TestCards.Card(),
            TestCards.Card(),
            TestCards.Card(),
        };
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers(seededDeckA);
        Assert.NotNull(gameSession.PlayerA);
        var playerA = gameSession.PlayerA;

        // Act
        gameSession.BeginGame(Engine.PlayerId.PlayerA);
        gameSession.DeclareAttack(playerA.Hand[0]);
        var result = gameSession.DeclareDefense(null);

        // Assert
        Assert.True(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.PendingHandOffTo);
    }

    [Fact]
    public void DeclareDefense_InvalidChoice_ReturnsFalse()
    {
        // Arrange
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers();

        // Act
        gameSession.BeginGame(Engine.PlayerId.PlayerA);
        var result = gameSession.DeclareDefense(null); // Can't defend before an attack

        // Assert
        Assert.False(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.PendingHandOffTo);
    }

        [Fact]
    public void DeclareDefense_GameEnds_ReturnsTrue()
    {
        // Arrange
        var seededDeckA = new List<Engine.BattleCard>
        {
            TestCards.Card(attack: 100),
            TestCards.Card(attack: 100),
            TestCards.Card(attack: 100),
            TestCards.Card(attack: 100),
            TestCards.Card(attack: 100),
        };
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers(seededDeckA);
        Assert.NotNull(gameSession.PlayerA);
        var playerA = gameSession.PlayerA;

        // Act
        gameSession.BeginGame(Engine.PlayerId.PlayerA);
        gameSession.DeclareAttack(playerA.Hand[0]);
        var result = gameSession.DeclareDefense(null);

        // Assert
        Assert.True(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(Engine.PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Null( gameSession.PendingHandOffTo);
    }
}
