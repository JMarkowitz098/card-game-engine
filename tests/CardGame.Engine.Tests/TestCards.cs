namespace CardGame.Engine.Tests;

public static class TestCards
{
    public static BattleCard Card(
        string name = "card",
        int cost = 1,
        int charge = 1,
        int attack = 2,
        int defense = 1
    )
    {
        return new BattleCard(name, cost, charge, attack, defense);
    }
}
