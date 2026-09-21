using CardGame.DeckManagement;
using CardGame.Engine;

namespace CardGame.Web;

public class GameSessionService
{
    private readonly HttpClient _httpClient;

    public GameSessionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public PlayerState? PlayerA { get; private set; }
    public PlayerState? PlayerB { get; private set; }
    public PlayerId? PendingHandOffTo { get; private set; }
    public GameState? Game { get; private set; }
    public event Action? Changed;

    public async Task SetupPlayers(
        List<BattleCard>? seededDeckA = null,
        List<BattleCard>? seededDeckB = null
    )
    {
        List<BattleCard>? starterDeck = null;
        if (seededDeckA == null || seededDeckB == null)
        {
            var catalog = await CardCatalog.LoadAsync(_httpClient, "data/cards.json");
            starterDeck = StarterDeck.Create(catalog, copiesPerCard: 3);
        }

        PlayerA = new PlayerState(seededDeckA ?? starterDeck!);
        PlayerB = new PlayerState(seededDeckB ?? starterDeck!);
        PendingHandOffTo = PlayerId.PlayerA;
        PlayerA.DrawCards(5);
        PlayerB.DrawCards(5);
        Changed?.Invoke();
    }

    public bool Mulligan(PlayerId id, bool willMulligan)
    {
        if (PlayerA == null || PlayerB == null || Game != null)
        {
            throw new InvalidOperationException(
                "Cannot Mulligan before creating Players or after Game has been started"
            );
        }
        var player = id == PlayerId.PlayerA ? PlayerA : PlayerB;
        var success = !willMulligan || player.Mulligan();

        if (success)
        {
            PendingHandOffTo = GameState.GetOpponentId(id);
            Changed?.Invoke();
        }
        return success;
    }

    public void BeginGame(PlayerId startingPlayerId)
    {
        if (PlayerA == null || PlayerB == null)
        {
            throw new InvalidOperationException(
                "Cannot begin the game before both players have been set up."
            );
        }

        PendingHandOffTo = startingPlayerId;
        Game = MatchSetup.StartGame(PlayerA, PlayerB, startingPlayerId);
        Changed?.Invoke();
    }

    public bool DeclareAttack(BattleCard? card)
    {
        if (Game == null)
        {
            throw new InvalidOperationException("Cannot declare an attack before game has begun");
        }

        var intentResult = Game.DeclareAttack(new DeclareAttackIntent(Game.ActivePlayer, card));
        if (intentResult.Success)
        {
            PendingHandOffTo =
                card == null ? Game.ActivePlayer : GameState.GetOpponentId(Game.ActivePlayer);
            Changed?.Invoke();
        }
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
        if (intentResult.Success)
        {
            PendingHandOffTo = IsGameOver(defendingPlayerId) ? null : Game.ActivePlayer;
            Changed?.Invoke();
        }
        return intentResult.Success;
    }

    private bool IsGameOver(PlayerId id)
    {
        return Game?.GetPlayer(id).CurrentHealth == 0;
    }

    public void ConfirmReady()
    {
        PendingHandOffTo = null;
        Changed?.Invoke();
    }
}
