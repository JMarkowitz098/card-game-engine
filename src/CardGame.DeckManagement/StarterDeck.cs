using CardGame.Engine;

namespace CardGame.DeckManagement;

public static class StarterDeck
{
    private static readonly List<(
        string Name,
        int Cost,
        int Charge,
        int Attack,
        int Defense,
        int Count
    )> Templates = new()
    {
        ("Spark  ", 1, 1, 2, 1, 4),
        ("Blaze  ", 1, 1, 1, 2, 4),
        ("Spicy  ", 2, 2, 1, 1, 4),
        ("Hot    ", 2, 0, 4, 3, 2),
        ("Fire   ", 2, 0, 3, 4, 2),
        ("Scorch ", 3, 0, 5, 0, 2),
        ("Ember  ", 3, 0, 0, 5, 2),
        ("Energy ", 4, 3, 0, 0, 4),
        ("Inferno", 4, 0, 6, 6, 2),
        ("Volcano", 5, 0, 0, 8, 2),
        ("Ash    ", 5, 0, 6, 0, 2),
        ("Heat   ", 6, 0, 2, 2, 2),
        ("Lava   ", 6, 0, 6, 6, 2),
        ("Wildfire", 8, 0, 8, 8, 2),
    };

    public static List<BattleCard> Create()
    {
        return Templates
            .SelectMany(t =>
                Enumerable.Repeat(
                    new BattleCard(t.Name, t.Cost, t.Charge, t.Attack, t.Defense),
                    t.Count
                )
            )
            .ToList();
    }
}
