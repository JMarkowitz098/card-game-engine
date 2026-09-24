namespace CardGame.DeckManagement.Tests;

public class DeckTests
{
    [Fact]
    public void CreateEmptyDeck()
    {
        // Act
        var deck = new Deck();

        // Assert
        Assert.NotNull(deck);
        Assert.Empty(deck.Cards);
    }

    [Fact]
    public void AddCard_AddsCardToDeck_ReturnsTrue()
    {
        // Arrange
        var deck = new Deck();

        // Act
        var result = deck.AddCard(new DeckCard("Spark", 3));

        // Assert
        Assert.Equal(Deck.MaxCardCopies, deck.Cards["Spark"]);
        Assert.True(result);
    }

    [Fact]
    public void AddCard_FailsWhenCardsAddedExceedsCopyLimit_ReturnsFalse()
    {
        // Arrange
        var deck = new Deck();

        // Act
        var result = deck.AddCard(new DeckCard("Spark", 4));

        // Assert
        Assert.Equal(Deck.MaxCardCopies, deck.Cards["Spark"]);
        Assert.False(result);
    }

    [Fact]
    public void AddCard_DoesNotAddKeyIfNumberIsLessThanOne_ReturnsFalse()
    {
        // Arrange
        var deck = new Deck();

        // Act
        var result1 = deck.AddCard(new DeckCard("Spark", -2));
        var result2 = deck.AddCard(new DeckCard("Fire", 0));

        // Assert
        Assert.Empty(deck.Cards);
        Assert.False(result1);
        Assert.False(result2);
    }

    [Fact]
    public void RemoveCard_RemovesCardAmountFromDeck_ReturnsTrue()
    {
        // Arrange
        var deck = new Deck();
        deck.AddCard(new DeckCard("Spark", Deck.MaxCardCopies));
        deck.AddCard(new DeckCard("Fire", 3));

        // Act
        Assert.Equal(3, deck.Cards["Spark"]);
        Assert.Equal(3, deck.Cards["Fire"]);
        var result1 = deck.RemoveCard(new DeckCard("Spark", Deck.MaxCardCopies));
        var result2 = deck.RemoveCard(new DeckCard("Fire", 1));

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.False(deck.Cards.ContainsKey("Spark"));
        Assert.Equal(2, deck.Cards["Fire"]);
    }

    [Fact]
    public void RemoveCard_FailsWhenRemovingMoreCardsThanInDeck_ReturnsFalse()
    {
        // Arrange
        var deck = new Deck();
        deck.AddCard(new DeckCard("Spark", 1));

        // Act
        Assert.Equal(1, deck.Cards["Spark"]);
        var result = deck.RemoveCard(new DeckCard("Spark", 3));

        // Assert
        Assert.False(result);
        Assert.Empty(deck.Cards);
    }

    [Fact]
    public void AddCards_AddsCardsInListToDeck_ReturnsTrue()
    {
        // Arrange
        var deck = new Deck();
        var cards = new List<DeckCard> { new("Spark", 3), new("Fire", 3) };

        // Act
        var result = deck.AddCards(cards);

        // Assert
        Assert.Equal(3, deck.Cards["Spark"]);
        Assert.Equal(3, deck.Cards["Fire"]);
        Assert.True(result);
    }

    [Fact]
    public void AddCards_AddsPossibleCardsIntoDeck_ReturnsFalse()
    {
        // Arrange
        var deck = new Deck();
        var cards = new List<DeckCard> { new("Spark", 3), new("Fire", 5) };

        // Act
        var result = deck.AddCards(cards);

        // Assert
        Assert.Equal(3, deck.Cards["Spark"]);
        Assert.Equal(Deck.MaxCardCopies, deck.Cards["Fire"]);
        Assert.False(result);
    }
}
