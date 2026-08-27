using CardGame.Engine;
using CardGame.Engine.Tests;

namespace CardGame.Web.Tests;

public class GameSessionServiceTests
{
    [Fact]
    public void SetupPlayers_CreatesPlayerStatesAndDrawsFiveCardsEach()
    {
        // Arrange
        var gameSession = new GameSessionService();
        var changedRaised = false;
        gameSession.Changed += () => changedRaised = true;

        // Act
        gameSession.SetupPlayers();

        // Assert
        Assert.NotNull(gameSession.PlayerA);
        Assert.NotNull(gameSession.PlayerB);
        Assert.Equal(5, gameSession.PlayerA.Hand.Count);
        Assert.Equal(5, gameSession.PlayerB.Hand.Count);
        Assert.True(changedRaised);
    }

    [Fact]
    public void Mulligan_PlayersCanMulligan()
    {
        // Arrange
        var gameSession = new GameSessionService();
        var changedRaised = false;
        gameSession.SetupPlayers();
        Assert.NotNull(gameSession.PlayerA);
        var originalHand = gameSession.PlayerA.Hand.ToList();
        Assert.Equal(PlayerId.PlayerA, gameSession.PendingHandOffTo);

        // Act
        gameSession.Changed += () => changedRaised = true;
        gameSession.Mulligan(PlayerId.PlayerA);

        // Assert
        Assert.Equal(PlayerId.PlayerB, gameSession.PendingHandOffTo);
        Assert.NotEqual(originalHand, gameSession.PlayerA.Hand);
        Assert.Equal(5, gameSession.PlayerA.Hand.Count);
        Assert.True(changedRaised);
    }

    [Fact]
    public void Mulligan_ThrowsIfPlayersAreNotCreated()
    {
        // Arrange
        var gameSession = new GameSessionService();
        var changedRaised = false;
        gameSession.Changed += () => changedRaised = true;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            gameSession.Mulligan(PlayerId.PlayerA)
        );

        // Assert
        Assert.Equal(
            "Cannot Mulligan before creating Players or after Game has been started",
            ex.Message
        );
        Assert.False(changedRaised);
    }

    [Fact]
    public void Mulligan_ThrowsIfGameHasStarted()
    {
        // Arrange
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers();
        gameSession.BeginGame(PlayerId.PlayerA);
        var changedRaised = false;
        gameSession.Changed += () => changedRaised = true;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            gameSession.Mulligan(PlayerId.PlayerA)
        );

        // Assert
        Assert.Equal(
            "Cannot Mulligan before creating Players or after Game has been started",
            ex.Message
        );
        Assert.False(changedRaised);
    }

    [Fact]
    public void BeginGame_CreatesGameAndSetsActivePlayer()
    {
        // Arrange
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers();
        var changedRaised = false;
        gameSession.Changed += () => changedRaised = true;

        // Act
        gameSession.BeginGame(PlayerId.PlayerA);

        // Assert
        Assert.NotNull(gameSession.Game);
        Assert.Equal(PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(PlayerId.PlayerA, gameSession.PendingHandOffTo);
        Assert.True(changedRaised);
    }

    [Fact]
    public void BeginGame_ThrowsIfPlayersAreNotCreated()
    {
        // Arrange
        var gameSession = new GameSessionService();
        var changedRaised = false;
        gameSession.Changed += () => changedRaised = true;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            gameSession.BeginGame(PlayerId.PlayerA)
        );

        // Assert
        Assert.Equal("Cannot begin the game before both players have been set up.", ex.Message);
        Assert.False(changedRaised);
    }

    [Fact]
    public void DeclareAttack_ActivePlayerChanges_ReturnsTrue()
    {
        // Arrange
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers();
        var changedRaised = false;

        // Act
        gameSession.BeginGame(PlayerId.PlayerA);
        gameSession.Changed += () => changedRaised = true;
        var result = gameSession.DeclareAttack(null);

        // Assert
        Assert.True(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(PlayerId.PlayerB, gameSession.Game.ActivePlayer);
        Assert.Equal(PlayerId.PlayerB, gameSession.PendingHandOffTo);
        Assert.True(changedRaised);
    }

    [Fact]
    public void DeclareAttack_InvalidChoice_ReturnsFalse()
    {
        // Arrange
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers();
        var invalidCard = TestCards.Card();
        var changedRaised = false;

        // Act
        gameSession.BeginGame(PlayerId.PlayerA);
        gameSession.Changed += () => changedRaised = true;
        var result = gameSession.DeclareAttack(invalidCard);

        // Assert
        Assert.False(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(PlayerId.PlayerA, gameSession.PendingHandOffTo);
        Assert.False(changedRaised);
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
        var changedRaised = false;

        // Act
        gameSession.BeginGame(PlayerId.PlayerA);
        gameSession.Changed += () => changedRaised = true;
        var result = gameSession.DeclareAttack(playerA.Hand[0]);

        // Assert
        Assert.True(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(PlayerId.PlayerB, gameSession.PendingHandOffTo);
        Assert.True(changedRaised);
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
        var changedRaised = false;

        // Act
        gameSession.BeginGame(PlayerId.PlayerA);
        gameSession.DeclareAttack(playerA.Hand[0]);
        gameSession.Changed += () => changedRaised = true;
        var result = gameSession.DeclareDefense(invalidDefenseCard);

        // Assert
        Assert.False(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(PlayerId.PlayerB, gameSession.PendingHandOffTo);
        Assert.False(changedRaised);
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
        var changedRaised = false;

        // Act
        gameSession.BeginGame(PlayerId.PlayerA);
        gameSession.DeclareAttack(playerA.Hand[0]);
        gameSession.Changed += () => changedRaised = true;
        var result = gameSession.DeclareDefense(null);

        // Assert
        Assert.True(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(PlayerId.PlayerA, gameSession.PendingHandOffTo);
        Assert.True(changedRaised);
    }

    [Fact]
    public void DeclareDefense_InvalidChoice_ReturnsFalse()
    {
        // Arrange
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers();
        var changedRaised = false;

        // Act
        gameSession.BeginGame(PlayerId.PlayerA);
        gameSession.Changed += () => changedRaised = true;
        var result = gameSession.DeclareDefense(null); // Can't defend before an attack

        // Assert
        Assert.False(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Equal(PlayerId.PlayerA, gameSession.PendingHandOffTo);
        Assert.False(changedRaised);
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
        var changedRaised = false;

        // Act
        gameSession.BeginGame(PlayerId.PlayerA);
        gameSession.DeclareAttack(playerA.Hand[0]);
        gameSession.Changed += () => changedRaised = true;
        var result = gameSession.DeclareDefense(null);

        // Assert
        Assert.True(result);
        Assert.NotNull(gameSession.Game);
        Assert.Equal(PlayerId.PlayerA, gameSession.Game.ActivePlayer);
        Assert.Null(gameSession.PendingHandOffTo);
        Assert.True(changedRaised);
    }

    [Fact]
    public void ConfirmReady_SetsPendingHandOffToNull()
    {
        // Arrange
        var gameSession = new GameSessionService();
        gameSession.SetupPlayers();
        var changedRaised = false;

        // Act
        gameSession.BeginGame(PlayerId.PlayerA);
        gameSession.DeclareAttack(null);
        var before = gameSession.PendingHandOffTo;
        gameSession.Changed += () => changedRaised = true;
        gameSession.ConfirmReady();

        // Assert
        Assert.NotNull(before);
        Assert.Null(gameSession.PendingHandOffTo);
        Assert.True(changedRaised);
    }
}
