namespace CardGame.Engine;

public interface IGameEvent { }

public record AttackDeclared(PlayerId Attacker, int AttackValue) : IGameEvent;
public record DefenseDeclared(PlayerId Defender, int DefendValue) : IGameEvent;
public record EnergySpent(PlayerId Player, int EnergyValue) : IGameEvent;
public record CardDiscarded(PlayerId Player, string Name) : IGameEvent;
public record DamageDealt(PlayerId Player, int DamageValue) : IGameEvent;
public record MaxEnergyIncreased(PlayerId Player, int AdditionalEneryValue) : IGameEvent;
public record CardDrawn(PlayerId Player) : IGameEvent;
public record EnergyReplenishedToFull(PlayerId Player) : IGameEvent;
public record MatchEnded(PlayerId Winner): IGameEvent;
