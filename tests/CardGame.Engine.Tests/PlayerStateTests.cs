namespace CardGame.Engine.Tests;

public class PlayerStateTests
{
    [Fact]
    public void TakeDamage_ReducesHealthByAmount_ReturnsDamageTaken()
    {
        // Arrange
        var player = new PlayerState();
        var damage = 2;

        // Act
        var result = player.TakeDamage(damage);

        // Assert
        Assert.Equal(8, player.CurrentHealth);
        Assert.Equal(2, result);
    }

    [Fact]
    public void TakeDamage_ExceedsStartingHealth_ClampsAtZero()
    {
        // Arrange
        var player = new PlayerState();
        var damage = 12;

        // Act
        var result = player.TakeDamage(damage);

        // Assert
        Assert.Equal(0, player.CurrentHealth);
        Assert.Equal(10, result);
    }

    [Fact]
    public void TakeDamage_ExceedsCurrentHealth_ClampsAtZero()
    {
        // Arrange
        var player = new PlayerState();

        // Act
        player.TakeDamage(5);
        var result = player.TakeDamage(10);

        // Assert
        Assert.Equal(0, player.CurrentHealth);
        Assert.Equal(5, result);
    }

    [Fact]
    public void IncreaseMaxEnergy_IncreasesEnergyByAmount_ReturnsEnergyIncreaseAmount()
    {
        // Arrange
        var player = new PlayerState();

        // Act
        var result = player.IncreaseMaxEnergy(1);

        // Assert
        Assert.Equal(2, player.MaxEnergy);
        Assert.Equal(1, player.CurrentEnergy);
        Assert.Equal(1, result);
    }

    [Fact]
    public void GainEnergy_IncreasesEnergyByAmount_ReturnsEnergyIncreaseAmount()
    {
        // Arrange
        var player = new PlayerState();

        // Act
        player.IncreaseMaxEnergy(1);
        var result = player.GainEnergy(1);

        // Assert
        Assert.Equal(2, player.CurrentEnergy);
        Assert.Equal(1, result);
    }

    [Fact]
    public void GainEnergy_ExceedsMaxEnergy_ReturnsEnergyIncreaseAmount()
    {
        // Arrange
        var player = new PlayerState();

        // Act
        player.IncreaseMaxEnergy(1);
        var result = player.GainEnergy(5);

        // Assert
        Assert.Equal(2, player.CurrentEnergy);
        Assert.Equal(1, result);
    }

    [Fact]
    public void TryUseEnergy_ReducesEnergyByAmount_ReturnsTrue()
    {
        // Arrange
        var player = new PlayerState();

        // Act
        var result = player.TryUseEnergy(1);

        // Assert
        Assert.Equal(0, player.CurrentEnergy);
        Assert.True(result);
    }

    [Fact]
    public void TryUseEnergy_FailsToReduceEnergy_ReturnsFalse()
    {
        // Arrange
        var player = new PlayerState();

        // Act
        var result = player.TryUseEnergy(5);

        // Assert
        Assert.Equal(1, player.CurrentEnergy);
        Assert.False(result);
    }

    [Fact]
    public void ReplenishEnergy_SetsCurrentEnergyToMaxEnergy()
    {
        // Arrange
        var player = new PlayerState();

        // Act
        player.IncreaseMaxEnergy(2);
        player.TryUseEnergy(1);
        player.ReplenishEnergy();

        // Assert
        Assert.Equal(3, player.CurrentEnergy);
        Assert.Equal(player.MaxEnergy, player.CurrentEnergy);
    }
}