using CardGame.Engine;

namespace CardGame.Web.Pages;

public partial class Home : IDisposable
{
    private enum Phase
    {
        Title,
        Setup,
        Play,
        Mulligan,
        GameOver,
    };

    private Phase _currentPhase = Phase.Title;
    private PlayerId? _activePlayerId;
    private readonly HashSet<PlayerId> _mulliganDecided = new();
    private bool _showingPostMulliganHand;
    private string _playerAName = "";
    private string _playerBName = "";
    private readonly List<BattleCard> _previewHand = new()
    {
        new("Spark", Cost: 1, Charge: 1, Attack: 2, Defense: 1),
        new("Ignition", Cost: 1, Charge: 1, Attack: 2, Defense: 1),
        new("Fire", Cost: 1, Charge: 1, Attack: 2, Defense: 1),
        new("Spark", Cost: 1, Charge: 1, Attack: 2, Defense: 1),
        new("Lightning", Cost: 1, Charge: 1, Attack: 2, Defense: 1),
    };
    private readonly PlayerState _previewStats;

    public Home()
    {
        _previewStats = new PlayerState(_previewHand);
    }

    protected override void OnInitialized()
    {
        Game.Changed += StateHasChanged;
    }

    public void Dispose()
    {
        Game.Changed -= StateHasChanged;
        GC.SuppressFinalize(this);
    }

    public void OnCardClicked(BattleCard Card)
    {
        if (Game.Game!.ActivePlayer == _activePlayerId)
        {
            var result = Game.DeclareAttack(Card);
            if (!result)
            {
                Console.WriteLine("Can't use that card");
            }
        }
        else
        {
            var result = Game.DeclareDefense(Card);
            if (!result)
            {
                Console.WriteLine("Can't use that card");
            }
            else if (Game.Game!.Phase == TurnPhase.MatchEnded)
            {
                _currentPhase = Phase.GameOver;
            }
        }
    }

    public void OnClickStart()
    {
        _currentPhase = Phase.Setup;
    }

    private void OnSetupSubmitted((string PlayerAName, string PlayerBName) names)
    {
        _playerAName = names.PlayerAName;
        _playerBName = names.PlayerBName;
        Game.SetupPlayers();
        _currentPhase = Phase.Mulligan;
    }

    public void OnMulliganChoiceClick(bool willMulligan)
    {
        if (_activePlayerId == null)
        {
            return;
        }

        Game.Mulligan(_activePlayerId.Value, willMulligan);
        _mulliganDecided.Add(_activePlayerId.Value);

        if (willMulligan)
        {
            _showingPostMulliganHand = true;
        }
        else
        {
            TryBeginGameIfMulliganComplete();
        }
    }

    private void OnPostMulliganContinue()
    {
        _showingPostMulliganHand = false;
        TryBeginGameIfMulliganComplete();
    }

    private void TryBeginGameIfMulliganComplete()
    {
        if (_mulliganDecided.Count == 2)
        {
            Game.BeginGame(PlayerId.PlayerA);
            _currentPhase = Phase.Play;
        }
    }

    private void OnReadyClicked()
    {
        _activePlayerId = Game.PendingHandOffTo;
        Game.ConfirmReady();
    }

    private PlayerState GetActivePlayer()
    {
        return _activePlayerId == PlayerId.PlayerA ? Game.PlayerA! : Game.PlayerB!;
    }

    private PlayerState GetOpponent()
    {
        return _activePlayerId == PlayerId.PlayerA ? Game.PlayerB! : Game.PlayerA!;
    }

    private string GetPlayerName(PlayerId id)
    {
        return id == PlayerId.PlayerA ? _playerAName : _playerBName;
    }

    private string GetActivePlayerName()
    {
        return GetPlayerName(_activePlayerId!.Value);
    }

    private string GetOpponentName()
    {
        return GetPlayerName(GameState.GetOpponentId(_activePlayerId!.Value));
    }

    private bool IsActivePlayerAttacking()
    {
        return Game.Game!.ActivePlayer == _activePlayerId;
    }

    private BattleCard? GetAttackingCard()
    {
        return Game.Game!.GetPendingAttackCard();
    }

    private string GetWinnerName()
    {
        return Game.Game!.PlayerA.CurrentHealth == 0
            ? GetPlayerName(PlayerId.PlayerB)
            : GetPlayerName(PlayerId.PlayerA);
    }
}
