namespace CardGame.Engine;

public class PlayerState
{
    public int CurrentHealth { get; private set; } = 10;
    public int MaxEnergy { get; private set; } = 1;
    public int CurrentEnergy { get; private set; } = 1;

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
}