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
        ("Spark", Element.Scor, 1, 1, 2, 1, 4),
        ("Blaze", Element.Scor, 1, 2, 1, 1, 4),
        ("Scorch", Element.Scor, 2, 2, 2, 2, 4),
        ("Inferno", Element.Scor, 4, 2, 4, 2, 4),
        ("Wildfire", Element.Scor, 8, 0, 10, 0, 4),
        ("Ember", Element.Scor, 3, 2, 3, 3, 4),
        ("Ash", Element.Scor, 5, 2, 2, 8, 4),
        ("Fire", Element.Scor, 2, 2, 0, 4, 4),
        ("Heat", Element.Scor, 4, 4, 2, 2, 4),
        ("Hot", Element.Scor, 1, 0, 3, 3, 4),
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
