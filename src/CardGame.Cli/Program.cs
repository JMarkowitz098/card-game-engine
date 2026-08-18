using CardGame.Engine;

namespace CardGame.Cli;

public class Program
{
    private static string s_playerAName = "PlayerA";
    private static string s_playerBName = "PlayerB";

    static void Main(string[] args)
    {
        Console.WriteLine("\n------------------------------------");
        Console.WriteLine("The Elemental Chronicles TCG Battler");
        Console.WriteLine("------------------------------------\n");

        Console.Write("Enter a name for Player A (press Enter for default): ");
        var nameAInput = Console.ReadLine();
        s_playerAName = string.IsNullOrWhiteSpace(nameAInput) ? "PlayerA" : nameAInput;

        Console.Write("Enter a name for Player B (press Enter for default): ");
        var nameBInput = Console.ReadLine();
        s_playerBName = string.IsNullOrWhiteSpace(nameBInput) ? "PlayerB" : nameBInput;

        // Setup
        var playerA = new PlayerState(StarterDeck.Create());
        var playerB = new PlayerState(StarterDeck.Create());
        Console.WriteLine($"\n{s_playerAName} created. Deck 40/40");
        Console.WriteLine($"{s_playerBName} created. Deck 40/40\n");
        Console.WriteLine($"{s_playerAName} draws 5 cards");
        playerA.DrawCards(5);
        Console.WriteLine($"{s_playerBName} draws 5 cards\n");
        playerB.DrawCards(5);
        Pause();

        Console.WriteLine(
            $"\nPass the device to {s_playerAName}. Press Enter when they're ready."
        );
        Console.ReadLine();
        ProcessMulligan(PlayerId.PlayerA, playerA);

        HandoffTo(PlayerId.PlayerB);
        ProcessMulligan(PlayerId.PlayerB, playerB);

        HandoffTo(PlayerId.PlayerA);

        var gameState = MatchSetup.StartGame(playerA, playerB, PlayerId.PlayerA);

        // Loop
        while (playerA.CurrentHealth > 0 && playerB.CurrentHealth > 0)
        {
            Loop(gameState);
        }

        if (playerB.CurrentHealth == 0)
        {
            PrintColored($"Congrats {s_playerAName}! You Win!", GetPlayerColor(PlayerId.PlayerA));
        }
        else
        {
            PrintColored($"Congrats {s_playerBName}! You Win!", GetPlayerColor(PlayerId.PlayerB));
        }
    }

    private static void ProcessMulligan(PlayerId playerId, PlayerState player)
    {
        PrintColored($"{GetPlayerName(playerId)}'s hand", GetPlayerColor(playerId));
        PrintHand(player.Hand, GetPlayerColor(playerId));

        if (PromptMulligan(playerId))
        {
            player.Mulligan();
            Console.WriteLine();
            PrintColored($"{GetPlayerName(playerId)}'s new hand", GetPlayerColor(playerId));
            PrintHand(player.Hand, GetPlayerColor(playerId));
        }
    }

    private static bool PromptMulligan(PlayerId playerId)
    {
        while (true)
        {
            Console.Write($"{GetPlayerName(playerId)}, mulligan your hand? (y/n): ");
            var input = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (input == "y")
            {
                return true;
            }
            if (input == "n")
            {
                return false;
            }

            Console.WriteLine("Please enter y or n.");
        }
    }

    private static void Loop(GameState gameState)
    {
        var ap = gameState.ActivePlayer;
        var player = gameState.GetPlayer(ap);
        var op = GameState.GetOpponentId(ap);
        var opponent = gameState.GetPlayer(op);
        var context = new TurnContext(player, opponent, ap, op, gameState);
        PrintColored($"It's {GetPlayerName(ap)}'s turn\n", GetPlayerColor(ap));
        PrintStatus(ap, op, player, opponent);

        Console.WriteLine($"{GetPlayerName(ap)} draws a card");
        PrintColored($"{GetPlayerName(ap)}'s hand", GetPlayerColor(ap));
        PrintHand(player.Hand, GetPlayerColor(ap));

        var attacked = ProcessAttack(context);
        if (attacked)
        {
            HandoffTo(context.Op);
            ProcessDefense(context);

            HandoffTo(context.Ap);
            PrintDefenseSummary(context);
        }
        HandoffTo(gameState.ActivePlayer);
    }

    private static void PrintDefenseSummary(TurnContext context)
    {
        var gameState = context.GameState;
        var lastDefenseCard = gameState.GetLastDefenseCard();
        if (lastDefenseCard == null)
        {
            PrintColored($"{GetPlayerName(context.Op)} doesn't defend", GetPlayerColor(context.Op));
        }
        else
        {
            PrintColored(
                $"{GetPlayerName(context.Op)} defends with {FormatCard(lastDefenseCard)}",
                GetPlayerColor(context.Op)
            );
        }

        var damage = CombatResolver.ResolveBattle(
            gameState.GetPendingAttackCard()!,
            lastDefenseCard
        );
        PrintColored($"{GetPlayerName(context.Op)} took {damage} damage!\n", ConsoleColor.Red);
        PrintStatus(context.Ap, context.Op, context.Player, context.Opponent);
    }

    private static ConsoleColor GetPlayerColor(PlayerId id)
    {
        return id == PlayerId.PlayerA ? ConsoleColor.Cyan : ConsoleColor.Magenta;
    }

    private static string GetPlayerName(PlayerId id)
    {
        return id == PlayerId.PlayerA ? s_playerAName : s_playerBName;
    }

