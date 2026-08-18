namespace CardGame.Engine.Tests;

public class CombatResolverTests
{
    [Fact]
    public void ResolveBattle_AttackGreaterThanDefense_ReturnsDamageDifference()
    {
        // Arrange
        var attackingCard = TestCards.Card(attack: 2);
        var defendingCard = TestCards.Card(attack: 2);

        // Act
        int result = CombatResolver.ResolveBattle(attackingCard, defendingCard);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void ResolveBattle_AttackLessThanDefense_Returns0()
    {
        // Arrange
        var attackingCard = TestCards.Card(attack: 2);
        var defendingCard = TestCards.Card(attack: 2, defense: 3);

        // Act
        int result = CombatResolver.ResolveBattle(attackingCard, defendingCard);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ResolveBattle_AttackWithNoDefense_ReturnsDamage()
    {
        // Arrange
        var attackingCard = TestCards.Card(attack: 2);
        BattleCard? defendingCard = null;

        // Act
        int result = CombatResolver.ResolveBattle(attackingCard, defendingCard);

        // Assert
        Assert.Equal(2, result);
    }
}
