namespace CardGame.Engine.Tests;

public class PlayerStateTests
{
    private readonly PlayerState _player;

    private static List<BattleCard> CreateDummyDeck(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new BattleCard($"card{i}", Element.Scor, 1, 1, 1, 1))
            .ToList();
    }

    public PlayerStateTests()
    {
        _player = new PlayerState(CreateDummyDeck(40));
    }

    [Fact]
    public void Constructor_ShufflesDeck()
    {
        // Arrange
        var deck = CreateDummyDeck(40);
        var seededRandom = new Random(42);

        // Act
        var player = new PlayerState(deck, seededRandom);

        // Assert
        Assert.NotEqual(deck, player.DrawPile);
        var sortedOriginal = deck.OrderBy(c => c.Name).ToList();
        var sortedShuffled = player.DrawPile.OrderBy(c => c.Name).ToList();
        Assert.Equal(sortedOriginal, sortedShuffled);
    }

    [Fact]
    public void TakeDamage_ReducesHealthByAmount_ReturnsDamageTaken()
    {
        // Arrange
        var damage = 2;

        // Act
        var result = _player.TakeDamage(damage);

        // Assert
        Assert.Equal(8, _player.CurrentHealth);
        Assert.Equal(2, result);
    }

    [Fact]
    public void TakeDamage_ExceedsStartingHealth_ClampsAtZero()
    {
        // Arrange
        var damage = 12;

        // Act
        var result = _player.TakeDamage(damage);

        // Assert
        Assert.Equal(0, _player.CurrentHealth);
        Assert.Equal(10, result);
    }

    [Fact]
    public void TakeDamage_ExceedsCurrentHealth_ClampsAtZero()
    {
        // Arrange

        // Act
        _player.TakeDamage(5);
        var result = _player.TakeDamage(10);

        // Assert
        Assert.Equal(0, _player.CurrentHealth);
        Assert.Equal(5, result);
    }

    [Fact]
    public void IncreaseMaxEnergy_IncreasesEnergyByAmount_ReturnsEnergyIncreaseAmount()
    {
        // Arrange

        // Act
        var result = _player.IncreaseMaxEnergy(1);

        // Assert
        Assert.Equal(2, _player.MaxEnergy);
        Assert.Equal(1, _player.CurrentEnergy);
        Assert.Equal(1, result);
    }

    [Fact]
    public void GainEnergy_IncreasesEnergyByAmount_ReturnsEnergyIncreaseAmount()
    {
        // Arrange

        // Act
        _player.IncreaseMaxEnergy(1);
        var result = _player.GainEnergy(1);

        // Assert
        Assert.Equal(2, _player.CurrentEnergy);
        Assert.Equal(1, result);
    }

    [Fact]
    public void GainEnergy_ExceedsMaxEnergy_ReturnsEnergyIncreaseAmount()
    {
        // Arrange

        // Act
        _player.IncreaseMaxEnergy(1);
        var result = _player.GainEnergy(5);

        // Assert
        Assert.Equal(2, _player.CurrentEnergy);
        Assert.Equal(1, result);
    }

    [Fact]
    public void TryUseEnergy_ReducesEnergyByAmount_ReturnsTrue()
    {
        // Arrange

        // Act
        var result = _player.TryUseEnergy(1);

        // Assert
        Assert.Equal(0, _player.CurrentEnergy);
        Assert.True(result);
    }

    [Fact]
    public void TryUseEnergy_FailsToReduceEnergy_ReturnsFalse()
    {
        // Arrange

        // Act
        var result = _player.TryUseEnergy(5);

        // Assert
        Assert.Equal(1, _player.CurrentEnergy);
        Assert.False(result);
    }

    [Fact]
    public void ReplenishEnergy_SetsCurrentEnergyToMaxEnergy()
    {
        // Arrange

        // Act
        _player.IncreaseMaxEnergy(2);
        _player.TryUseEnergy(1);
        _player.ReplenishEnergy();

        // Assert
        Assert.Equal(3, _player.CurrentEnergy);
        Assert.Equal(_player.MaxEnergy, _player.CurrentEnergy);
    }

    [Fact]
    public void DrawCard_AddSingleCardFromDrawPileToHand_ReturnsTrue()
    {
        // Arrange

        // Act
        bool result = _player.DrawCard();

        // Assert
        Assert.Single(_player.Hand);
        Assert.Equal(39, _player.DrawPile.Count);
        Assert.True(result);
    }

    [Fact]
    public void DrawCards_AddMultipleCardsFromDrawPileToHand_ReturnsTrue()
    {
        // Arrange

        // Act
        bool result = _player.DrawCards(5);

        // Assert
        Assert.Equal(5, _player.Hand.Count);
        Assert.Equal(35, _player.DrawPile.Count);
        Assert.True(result);
    }

    [Fact]
    public void DrawCards_DrawMoreCardsThanInPile_ReturnsFalseAndDrawsRemaining()
    {
        // Arrange

        // Act
        bool result = _player.DrawCards(43);

        // Assert
        Assert.Equal(40, _player.Hand.Count);
        Assert.Empty(_player.DrawPile);
        Assert.False(result);
    }

    [Fact]
    public void DiscardCard_MovesCardFromHandToDiscardPile_ReturnsTrue()
    {
        // Arrange
        _player.DrawCards(5);
        var card = _player.Hand[0];

        // Act
        bool result = _player.DiscardCard(card);

        //Assert
        Assert.Equal(4, _player.Hand.Count);
        Assert.Single(_player.DiscardPile);
        Assert.Equal(card, _player.DiscardPile[0]);
        Assert.True(result);
    }

    [Fact]
    public void DiscardCard_HandIsEmpty_ReturnsFalse()
    {
        // Arrange
        var card = _player.DrawPile[0];

        // Act
        bool result = _player.DiscardCard(card);

        //Assert
        Assert.Empty(_player.Hand);
        Assert.Empty(_player.DiscardPile);
        Assert.False(result);
    }
}