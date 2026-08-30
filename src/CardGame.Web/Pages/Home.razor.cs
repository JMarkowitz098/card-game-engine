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
    };

    private Phase _currentPhase = Phase.Title;
    private PlayerId? _activePlayerId;
    private readonly HashSet<PlayerId> _mulliganDecided = new();
    private bool _showingPostMulliganHand;

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
        Console.WriteLine("Clicked " + Card.Name);
    }

    public void OnClickStart()
    {
        _currentPhase = Phase.Setup;
    }

    private void OnSetupSubmitted()
    {
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
}
