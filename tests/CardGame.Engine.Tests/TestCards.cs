namespace CardGame.Engine.Tests;

public static class TestCards
{
    public static BattleCard Card(
        string name = "card",
        Element element = Element.Scor,
        int cost = 1,
        int charge = 1,
        int attack = 2,
        int defense = 1
    )
    {
        return new BattleCard(name, element, cost, charge, attack, defense);
    }
}
