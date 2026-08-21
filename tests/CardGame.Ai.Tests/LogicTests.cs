using CardGame.Engine;
using CardGame.Engine.Tests;

namespace CardGame.Ai.Tests;

public class LogicTest
{
    [Fact]
    public void DeclareAttack_ReturnsCard()
    {
        // Arrange
        var hand = new List<BattleCard> { TestCards.Card() };
        var energy = 1;

        // Act
        var result = Logic.DeclareAttack(hand, energy);

        // Assert
        Assert.Equal(hand[0], result);
    }

    [Fact]
    public void DeclareAttack_ReturnsHighestAttackCardThatIsUsable()
    {
        // Arrange
        var usable = TestCards.Card(name: "card2", cost: 2, attack: 2);
        var hand = new List<BattleCard>
        {
            TestCards.Card(name: "card1", cost: 1, attack: 1),
            usable,
            TestCards.Card(name: "card3", cost: 3, attack: 3),
        };
        var energy = 2;

        // Act
        var result = Logic.DeclareAttack(hand, energy);

        // Assert
        Assert.Equal(usable, result);
    }

    [Fact]
    public void DeclareAttack_ReturnsNullIfNoUseableCards()
    {
        // Arrange

        var hand = new List<BattleCard>
        {
            TestCards.Card(name: "card1", cost: 1, attack: 1),
            TestCards.Card(name: "card2", cost: 2, attack: 2),
            TestCards.Card(name: "card3", cost: 3, attack: 3),
        };
        var energy = 0;

        // Act
        var result = Logic.DeclareAttack(hand, energy);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeclareAttack_ReturnsNullHandIsEmpty()
    {
        // Arrange

        var hand = new List<BattleCard>();
        var energy = 3;

        // Act
        var result = Logic.DeclareAttack(hand, energy);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeclareAttack_ReturnsLowestCostIfAttackTie()
    {
        // Arrange
        var cheap = TestCards.Card(name: "cheap", cost: 1, attack: 3);
        var expensive = TestCards.Card(name: "expensive", cost: 3, attack: 3);
        var hand = new List<BattleCard> { expensive, cheap };
        var energy = 3;

        // Act
        var result = Logic.DeclareAttack(hand, energy);

        // Assert
        Assert.Equal(cheap, result);
    }

    [Fact]
    public void DeclareAttack_ReturnsHighestChargeIfAttackAndCostTie()
    {
        // Arrange
        var lowCharge = TestCards.Card(name: "lowCharge", cost: 2, charge: 1, attack: 3);
        var highCharge = TestCards.Card(name: "highCharge", cost: 2, charge: 3, attack: 3);
        var hand = new List<BattleCard> { lowCharge, highCharge };
        var energy = 2;

        // Act
        var result = Logic.DeclareAttack(hand, energy);

        // Assert
        Assert.Equal(highCharge, result);
    }

    [Fact]
    public void DeclareAttack_ReturnsLowestDefenseIfAttackCostAndChargeTie()
    {
        // Arrange
        var highDefense = TestCards.Card(name: "highDefense", cost: 2, charge: 2, attack: 3, defense: 3);
        var lowDefense = TestCards.Card(name: "lowDefense", cost: 2, charge: 2, attack: 3, defense: 1);
        var hand = new List<BattleCard> { highDefense, lowDefense };
        var energy = 2;

        // Act
        var result = Logic.DeclareAttack(hand, energy);

        // Assert
        Assert.Equal(lowDefense, result);
    }

    [Fact]
    public void DeclareAttack_ReturnsFirstFoundCardIfAllStatsSame()
    {
        // Arrange
        var first = TestCards.Card(name: "twin", cost: 2, charge: 2, attack: 3, defense: 2);
        var second = TestCards.Card(name: "twin", cost: 2, charge: 2, attack: 3, defense: 2);
        var hand = new List<BattleCard> { first, second };
        var energy = 2;

        // Act
        var result = Logic.DeclareAttack(hand, energy);

        // Assert
        Assert.Same(first, result);
    }

    [Fact]
    public void DeclareDefense_ReturnsCard()
    {
        // Arrange
        var hand = new List<BattleCard> { TestCards.Card() };
        var energy = 1;
        var incomingAttack = 3;

        // Act
        var result = Logic.DeclareDefense(hand, energy, incomingAttack);

        // Assert
        Assert.Equal(hand[0], result);
    }

    [Fact]
    public void DeclareDefense_ReturnsCardThatReducesTheMostDamage()
    {
        // Arrange
        var usable = TestCards.Card(name: "card2", cost: 2, defense: 2);
        var hand = new List<BattleCard>
        {
            TestCards.Card(name: "card1", cost: 1, defense: 1),
            usable,
            TestCards.Card(name: "card3", cost: 3, defense: 3),
        };
        var energy = 2;
        var incomingAttack = 10;

        // Act
        var result = Logic.DeclareDefense(hand, energy, incomingAttack);

        // Assert
        Assert.Equal(usable, result);
    }

    [Fact]
    public void DeclareDefense_ReturnsLowestCostCardIfDamageReductionTies()
    {
        // Arrange
        var overkill = TestCards.Card(name: "overkill", cost: 3, defense: 5);
        var exactMatch = TestCards.Card(name: "exactMatch", cost: 1, defense: 3);
        var hand = new List<BattleCard> { overkill, exactMatch };
        var energy = 3;
        var incomingAttack = 3;

        // Act
        var result = Logic.DeclareDefense(hand, energy, incomingAttack);

        // Assert
        Assert.Equal(exactMatch, result);
    }

    [Fact]
    public void DeclareDefense_ReturnsNullIfNoUseableCards()
    {
        // Arrange

        var hand = new List<BattleCard>
        {
            TestCards.Card(name: "card1", cost: 1, defense: 1),
            TestCards.Card(name: "card2", cost: 2, defense: 2),
            TestCards.Card(name: "card3", cost: 3, defense: 3),
        };
        var energy = 0;
        var incomingAttack = 5;

        // Act
        var result = Logic.DeclareDefense(hand, energy, incomingAttack);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeclareDefense_ReturnsNullHandIsEmpty()
    {
        // Arrange

        var hand = new List<BattleCard>();
        var energy = 3;
        var incomingAttack = 5;

        // Act
        var result = Logic.DeclareDefense(hand, energy, incomingAttack);

        // Assert
        Assert.Null(result);
    }
}
