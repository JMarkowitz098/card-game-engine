using System.Text.Json.Serialization;
using CardGame.Engine;

namespace CardGame.DeckManagement;

public class Deck
{
    public const int MaxCardCopies = 3;
    public const int MinDeckSize = 40;

    public Guid Id { get; } = Guid.NewGuid();
    public string Label { get; set; } = "New Deck";
    private readonly Dictionary<string, int> _cards = new();
    public IReadOnlyDictionary<string, int> Cards => _cards;

    public Deck() { }

    [JsonConstructor]
    public Deck(Guid id, string label, IReadOnlyDictionary<string, int> cards)
    {
        Id = id;
        Label = label;
        _cards = new Dictionary<string, int>(cards);
    }

    public bool AddCard(DeckCard card)
    {
        if (card.Amount <= 0)
        {
            return false;
        }

        var numCards = card.Amount;
        if (_cards.TryGetValue(card.Name, out var value))
        {
            numCards += value;
            _cards[card.Name] = Math.Clamp(numCards, 0, MaxCardCopies);
        }
        else
        {
            _cards.Add(card.Name, Math.Clamp(numCards, 0, MaxCardCopies));
        }
        return numCards <= MaxCardCopies;
    }

    public bool AddCards(List<DeckCard> listCards)
    {
        var result = true;
        for (var i = 0; i < listCards.Count; i++)
        {
            if (!AddCard(listCards[i]))
            {
                result = false;
            }
        }
        return result;
    }

    public bool RemoveCard(DeckCard card)
    {
        if (_cards.TryGetValue(card.Name, out var value))
        {
            var numCardsLeft = value - card.Amount;
            if (numCardsLeft <= 0)
            {
                _cards.Remove(card.Name);
            }
            else
            {
                _cards[card.Name] = numCardsLeft;
            }
            return numCardsLeft >= 0;
        }
        else
        {
            return false;
        }
    }

    public List<BattleCard> ToBattleCards(IEnumerable<CardTemplate> catalog)
    {
        var result = new List<BattleCard>();
        foreach (var (name, count) in _cards)
        {
            var template = catalog.FirstOrDefault(c => c.Name == name);
            if (template == null)
            {
                Console.WriteLine($"Skipping \"{name}\" - not found in catalog");
                continue;
            }
            result.AddRange(Enumerable.Repeat(template.ToBattleCard(), count));
        }
        return result;
    }
}
