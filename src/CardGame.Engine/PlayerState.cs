namespace CardGame.Engine;

public class PlayerState
{
    public int CurrentHealth { get; private set; } = 10;
    public int MaxEnergy { get; private set; } = 1;
    public int CurrentEnergy { get; private set; } = 1;
    private readonly List<BattleCard> _hand = new();
    public IReadOnlyList<BattleCard> Hand => _hand;
    private readonly List<BattleCard> _drawPile;
    public IReadOnlyList<BattleCard> DrawPile => _drawPile;
    private readonly List<BattleCard> _discardPile = new();
    public IReadOnlyList<BattleCard> DiscardPile => _discardPile;

    public PlayerState(List<BattleCard> deck)
    {
        _drawPile = new List<BattleCard>(deck);
    }

    public int TakeDamage(int rawDamage)
    {
        var clampedDamage = Math.Clamp(rawDamage, 0, CurrentHealth);
        CurrentHealth -= clampedDamage;
        return clampedDamage;
    }

    public int IncreaseMaxEnergy(int rawEnergy)
    {
        MaxEnergy += rawEnergy;
        return rawEnergy;
    }

    public bool TryUseEnergy(int rawEnergy)
    {
        if (rawEnergy > CurrentEnergy)
        {
            return false;
        }

        CurrentEnergy -= rawEnergy;
        return true;
    }

    public int GainEnergy(int rawEnergy)
    {
        var previousEnergy = CurrentEnergy;
        CurrentEnergy = Math.Clamp(rawEnergy + CurrentEnergy, 0, MaxEnergy);
        return CurrentEnergy - previousEnergy;
    }

    public void ReplenishEnergy()
    {
        CurrentEnergy = MaxEnergy;
    }

    public bool DrawCard()
    {
        if (_drawPile.Count == 0)
        {
            return false;
        }
        _hand.Add(PopTopCard());
        return true;
    }

    public bool DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            bool drewCard = DrawCard();
            if (!drewCard) return false;
        }
        return true;
    }

    private BattleCard PopTopCard()
    {
        var topCard = _drawPile[^1];
        _drawPile.RemoveAt(_drawPile.Count - 1);
        return topCard;
    }
}