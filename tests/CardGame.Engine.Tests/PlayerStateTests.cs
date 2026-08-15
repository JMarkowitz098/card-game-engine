namespace CardGame.Engine.Tests;

public class PlayerStateTests
{
    private readonly PlayerState _player;

    public PlayerStateTests()
    {
        _player = new PlayerState();
        // _player = new PlayerState(new List<BattleCard>());
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

    
}