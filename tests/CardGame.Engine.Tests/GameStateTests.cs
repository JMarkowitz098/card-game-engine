namespace CardGame.Engine.Tests;

public class GameStateTests
{
    public static GameState CreateGameState(
        IEnumerable<BattleCard>? playerADeck = null,
        IEnumerable<BattleCard>? playerBDeck = null,
        PlayerId startingPlayer = PlayerId.PlayerA
    )
    {
        var playerA = new PlayerState((playerADeck ?? new[] { TestCards.Card() }).ToList());
        var playerB = new PlayerState((playerBDeck ?? new[] { TestCards.Card() }).ToList());
        return new GameState(playerA, playerB, startingPlayer);
    }

    private static List<BattleCard> CreateTestDeck(PlayerId id)
    {
        return new List<BattleCard>
        {
            TestCards.Card(name: $"{id}-card1"),
            TestCards.Card(name: $"{id}-card2"),
            TestCards.Card(name: $"{id}-card3"),
            TestCards.Card(name: $"{id}-card4"),
        };
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
    public void DeclareAttack_ValidatesAttack_ReturnsTrue()
    {
        // Arrange
        var gameState = CreateGameState();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, TestCards.Card());

        // Act
        var result = gameState.DeclareAttack(attackIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToDefend, gameState.Phase);
        Assert.Empty(gameState.PlayerA.Hand);
        Assert.Equal(0, gameState.PlayerA.CurrentEnergy);
        Assert.Equal(2, gameState.PlayerA.MaxEnergy);
        Assert.Equal(4, result.Events.Count);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        var attackEvent = Assert.IsType<AttackDeclared>(result.Events[0]);
        Assert.Equal(PlayerId.PlayerA, attackEvent.Attacker);
        Assert.Equal(2, attackEvent.AttackValue);
    }

