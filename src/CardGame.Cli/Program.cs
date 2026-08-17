using CardGame.Engine;

namespace CardGame.Cli;

public class Program
{
    static void Main(string[] args)
    {
        // Setup
        var playerA = new PlayerState(StarterDeck.Create());
        var playerB = new PlayerState(StarterDeck.Create());
        var gameState = new GameState(playerA, playerB, PlayerId.PlayerA);
        Console.WriteLine("\n------------------------------------");
        Console.WriteLine("The Elemental Chronicles TCG Battler");
        Console.WriteLine("------------------------------------\n");
        Console.WriteLine("PlayerA created. Deck 40/40");
        Console.WriteLine("PlayerB created. Deck 40/40\n");
        Console.WriteLine("PlayerA draws 5 cards");
        playerA.DrawCards(5);
        Console.WriteLine("PlayerB draws 5 cards\n");
        playerB.DrawCards(5);
        Pause();

        // Loop
        while (playerA.CurrentHealth > 0 && playerB.CurrentHealth > 0)
        {
            Loop(gameState);
        }

        if (playerB.CurrentHealth == 0)
        {
            PrintColored("Congrats Player A! You Win!", GetPlayerColor(PlayerId.PlayerA));
        }
        else
        {
            PrintColored("Congrats Player B! You Win!", GetPlayerColor(PlayerId.PlayerB));
        }
    }

    private static void Loop(GameState gameState)
    {
        var ap = gameState.ActivePlayer;
        var player = gameState.GetPlayer(ap);
        var op = GameState.GetOpponentId(ap);
        var opponent = gameState.GetPlayer(op);
        var context = new TurnContext(player, opponent, ap, op, gameState);
        PrintColored($"It's {ap}'s turn\n", GetPlayerColor(ap));
        PrintStatus(ap, op, player, opponent);

        PrintColored($"{ap}'s hand", GetPlayerColor(ap));
        PrintHand(player.Hand, GetPlayerColor(ap));
        Console.WriteLine($"{ap} draws a card");
        player.DrawCard();
        PrintHand(player.Hand, GetPlayerColor(ap));

        ProcessAttack(context);
        ProcessDefense(context);
    }

    private static ConsoleColor GetPlayerColor(PlayerId id)
    {
        return id == PlayerId.PlayerA ? ConsoleColor.Cyan : ConsoleColor.Magenta;
    }

    private static void PrintColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    private static void Pause()
    {
        Thread.Sleep(1000);
    }

    private static void PrintCard(BattleCard card, int index, ConsoleColor color)
    {
        PrintColored(
            $"{index + 1}. {card.Name} | {card.Element} | CT: {card.Cost} | CH: {card.Charge} | AT: {card.Attack} | DF: {card.Defense}",
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
        PrintColored($"{ap}'s Health: {player.CurrentHealth}/10", GetPlayerColor(ap));
        PrintColored(
            $"{ap}'s Energy: {player.CurrentEnergy}/{player.MaxEnergy}",
            GetPlayerColor(ap)
        );
        PrintColored($"{op}'s Health: {opponent.CurrentHealth}/10", GetPlayerColor(op));
        PrintColored(
            $"{op}'s Energy: {opponent.CurrentEnergy}/{opponent.MaxEnergy}\n",
            GetPlayerColor(op)
        );
        Pause();
    }

    private static void PrintInvalidOption()
    {
        Console.WriteLine("Sorry that's not an option. Please enter a number from your hand.");
    }

    private static void ProcessAttack(TurnContext context)
    {
        while (true)
        {
            Console.WriteLine("Choose a card to attack with:");
            string? input = Console.ReadLine();
            if (
                !int.TryParse(input, out int index)
                || index < 1
                || index > context.Player.Hand.Count
            )
            {
                PrintInvalidOption();
                continue;
            }

            var card = context.Player.Hand[index - 1];
            PrintColored(
                $"\n{context.Ap} attacks with {card.Name} (AT {card.Attack})",
                GetPlayerColor(context.Ap)
            );
            Pause();
            var result = context.GameState.DeclareAttack(new DeclareAttackIntent(context.Ap, card));

            if (result.Success)
            {
                Console.WriteLine("Attack successful");
                Console.WriteLine(
                    $"Energy reduced by {card.Cost}. Max energy increased by {card.Charge} for a total of {context.Player.MaxEnergy}\n"
                );
                Pause();
                return;
            }

            Console.WriteLine("You can't play that card. Try again.\n");
        }
    }

    private static void ProcessDefense(TurnContext context)
    {
        var op = context.Op;
        var opponent = context.Opponent;
        Console.WriteLine($"{op} chance to defend!\n");
        Pause();
        PrintStatus(context.Ap, op, context.Player, opponent);
        PrintColored($"{op}'s hand", GetPlayerColor(op));
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
                PrintColored($"{op} doesn't defend", GetPlayerColor(op));
            }
            else
            {
                PrintColored($"{op} defends with {opCard!.Name}", GetPlayerColor(op));
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
                    $"{damageEvent.Player} took {damageEvent.DamageValue} damage!\n",
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
}
