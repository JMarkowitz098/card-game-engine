using CardGame.Engine;

namespace CardGame.Cli;

public static class StarterDeck
{
    private static readonly List<(
        string Name,
        Element Element,
        int Cost,
        int Charge,
        int Attack,
        int Defense,
        int Count
    )> Templates = new()
    {
        ("Spark  ", Element.Scor, 1, 1, 2, 1, 4),
        ("Blaze  ", Element.Scor, 1, 1, 1, 2, 4),
        ("Spicy  ", Element.Scor, 2, 2, 1, 1, 4),
        ("Hot    ", Element.Scor, 2, 0, 4, 3, 2),
        ("Fire   ", Element.Scor, 2, 0, 3, 4, 2),
        ("Scorch ", Element.Scor, 3, 0, 5, 0, 2),
        ("Ember  ", Element.Scor, 3, 0, 0, 5, 2),
        ("Energy ", Element.Scor, 4, 3, 0, 0, 4),
        ("Inferno", Element.Scor, 4, 0, 6, 6, 2),
        ("Volcano", Element.Scor, 5, 0, 0, 8, 2),
        ("Ash    ", Element.Scor, 5, 0, 6, 0, 2),
        ("Heat   ", Element.Scor, 6, 0, 2, 2, 2),
        ("Lava   ", Element.Scor, 6, 0, 6, 6, 2),
        ("Wildfire", Element.Scor, 8, 0, 8, 8, 2),
    };

    public static List<BattleCard> Create()
    {
        return Templates
            .SelectMany(t =>
                Enumerable.Repeat(
                    new BattleCard(t.Name, t.Element, t.Cost, t.Charge, t.Attack, t.Defense),
                    t.Count
                )
            )
            .ToList();
    }
}
