using CardGame.Engine;
using CardGame.DeckManagement;

namespace CardGame.Web;

public class GameSessionService
{
    public PlayerState? PlayerA { get; private set; }
    public PlayerState? PlayerB { get; private set; }

    public void StartMatch()
    {
        PlayerA = new PlayerState(StarterDeck.Create());
        PlayerB = new PlayerState(StarterDeck.Create());
        PlayerA.DrawCards(5);
        PlayerB.DrawCards(5);
    }
}
