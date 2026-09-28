namespace CardGame.Web.Pages;

using CardGame.DeckManagement;
using CardGame.Engine;
using Microsoft.AspNetCore.Components;

public partial class DeckBuilder
{
    private readonly Deck _deck = new Deck();
    private string _catalogCardDetails;
    private readonly HttpClient _httpClient;
    private List<CardTemplate> _catalog = [];

    [Inject]
    public HttpClient Http { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _catalog = await CardCatalog.LoadAsync(Http, "data/cards.json");
        _deck.Label = "Default";
    }

    public void OnAddCard(string name)
    {
        _deck.AddCard(new DeckCard(name, 1));
    }

    public void OnRemoveCard(string name)
    {
        _deck.RemoveCard(new DeckCard(name, 1));
    }

    public CardTemplate? GetCardDetails(string name)
    {
        return name == null ? null : _catalog.FirstOrDefault(c => c.Name == name);
    }

    public void HandleSubmit()
    {
        Console.WriteLine($"You created the {_deck.Label} deck");
        foreach (var card in _deck.Cards)
        {
            Console.WriteLine($"{card.Key}: {card.Value}");
        }
    }
}
