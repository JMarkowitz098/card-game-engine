using System.Net;
using System.Text;
using CardGame.Engine;
using CardGame.Engine.Tests;

namespace CardGame.Web.Tests;

public class GameSessionServiceTests
{
    private sealed class FakeCardCatalogHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            const string json = """
                [
                    { "Name": "Spark", "Cost": 1, "Charge": 1, "Attack": 2, "Defense": 1, "ImagePath": null },
                    { "Name": "Blaze", "Cost": 1, "Charge": 1, "Attack": 1, "Defense": 2, "ImagePath": null },
                    { "Name": "Spicy", "Cost": 2, "Charge": 2, "Attack": 1, "Defense": 1, "ImagePath": null },
                    { "Name": "Hot", "Cost": 2, "Charge": 0, "Attack": 4, "Defense": 3, "ImagePath": null },
                    { "Name": "Fire", "Cost": 2, "Charge": 0, "Attack": 3, "Defense": 4, "ImagePath": null }
                ]
                """;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }

    private static GameSessionService CreateGameSession()
    {
        var httpClient = new HttpClient(new FakeCardCatalogHandler())
        {
            BaseAddress = new Uri("http://localhost/"),
        };
        return new GameSessionService(httpClient);
    }

    [Fact]
    public async Task SetupPlayers_CreatesPlayerStatesAndDrawsFiveCardsEach()
    {
        // Arrange
        var gameSession = CreateGameSession();
        var changedRaised = false;
        gameSession.Changed += () => changedRaised = true;

        // Act
        await gameSession.SetupPlayers();

        // Assert
        Assert.NotNull(gameSession.PlayerA);
        Assert.NotNull(gameSession.PlayerB);
        Assert.Equal(5, gameSession.PlayerA.Hand.Count);
        Assert.Equal(5, gameSession.PlayerB.Hand.Count);
        Assert.True(changedRaised);
    }

    [Fact]
    public async Task Mulligan_PlayersCanMulligan()
    {
        // Arrange
        var gameSession = CreateGameSession();
        var changedRaised = false;
        await gameSession.SetupPlayers();
        Assert.NotNull(gameSession.PlayerA);
        var originalHand = gameSession.PlayerA.Hand.ToList();
        Assert.Equal(PlayerId.PlayerA, gameSession.PendingHandOffTo);

        // Act
        gameSession.Changed += () => changedRaised = true;
        gameSession.Mulligan(PlayerId.PlayerA, willMulligan: true);

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
        var gameSession = CreateGameSession();
        var changedRaised = false;
        gameSession.Changed += () => changedRaised = true;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            gameSession.Mulligan(PlayerId.PlayerA, willMulligan: true)
        );

        // Assert
        Assert.Equal(
            "Cannot Mulligan before creating Players or after Game has been started",
            ex.Message
        );
        Assert.False(changedRaised);
    }

    [Fact]
    public async Task Mulligan_ThrowsIfGameHasStarted()
    {
        // Arrange
        var gameSession = CreateGameSession();
        await gameSession.SetupPlayers();
        gameSession.BeginGame(PlayerId.PlayerA);
        var changedRaised = false;
        gameSession.Changed += () => changedRaised = true;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            gameSession.Mulligan(PlayerId.PlayerA, willMulligan: true)
        );

        // Assert
        Assert.Equal(
            "Cannot Mulligan before creating Players or after Game has been started",
            ex.Message
        );
        Assert.False(changedRaised);
    }

    [Fact]
    public async Task BeginGame_CreatesGameAndSetsActivePlayer()
    {
        // Arrange
        var gameSession = CreateGameSession();
        await gameSession.SetupPlayers();
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
        var gameSession = CreateGameSession();
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
    public async Task DeclareAttack_ActivePlayerChanges_ReturnsTrue()
    {
        // Arrange
        var gameSession = CreateGameSession();
        await gameSession.SetupPlayers();
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
    public async Task DeclareAttack_InvalidChoice_ReturnsFalse()
    {
        // Arrange
        var gameSession = CreateGameSession();
        await gameSession.SetupPlayers();
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
    public async Task DeclareAttack_RealCard_HandsOffToDefender()
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
        var gameSession = CreateGameSession();
        await gameSession.SetupPlayers(seededDeckA);
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
    public async Task DeclareDefense_InvalidChoiceAfterValidAttack_StaysOnDefender()
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
        var gameSession = CreateGameSession();
        await gameSession.SetupPlayers(seededDeckA);
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
    public async Task DeclareDefense_ActivePlayerStaysSame_ReturnsTrue()
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
        var gameSession = CreateGameSession();
        await gameSession.SetupPlayers(seededDeckA);
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
    public async Task DeclareDefense_InvalidChoice_ReturnsFalse()
    {
        // Arrange
        var gameSession = CreateGameSession();
        await gameSession.SetupPlayers();
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
    public async Task DeclareDefense_GameEnds_ReturnsTrue()
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
        var gameSession = CreateGameSession();
        await gameSession.SetupPlayers(seededDeckA);
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
    public async Task ConfirmReady_SetsPendingHandOffToNull()
    {
        // Arrange
        var gameSession = CreateGameSession();
        await gameSession.SetupPlayers();
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
