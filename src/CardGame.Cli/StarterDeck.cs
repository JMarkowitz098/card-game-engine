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
        ("Blaze", Element.Scor, 1, 1, 2, 1, 4),
        ("Scorch", Element.Scor, 1, 1, 2, 1, 4),
        ("Inferno", Element.Scor, 1, 1, 2, 1, 4),
        ("Wildfire", Element.Scor, 1, 1, 2, 1, 4),
        ("Ember", Element.Scor, 1, 1, 2, 1, 4),
        ("Ash", Element.Scor, 1, 1, 2, 1, 4),
        ("Fire", Element.Scor, 1, 1, 2, 1, 4),
        ("", Element.Scor, 1, 1, 2, 1, 4),
        ("Wildfire", Element.Scor, 1, 1, 2, 1, 4),
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
