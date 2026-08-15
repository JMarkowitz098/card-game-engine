namespace CardGame.Engine.Tests;

public class CombatResolverTests
{
    [Fact]
    public void ResolveBattle_AttackGreaterThanDefense_ReturnsDamageDifference()
    {
        // Arrange
        var attackingCard = new BattleCard("spark", Element.Scor, 1, 1, 2, 1);
        var defendingCard = new BattleCard("breeze", Element.Eth, 1, 1, 2, 1);

        // Act
        int result = CombatResolver.ResolveBattle(attackingCard, defendingCard);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void ResolveBattle_AttackLessThanDefense_Returns0()
    {
        // Arrange
        var attackingCard = new BattleCard("spark", Element.Scor, 1, 1, 2, 1);
        var defendingCard = new BattleCard("breeze", Element.Eth, 1, 1, 2, 3);

        // Act
        int result = CombatResolver.ResolveBattle(attackingCard, defendingCard);

        // Assert
        Assert.Equal(0, result);
    }
    [Fact]
    public void ResolveBattle_AttackWithNoDefense_ReturnsDamage()
    {
        // Arrange
        var attackingCard = new BattleCard("spark", Element.Scor, 1, 1, 2, 1);
        BattleCard? defendingCard = null;

        // Act
        int result = CombatResolver.ResolveBattle(attackingCard, defendingCard);

        // Assert
        Assert.Equal(2, result);
    }
}