namespace CardGame.Engine;

public static class CombatResolver
{
    public static int ResolveBattle(BattleCard attackingCard, BattleCard defendingCard)
    {
        return Math.Max(0, attackingCard.Attack - defendingCard.Defense);
    }
}