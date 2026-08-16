namespace CardGame.Engine;

public class GameState
{
    public PlayerState PlayerA { get; }
    public PlayerState PlayerB { get; }
    public PlayerId ActivePlayer { get; private set; }
    public TurnPhase Phase { get; private set; }
    private BattleCard? _pendingAttackCard;


    public GameState(PlayerState playerA, PlayerState playerB, PlayerId startingPlayer)
    {
        PlayerA = playerA;
        PlayerB = playerB;
        ActivePlayer = startingPlayer;
        Phase = TurnPhase.ReadyToAttack;
    }

    public IntentResult DeclareAttack(DeclareAttackIntent intent)
    {
        var card = intent.Card;

        if (!CanAttack(intent))
        {
            return IntentResultFail();
        }

        var attacker = GetPlayer(intent.AttackingPlayerId);

        if (!CanUseCard(attacker, card))
        {
            return IntentResultFail();
        }

        var events = new List<IGameEvent> { new AttackDeclared(intent.AttackingPlayerId, card.Attack) };
        events.Add(new EnergySpent(intent.AttackingPlayerId, card.Cost));

        attacker.IncreaseMaxEnergy(card.Charge);
        events.Add(new MaxEnergyIncreased(intent.AttackingPlayerId, card.Charge));

        attacker.DiscardCard(card);
        events.Add(new CardDiscarded(intent.AttackingPlayerId, card.Name));

        _pendingAttackCard = card;
        Phase = TurnPhase.ReadyToDefend;

        return new IntentResult(true, events);
    }

    public IntentResult DeclareDefense(DeclareDefenseIntent intent)
    {
        var defender = GetPlayer(intent.DefendingPlayerId);
        var events = new List<IGameEvent>();

        if (!CanDefend(intent))
        {
            return IntentResultFail();
        }

        if (intent.Card != null)
        {
            var card = intent.Card;

            if (!CanUseCard(defender, card))
            {
                return IntentResultFail();
            }

            events.Add(new DefenseDeclared(intent.DefendingPlayerId, card.Defense));
            events.Add(new EnergySpent(intent.DefendingPlayerId, card.Cost));

            defender.DiscardCard(card);
            events.Add(new CardDiscarded(intent.DefendingPlayerId, card.Name));
        }

        if (_pendingAttackCard == null)
        {
            throw new InvalidOperationException();
        }

        int damage = CombatResolver.ResolveBattle(_pendingAttackCard, intent.Card);

        defender.TakeDamage(damage);
        events.Add(new DamageDealt(intent.DefendingPlayerId, damage));

        if (defender.CurrentHealth == 0)
        {
            Phase = TurnPhase.MatchEnded;
            events.Add(new MatchEnded(GetOpponentId(intent.DefendingPlayerId)));
            return new IntentResult(true, events);
        }

        ReadyNewTurn(defender, events, intent);

        return new IntentResult(true, events);
    }

    public PlayerState GetPlayer(PlayerId id)
    {
        return id == PlayerId.PlayerA ? PlayerA : PlayerB;
    }

    public static PlayerId GetOpponentId(PlayerId id)
    {
        return id == PlayerId.PlayerA ? PlayerId.PlayerB : PlayerId.PlayerA;
    }

    private void ReadyNewTurn(PlayerState defender, List<IGameEvent> events, DeclareDefenseIntent intent)
    {
        Phase = TurnPhase.ReadyToAttack;
        defender.ReplenishEnergy();
        events.Add(new EnergyReplenishedToFull(intent.DefendingPlayerId));
        var opponentId = GetOpponentId(intent.DefendingPlayerId);
        var opponent = GetPlayer(opponentId);
        opponent.ReplenishEnergy();
        events.Add(new EnergyReplenishedToFull(opponentId));
        opponent.DrawCard();
        events.Add(new CardDrawn(opponentId));
        ActivePlayer = intent.DefendingPlayerId;
    }

    private bool CanAttack(DeclareAttackIntent intent)
    {
        return Phase == TurnPhase.ReadyToAttack && intent.AttackingPlayerId == ActivePlayer;
    }

    private bool CanDefend(DeclareDefenseIntent intent)
    {
        return Phase == TurnPhase.ReadyToDefend && intent.DefendingPlayerId != ActivePlayer;
    }

    private static bool CanUseCard(PlayerState player, BattleCard card)
    {
        return player.Hand.Contains(card) && player.TryUseEnergy(card.Cost);
    }

    private static IntentResult IntentResultFail()
    {
        return new IntentResult(false, new List<IGameEvent>());
    }

}