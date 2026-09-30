namespace CardGame.Web.Pages;

using CardGame.DeckManagement;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class DeckEditor
{
    private Deck _deck = new Deck();
    private string? _catalogCardDetails;
    private string? _deckLabel;
    private List<CardTemplate> _catalog = [];

    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    public HttpClient Http { get; set; } = default!;

    [Inject]
    public DeckStorageService Decks { get; set; } = default!;

    [Inject]
    public NavigationManager Nav { get; set; } = default!;

    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _catalog = await CardCatalog.LoadAsync(Http, "data/cards.json");

        var decks = await Decks.LoadDecksAsync();
        var deck = decks.FirstOrDefault(d => d.Id == Id);
        if (deck == null)
        {
            NavigateToDeckList();
            return;
        }

        _deck = deck;
        _deckLabel = deck.Label;
    }

    public void OnAddCard(string name)
    {
        if (!_deck.AddCard(new DeckCard(name, 1)))
        {
            Console.WriteLine($"Couldn't add {name} to the deck");
        }
    }

    public void OnRemoveCard(string name)
    {
        if (!_deck.RemoveCard(new DeckCard(name, 1)))
        {
            Console.WriteLine($"Couldn't remove {name} from the deck");
        }
    }

    public CardTemplate? GetCardDetails(string? name)
    {
        return name == null ? null : _catalog.FirstOrDefault(c => c.Name == name);
    }

    public void NavigateToDeckList()
    {
        Nav.NavigateTo("deck-builder");
    }

    public async Task HandleSubmitAsync()
    {
        if (_deckLabel != null)
        {
            _deck.Label = _deckLabel;
            await Decks.SaveDeckAsync(_deck);
        }
    }

    public async Task DeleteDeckAsync()
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", $"Delete \"{_deck.Label}\"? This can't be undone.");
        if (!confirmed)
        {
            return;
        }

        await Decks.DeleteDeckAsync(_deck.Id);
        NavigateToDeckList();
    }
}