    [Fact]
    public void DeclareDefense_ValidatesDefenseAndUpdatesHealth_ReturnsTrueSetsAwaitingAttackPhase()
    {
        // Arrange
        var gameState = CreateGameState();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, TestCards.Card());
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerB, TestCards.Card());

        // Act
        gameState.PlayerB.DrawCard();
        var result = gameState.DeclareAttack(attackIntent);
        result = gameState.DeclareDefense(defenseIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(0, gameState.PlayerA.CurrentEnergy);
        Assert.Equal(0, gameState.PlayerB.CurrentEnergy);
        Assert.Equal(4, result.Events.Count);
        Assert.Equal(9, gameState.PlayerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        var defenseEvent = Assert.IsType<DefenseDeclared>(result.Events[0]);
        Assert.Equal(PlayerId.PlayerB, defenseEvent.Defender);
        Assert.Equal(1, defenseEvent.DefendValue);
    }

    [Fact]
    public void DeclareAttack_WrongPlayerAttacks_ReturnsFalse()
    {
        // Arrange
        var gameState = CreateGameState();
        var failingIntent = new DeclareAttackIntent(PlayerId.PlayerB, TestCards.Card());

        // Act
        var result = gameState.DeclareAttack(failingIntent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(1, gameState.PlayerA.CurrentEnergy);
        Assert.Equal(1, gameState.PlayerA.MaxEnergy);
        Assert.Single(gameState.PlayerA.Hand);
    }

    [Fact]
    public void DeclareDefense_PlayerHealthReducedToZero_SetsMatchEndedPhase()
    {
        // Arrange
        var attackCard = TestCards.Card(attack: 20);
        var playerADeck = new List<BattleCard> { attackCard };
        var gameState = CreateGameState(playerADeck);
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, attackCard);
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerB, TestCards.Card());

        // Act
        gameState.PlayerB.DrawCard();
        var result = gameState.DeclareAttack(attackIntent);
        result = gameState.DeclareDefense(defenseIntent);

        // Assert
        Assert.Equal(0, gameState.PlayerB.CurrentHealth);
        Assert.Equal(TurnPhase.MatchEnded, gameState.Phase);
        Assert.True(result.Success);
        var matchEndedEvent = Assert.IsType<MatchEnded>(result.Events[^1]);
        Assert.Equal(PlayerId.PlayerA, matchEndedEvent.Winner);
    }

    [Fact]
    public void DeclareDefense_DeclineToDefend_ReturnsTrueAndSetsAwaitingAttackPhase()
    {
        // Arrange
        var gameState = CreateGameState();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, TestCards.Card());
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerB, null);

        // Act
        gameState.DeclareAttack(attackIntent);
        var result = gameState.DeclareDefense(defenseIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(0, gameState.PlayerA.CurrentEnergy);
        Assert.Equal(1, gameState.PlayerB.CurrentEnergy);
        Assert.Single(result.Events);
        Assert.Equal(8, gameState.PlayerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        var damageDealtEvent = Assert.IsType<DamageDealt>(result.Events[0]);
        Assert.Equal(2, damageDealtEvent.DamageValue);
    }

    [Fact]
    public void DeclareDefense_NotReadyToDefendPhase_ReturnsFalse()
    {
        // Arrange
        var gameState = CreateGameState();
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerA, TestCards.Card());

        // Act
        var result = gameState.DeclareDefense(defenseIntent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(10, gameState.PlayerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
    }

    [Fact]
    public void DeclareDefense_NotActivePlayer_ReturnsFalse()
    {
        // Arrange
        var gameState = CreateGameState();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, TestCards.Card());
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerA, TestCards.Card());

        // Act
        gameState.DeclareAttack(attackIntent);
        var result = gameState.DeclareDefense(defenseIntent);

        Assert.False(result.Success);
        Assert.Equal(TurnPhase.ReadyToDefend, gameState.Phase);
        Assert.Equal(10, gameState.PlayerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
    }

    [Fact]
    public void DeclareAttack_NotReadyToAttackPhase_ReturnsFalse()
    {
        // Arrange
        var gameState = CreateGameState();
        var firstAttackIntent = new DeclareAttackIntent(PlayerId.PlayerA, TestCards.Card());
        gameState.DeclareAttack(firstAttackIntent);
        var secondAttackIntent = new DeclareAttackIntent(PlayerId.PlayerA, TestCards.Card());

        // Act
        var result = gameState.DeclareAttack(secondAttackIntent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(TurnPhase.ReadyToDefend, gameState.Phase);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        Assert.Equal(0, gameState.PlayerA.CurrentEnergy);
    }

    [Fact]
    public void DeclareAttack_CardNotAffordable_ReturnsFalse()
    {
        // Arrange
        var expensiveCard = TestCards.Card(cost: 2);
        var playerADeck = new List<BattleCard> { expensiveCard };
        var gameState = CreateGameState(playerADeck);
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, expensiveCard);

        // Act
        var result = gameState.DeclareAttack(attackIntent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(1, gameState.PlayerA.CurrentEnergy);
        Assert.Single(gameState.PlayerA.Hand);
    }

    [Fact]
    public void DeclareDefense_CardNotAffordable_ReturnsFalse()
    {
        // Arrange
        var expensiveCard = TestCards.Card(cost: 2);
        var playerBDeck = new List<BattleCard> { expensiveCard };
        var gameState = CreateGameState(playerBDeck: playerBDeck);
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, TestCards.Card());
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerB, expensiveCard);

        // Act
        gameState.PlayerB.DrawCard();
        gameState.DeclareAttack(attackIntent);
        var result = gameState.DeclareDefense(defenseIntent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(TurnPhase.ReadyToDefend, gameState.Phase);
        Assert.Equal(1, gameState.PlayerB.CurrentEnergy);
        Assert.Equal(10, gameState.PlayerB.CurrentHealth);
        Assert.Single(gameState.PlayerB.Hand);
    }

    [Fact]
    public void DeclareAttack_DeclineToAttack_ReturnsTrueAndSetsOpponentAsActivePlayer()
    {
        // Arrange
        var gameState = CreateGameState();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, null);

        // Act
        var result = gameState.DeclareAttack(attackIntent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(1, gameState.PlayerA.CurrentEnergy);
        Assert.Equal(1, gameState.PlayerB.CurrentEnergy);
        Assert.Equal(3, result.Events.Count);
        Assert.Equal(10, gameState.PlayerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerB, gameState.ActivePlayer);
    }

    [Fact]
    public void DeclareAttack_DeclineToAttack_OpponentDrawsCardNotThePasser()
    {
        // Arrange — Player B needs a spare card left in their deck for the draw triggered by A's pass
        var playerBDeck = new List<BattleCard> { TestCards.Card(), TestCards.Card() };
        var gameState = CreateGameState(playerBDeck: playerBDeck);

        var playerAHandBefore = gameState.PlayerA.Hand.Count;
        var playerADrawPileBefore = gameState.PlayerA.DrawPile.Count;
        var playerBHandBefore = gameState.PlayerB.Hand.Count;

        // Act
        var result = gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerA, null));

        // Assert
        Assert.Equal(playerAHandBefore, gameState.PlayerA.Hand.Count);
        Assert.Equal(playerADrawPileBefore, gameState.PlayerA.DrawPile.Count);
        Assert.Equal(playerBHandBefore + 1, gameState.PlayerB.Hand.Count);
        var cardDrawnEvent = Assert.Single(result.Events.OfType<CardDrawn>());
        Assert.Equal(PlayerId.PlayerB, cardDrawnEvent.Player);
    }

    [Fact]
    public void TwoConsecutiveTurns_RolesReversed_BothExchangesResolveCorrectly()
    {
        // Arrange — each player needs two cards in hand: one to attack with, one to defend with
        var playerADeck = new List<BattleCard> { TestCards.Card(), TestCards.Card() };
        var playerBDeck = new List<BattleCard> { TestCards.Card(), TestCards.Card() };
        var gameState = CreateGameState(playerADeck, playerBDeck);
        gameState.PlayerA.DrawCard();
        gameState.PlayerB.DrawCards(2);

        var playerAAttackCard = gameState.PlayerA.Hand[0];
        var playerADefendCard = gameState.PlayerA.Hand[1];
        var playerBAttackCard = gameState.PlayerB.Hand[0];
        var playerBDefendCard = gameState.PlayerB.Hand[1];

        // Act + Assert: Turn 1 — Player A attacks, Player B defends
        var turn1AttackResult = gameState.DeclareAttack(
            new DeclareAttackIntent(PlayerId.PlayerA, playerAAttackCard)
        );
        var turn1DefenseResult = gameState.DeclareDefense(
            new DeclareDefenseIntent(PlayerId.PlayerB, playerBDefendCard)
        );

        Assert.True(turn1AttackResult.Success);
        Assert.True(turn1DefenseResult.Success);
        Assert.Equal(9, gameState.PlayerB.CurrentHealth);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);

        // Pass to change turn
        gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerA, null));

        // Act: Turn 2 — roles reversed, Player B attacks, Player A defends
        var turn2AttackResult = gameState.DeclareAttack(
            new DeclareAttackIntent(PlayerId.PlayerB, playerBAttackCard)
        );
        var turn2DefenseResult = gameState.DeclareDefense(
            new DeclareDefenseIntent(PlayerId.PlayerA, playerADefendCard)
        );
        gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerB, null));

        // Assert: turn 2 — the defender is now Player A, not Player B (the exact bug class fixed earlier)
        Assert.True(turn2AttackResult.Success);
        Assert.True(turn2DefenseResult.Success);
        Assert.Equal(TurnPhase.ReadyToAttack, gameState.Phase);
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer);
        Assert.Equal(9, gameState.PlayerA.CurrentHealth);
        Assert.Equal(9, gameState.PlayerB.CurrentHealth);
        Assert.Equal(2, gameState.PlayerA.MaxEnergy);
        Assert.Equal(2, gameState.PlayerB.MaxEnergy);
        Assert.Equal(2, gameState.PlayerA.CurrentEnergy);
        Assert.Equal(2, gameState.PlayerB.CurrentEnergy);
        Assert.Empty(gameState.PlayerA.Hand);
        Assert.Empty(gameState.PlayerB.Hand);
    }

    [Fact]
    public void DeclareAttack_PlayerCanAttackMultipleTimes_ReturnsTrue()
    {
        // Arrange
        var playerADeck = CreateTestDeck(PlayerId.PlayerA);
        var playerBDeck = CreateTestDeck(PlayerId.PlayerB);

        var gameState = CreateGameState(playerADeck, playerBDeck);
        gameState.PlayerA.DrawCards(4);
        gameState.PlayerB.DrawCards(4);
        var playerAHand = gameState.PlayerA.Hand;
        var playerBHand = gameState.PlayerB.Hand;

        // Act
        // Turn 1 — Player A attacks, Player B defends,
        gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerA, playerAHand[0]));
        gameState.DeclareDefense(new DeclareDefenseIntent(PlayerId.PlayerB, playerBHand[0]));
        gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerA, null));

        // Turn 1 — Player B Attacks (Passes)
        gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerB, null));

        // Turn 2 - Player A attacks
        gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerA, playerAHand[0]));
        gameState.DeclareDefense(new DeclareDefenseIntent(PlayerId.PlayerB, playerBHand[0]));
        Assert.Equal(PlayerId.PlayerA, gameState.ActivePlayer); // Ensure attacker has not changed
        gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerA, playerAHand[0]));
        gameState.DeclareDefense(new DeclareDefenseIntent(PlayerId.PlayerB, null));
        var result = gameState.DeclareAttack(new DeclareAttackIntent(PlayerId.PlayerA, null));

        // Assert
        Assert.Equal(PlayerId.PlayerB, gameState.ActivePlayer);
        Assert.True(result.Success);
    }

    [Fact]
    public void DeclareDefense_ResolvesExchange_ClearsPendingAttackCard()
    {
        // Arrange
        var gameState = CreateGameState();
        var attackIntent = new DeclareAttackIntent(PlayerId.PlayerA, TestCards.Card());
        var defenseIntent = new DeclareDefenseIntent(PlayerId.PlayerB, null);

        // Act
        gameState.DeclareAttack(attackIntent);
        Assert.NotNull(gameState.GetPendingAttackCard());
        gameState.DeclareDefense(defenseIntent);

        // Assert
        Assert.Null(gameState.GetPendingAttackCard());
    }

    [Fact(Skip = "To be implemented")]
    public void DeclareAttack_AttemptToPlayCardThatSharesNameButIsDifferentInstance_ReturnsFalse() // Maybe throw
    { }
}
