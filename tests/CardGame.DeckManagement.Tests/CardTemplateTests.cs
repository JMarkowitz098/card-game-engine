namespace CardGame.DeckManagement.Tests;

public class CardTemplateTests
{
    [Fact]
    public void ToBattleCard_MapsStats_ReturnsBattleCardWithSameStats()
    {
        // Arrange
        var template = new CardTemplate(
            Name: "Spark",
            Cost: 1,
            Charge: 1,
            Attack: 2,
            Defense: 1,
            ImagePath: "images/spark.png"
        );

        // Act
        var battleCard = template.ToBattleCard();

        // Assert
        Assert.Equal(template.Name, battleCard.Name);
        Assert.Equal(template.Cost, battleCard.Cost);
        Assert.Equal(template.Charge, battleCard.Charge);
        Assert.Equal(template.Attack, battleCard.Attack);
        Assert.Equal(template.Defense, battleCard.Defense);
    }

    [Fact]
    public void ToBattleCard_ImagePathIsNull_StillReturnsBattleCard()
    {
        // Arrange
        var template = new CardTemplate(
            Name: "Blaze",
            Cost: 1,
            Charge: 1,
            Attack: 1,
            Defense: 2,
            ImagePath: null
        );

        // Act
        var battleCard = template.ToBattleCard();

        // Assert
        Assert.Equal(template.Name, battleCard.Name);
    }
}
