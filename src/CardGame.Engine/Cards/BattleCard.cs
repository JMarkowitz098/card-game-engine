namespace CardGame.Engine;

public record BattleCard(
    string Name,
    Element Element,
    int Cost,
    int Charge,
    int Attack,
    int Defense
);
