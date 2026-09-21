using CardGame.Engine;

namespace CardGame.DeckManagement;

public record CardTemplate(
    string Name,
    int Cost,
    int Charge,
    int Attack,
    int Defense,
    string? ImagePath
)
{
    public BattleCard ToBattleCard()
    {
        throw new NotImplementedException();
    }
}
