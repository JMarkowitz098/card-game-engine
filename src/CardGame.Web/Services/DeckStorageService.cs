using System.Text.Json;
using CardGame.DeckManagement;
using Microsoft.JSInterop;

namespace CardGame.Web;

public class DeckStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string DecksKey = "decks";
    private List<Deck> _decks = [];

    public DeckStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<List<Deck>> LoadDecksAsync()
    {
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", DecksKey);
        if (json == null)
        {
            _decks = [];
            return _decks;
        }

        try
        {
            _decks = JsonSerializer.Deserialize<List<Deck>>(json) ?? [];
        }
        catch (JsonException)
        {
            _decks = [];
        }

        return _decks;
    }

    public async Task SaveDecksAsync()
    {
        var json = JsonSerializer.Serialize(_decks);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", DecksKey, json);
    }

    public async Task SaveDeckAsync(Deck deck)
    {
        await LoadDecksAsync(); // In case someone navigated directly to the edit page
        var existingIndex = _decks.FindIndex(d => d.Id == deck.Id);
        if (existingIndex >= 0)
        {
            _decks[existingIndex] = deck;
        }
        else
        {
            _decks.Add(deck);
        }

        await SaveDecksAsync();
    }

    public async Task DeleteDeckAsync(Guid id)
    {
        await LoadDecksAsync(); // In case someone navigated directly to the edit page
        _decks.RemoveAll(d => d.Id == id);
        await SaveDecksAsync();
    }
}
