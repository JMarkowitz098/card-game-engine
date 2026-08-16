namespace CardGame.Engine.Tests;

public class GameStateTests
{
    private static GameStateTestContext SetupTestContext()
    {
        // Arrange
        var attackCard = new BattleCard("spark", Element.Scor, 1, 1, 2, 1);
        var defenseCard = new BattleCard("breeze", Element.Eth, 1, 1, 2, 1);
        var playerA = new PlayerState(new List<BattleCard> { attackCard });
        var playerB = new PlayerState(new List<BattleCard> { defenseCard });
        var gameState = new GameState(playerA, playerB, PlayerId.PlayerA);
        playerA.DrawCard();
        playerB.DrawCard();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, attackCard);
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerB, defenseCard);
        return new GameStateTestContext(
            attackCard, defenseCard, playerA, playerB, gameState, attackIntent, defenseIntent);
    }

    private static GameStateTestContext SetupGameEndContext()
    {
        // Arrange
        var attackCard = new BattleCard("spark", Element.Scor, 1, 1, 20, 1);
        var defenseCard = new BattleCard("breeze", Element.Eth, 1, 1, 2, 1);
        var playerA = new PlayerState(new List<BattleCard> { attackCard });
        var playerB = new PlayerState(new List<BattleCard> { defenseCard });
        var gameState = new GameState(playerA, playerB, PlayerId.PlayerA);
        playerA.DrawCard();
        playerB.DrawCard();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, attackCard);
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerB, defenseCard);
        return new GameStateTestContext(
            attackCard, defenseCard, playerA, playerB, gameState, attackIntent, defenseIntent);
    }

    [Fact]
    public void Constructor_SetsActivePlayerAndAwaitingAttackPhase()
    {
        var playerA = new PlayerState(new List<BattleCard>());
        var playerB = new PlayerState(new List<BattleCard>());

        var gameState = new GameState(playerA, playerB, PlayerId.PlayerA);

        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
    }

    [Fact]
    public void DeclareAttack_ValidatesAttack_ReturnsTrueAndSetsAwaitingDefensePhase()
    {
        // Arrange
        var context = SetupTestContext();
        var gameState = context.GameState;

        // Act
        var result = gameState.DeclareAttack(context.AttackIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToDefend, gameState.Phase);
        Assert.Empty(context.PlayerA.Hand);
        Assert.Equal(0, context.PlayerA.CurrentEnergy);
        Assert.Equal(2, context.PlayerA.MaxEnergy);
        Assert.Equal(4, result.Events.Count);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        var attackEvent = Assert.IsType<AttackDeclared>(result.Events[0]);
        Assert.Equal(PlayerId.PlayerA, attackEvent.Attacker);
        Assert.Equal(2, attackEvent.AttackValue);
    }

    [Fact]
    public void DeclareDefense_ValidatesDefenseAndUpdatesHealth_ReturnsTrueAndSetsAwaitingAttackPhase()
    {
        // Arrange
        var context = SetupTestContext();
        var gameState = context.GameState;

        // Act
        var result = gameState.DeclareAttack(context.AttackIntent);
        result = gameState.DeclareDefense(context.DefenseIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(2, context.PlayerA.CurrentEnergy);
        Assert.Equal(1, context.PlayerB.CurrentEnergy);
        Assert.Equal(7, result.Events.Count);
        Assert.Equal(9, context.PlayerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerB, gameState.ActivePlayer);
        var defenseEvent = Assert.IsType<DefenseDeclared>(result.Events[0]);
        Assert.Equal(PlayerId.PlayerB, defenseEvent.Defender);
        Assert.Equal(1, defenseEvent.DefendValue);
    }

    [Fact]
    public void DeclareAttack_WrongPlayerAttacks_ReturnsFalse()
    {
        // Arrange
        var context = SetupTestContext();
        var gameState = context.GameState;
        var failingIntent = new DeclareAttackIntent(PlayerId.PlayerB, context.AttackCard);

        // Act
        var result = gameState.DeclareAttack(failingIntent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(1, context.PlayerA.CurrentEnergy);
        Assert.Equal(1, context.PlayerA.MaxEnergy);
        Assert.Single(context.PlayerA.Hand);
    }

    [Fact]
    public void DeclareDefense_PlayerHealthReducedToZero_SetsMatchEndedPhase()
    {
        // Arrange
        var context = SetupGameEndContext();
        var gameState = context.GameState;

        // Act
        var result = gameState.DeclareAttack(context.AttackIntent);
        result = gameState.DeclareDefense(context.DefenseIntent);

        // Assert
        Assert.Equal(0, context.PlayerB.CurrentHealth);
        Assert.Equal(TurnPhase.MatchEnded, gameState.Phase);
        Assert.True(result.Success);
        var matchEndedEvent = Assert.IsType<MatchEnded>(result.Events[^1]);
        Assert.Equal(PlayerId.PlayerA, matchEndedEvent.Winner);
    }

    [Fact]
    public void DeclareDefense_DeclineToDefend_ReturnsTrueAndSetsAwaitingAttackPhase()
    {
        // Arrange
        var context = SetupTestContext();
        var gameState = context.GameState;
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerB, null);

        // Act
        var result = gameState.DeclareAttack(context.AttackIntent);
        result = gameState.DeclareDefense(defenseIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(2, context.PlayerA.CurrentEnergy);
        Assert.Equal(1, context.PlayerB.CurrentEnergy);
        Assert.Equal(4, result.Events.Count);
        Assert.Equal(8, context.PlayerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerB, gameState.ActivePlayer);
        var damageDealtEvent = Assert.IsType<DamageDealt>(result.Events[0]);
        Assert.Equal(2, damageDealtEvent.DamageValue);
    }

    [Fact]
    public void TwoConsecutiveTurns_RolesReversed_BothExchangesResolveCorrectly()
    {
        // Arrange
        var playerAAttackCard = new BattleCard("aAttack", Element.Scor, 1, 1, 2, 1);
        var playerADefendCard = new BattleCard("aDefend", Element.Eth, 1, 1, 2, 1);
        var playerBDefendCard = new BattleCard("bDefend", Element.Eth, 1, 1, 2, 1);
        var playerBAttackCard = new BattleCard("bAttack", Element.Scor, 1, 1, 2, 1);

        var playerA = new PlayerState(new List<BattleCard> { playerADefendCard, playerAAttackCard });
        var playerB = new PlayerState(new List<BattleCard> { playerBAttackCard, playerBDefendCard });
        var gameState = new GameState(playerA, playerB, PlayerId.PlayerA);
        playerA.DrawCards(2);
        playerB.DrawCards(2);

        // Act + Assert: Turn 1 — Player A attacks, Player B defends
        var turn1AttackResult = gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerA, playerAAttackCard));
        var turn1DefenseResult = gameState.DeclareDefense(new DeclareDefenseIntent(PlayerId.PlayerB, playerBDefendCard));

        Assert.True(turn1AttackResult.Success);
        Assert.True(turn1DefenseResult.Success);
        Assert.Equal(9, playerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerB, gameState.ActivePlayer);

        // Act: Turn 2 — roles reversed, Player B attacks, Player A defends
        var turn2AttackResult = gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerB, playerBAttackCard));
        var turn2DefenseResult = gameState.DeclareDefense(new DeclareDefenseIntent(PlayerId.PlayerA, playerADefendCard));

        // Assert: turn 2 — the defender is now Player A, not Player B (the exact bug class fixed earlier)
        Assert.True(turn2AttackResult.Success);
        Assert.True(turn2DefenseResult.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        Assert.Equal(9, playerA.CurrentHealth);
        Assert.Equal(9, playerB.CurrentHealth); // unchanged since turn 1 — Player B was attacking, not defending
        Assert.Equal(2, playerA.MaxEnergy);
        Assert.Equal(2, playerB.MaxEnergy);
        Assert.Equal(2, playerA.CurrentEnergy);
        Assert.Equal(2, playerB.CurrentEnergy);
        Assert.Empty(playerA.Hand);
        Assert.Empty(playerB.Hand);
    }
}
