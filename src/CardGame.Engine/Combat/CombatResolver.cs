namespace CardGame.Engine;

public static class CombatResolver
{
    public static int ResolveBattle(BattleCard attackingCard, BattleCard? defendingCard)
    {
        int rawDamage = attackingCard.Attack - (defendingCard?.Defense ?? 0);
        return Math.Max(0, rawDamage);
    }
}