    private static void PrintColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    private static void Pause()
    {
        Thread.Sleep(800);
    }

    private static string FormatCard(BattleCard card)
    {
        return $"{card.Name} | CT: {card.Cost} | CH: {card.Charge} | AT: {card.Attack} | DF: {card.Defense}";
    }

    private static void PrintCard(BattleCard card, int index, ConsoleColor color)
    {
        PrintColored(
            $"{index + 1}. {card.Name, -8} | CT: {card.Cost, -2} | CH: {card.Charge, -2} | AT: {card.Attack, -2} | DF: {card.Defense, -2}",
            color
        );
    }

    private static void PrintHand(IReadOnlyList<BattleCard> hand, ConsoleColor color)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            PrintCard(hand[i], i, color);
        }
        Console.WriteLine();
    }

    private static void PrintStatus(
        PlayerId ap,
        PlayerId op,
        PlayerState player,
        PlayerState opponent
    )
    {
        PrintColored(
            $"{GetPlayerName(ap)}'s Health: {player.CurrentHealth}/10",
            GetPlayerColor(ap)
        );
        PrintColored(
            $"{GetPlayerName(ap)}'s Energy: {player.CurrentEnergy}/{player.MaxEnergy}",
            GetPlayerColor(ap)
        );
        PrintColored(
            $"{GetPlayerName(op)}'s Health: {opponent.CurrentHealth}/10",
            GetPlayerColor(op)
        );
        PrintColored(
            $"{GetPlayerName(op)}'s Energy: {opponent.CurrentEnergy}/{opponent.MaxEnergy}\n",
            GetPlayerColor(op)
        );
        Pause();
    }

    private static void PrintInvalidOption()
    {
        Console.WriteLine("Sorry that's not an option. Please enter a number from your hand.");
    }

    private static bool ProcessAttack(TurnContext context)
    {
        while (true)
        {
            Console.WriteLine("Choose a card to attack with (0 to pass)");
            string? input = Console.ReadLine();
            if (
                !int.TryParse(input, out int index)
                || index < 0
                || index > context.Player.Hand.Count
            )
            {
                PrintInvalidOption();
                continue;
            }

            var card = index == 0 ? null : context.Player.Hand[index - 1];
            if (card == null)
            {
                PrintColored(
                    $"{GetPlayerName(context.Ap)} doesn't attack",
                    GetPlayerColor(context.Ap)
                );
            }
            else
            {
                PrintColored(
                    $"\n{GetPlayerName(context.Ap)} attacks with {FormatCard(card)}",
                    GetPlayerColor(context.Ap)
                );
                Pause();
            }

            var result = context.GameState.DeclareAttack(new DeclareAttackIntent(context.Ap, card));

            if (result.Success)
            {
                if (card == null)
                {
                    return false;
                }
                Console.WriteLine("Attack successful");
                Console.WriteLine(
                    $"Energy reduced by {card.Cost}. Max energy increased by {card.Charge} for a total of {context.Player.MaxEnergy}\n"
                );
                Pause();
                return true;
            }

            Console.WriteLine("You can't play that card. Try again.\n");
        }
    }

    private static void ProcessDefense(TurnContext context)
    {
        var op = context.Op;
        var opponent = context.Opponent;
        var card = context.GameState.GetPendingAttackCard()!;
        PrintColored(
            $"\n{GetPlayerName(context.Ap)} attacks with {FormatCard(card)}",
            GetPlayerColor(context.Ap)
        );
        Console.WriteLine($"{GetPlayerName(op)} chance to defend!\n");

        Pause();
        PrintStatus(context.Ap, op, context.Player, opponent);
        PrintColored($"{GetPlayerName(op)}'s hand", GetPlayerColor(op));
        PrintHand(opponent.Hand, GetPlayerColor(op));

        while (true)
        {
            Console.WriteLine("Choose a card to defend with (0 to Pass):");
            var input = Console.ReadLine();

            if (!int.TryParse(input, out int index) || index < 0 || index > opponent.Hand.Count)
            {
                PrintInvalidOption();
                continue;
            }

            var opCard = index == 0 ? null : opponent.Hand[index - 1];
            if (index == 0)
            {
                PrintColored($"{GetPlayerName(op)} doesn't defend", GetPlayerColor(op));
            }
            else
            {
                PrintColored(
                    $"{GetPlayerName(op)} defends with {FormatCard(opCard!)}",
                    GetPlayerColor(op)
                );
            }
            Pause();

            var result = context.GameState.DeclareDefense(new DeclareDefenseIntent(op, opCard));

            if (!result.Success)
            {
                Console.WriteLine("You can't play that card. Try again.\n");
                continue;
            }

            var damageEvent = result.Events.OfType<DamageDealt>().FirstOrDefault();
            if (damageEvent != null)
            {
                PrintColored(
                    $"{GetPlayerName(damageEvent.Player)} took {damageEvent.DamageValue} damage!\n",
                    ConsoleColor.Red
                );
                Pause();
            }
            Console.WriteLine("-------------------");
            Console.WriteLine("-----Next turn-----");
            Console.WriteLine("-------------------\n");
            return;
        }
    }

    private static void HandoffTo(PlayerId nextPlayerId)
    {
        Console.WriteLine("\nPress Enter when you're done and ready to hide your cards.");
        Console.ReadLine();
        Console.Clear();
        Console.WriteLine(
            $"Pass the device to {GetPlayerName(nextPlayerId)}. Press Enter when they're ready."
        );
        Console.ReadLine();
    }
}
