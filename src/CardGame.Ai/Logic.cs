using CardGame.Engine;

namespace CardGame.Ai;

public class Logic
{
    public static BattleCard? DeclareAttack(List<BattleCard> hand, int energy)
    {
        return hand
            .Where(card => card.Cost <= energy)
            .OrderByDescending(card => card.Attack)
            .ThenBy(card => card.Cost)
            .ThenByDescending(card => card.Charge)
            .ThenBy(card => card.Defense)
            .FirstOrDefault();
    }

    public static BattleCard? DeclareDefense(List<BattleCard> hand, int energy, int incomingAttack)
    {
        return hand
            .Where(card => card.Cost <= energy)
            .OrderByDescending(card => Math.Min(card.Defense, incomingAttack))
            .ThenBy(card => card.Cost)
            .FirstOrDefault();
    }
}
