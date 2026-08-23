using CardGame.DeckManagement;
using CardGame.Engine;

namespace CardGame.Web;

public class GameSessionService
{
    public PlayerState? PlayerA { get; private set; }
    public PlayerState? PlayerB { get; private set; }
    public GameState? Game { get; private set; }

    public void SetupPlayers(
        List<BattleCard>? seededDeckA = null,
        List<BattleCard>? seededDeckB = null
    )
    {
        PlayerA = new PlayerState(seededDeckA ?? StarterDeck.Create());
        PlayerB = new PlayerState(seededDeckB ?? StarterDeck.Create());
        PlayerA.DrawCards(5);
        PlayerB.DrawCards(5);
    }

    public void BeginGame(PlayerId startingPlayerId)
    {
        if (PlayerA == null || PlayerB == null)
        {
            throw new InvalidOperationException(
                "Cannot begin the game before both players have been set up."
            );
        }

        Game = MatchSetup.StartGame(PlayerA, PlayerB, startingPlayerId);
    }

    public bool DeclareAttack(BattleCard? card)
    {
        if (Game == null)
        {
            throw new InvalidOperationException("Cannot declare an attack before game has begun");
        }

        var intentResult = Game.DeclareAttack(new DeclareAttackIntent(Game.ActivePlayer, card));
        return intentResult.Success;
    }

    public bool DeclareDefense(BattleCard? card)
    {
        if (Game == null)
        {
            throw new InvalidOperationException("Cannot declare defense before game has begun");
        }
        var defendingPlayerId = GameState.GetOpponentId(Game.ActivePlayer);
        var intentResult = Game.DeclareDefense(new DeclareDefenseIntent(defendingPlayerId, card));
        return intentResult.Success;
    }
}
