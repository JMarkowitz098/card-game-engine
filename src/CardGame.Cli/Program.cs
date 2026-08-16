using CardGame.Engine;
namespace CardGame.Cli;

public class Program
{
    static void Main(string[] args)
    {
        // Setup
        var playerA = new PlayerState(CreateDummyDeck(40));
        var playerB = new PlayerState(CreateDummyDeck(40));
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

        // Loop
        while (playerA.CurrentHealth > 0 && playerB.CurrentHealth > 0)
        {
            var ap = gameState.ActivePlayer;
            var player = gameState.GetPlayer(ap);
            var op = GameState.GetOpponentId(ap);
            var opponent = gameState.GetPlayer(op);
            Console.WriteLine("It's " + ap + "'s turn\n");
            printStatus(ap, op, player, opponent);

            Console.WriteLine($"{ap}s hand");
            printHand(player.Hand);
            Console.WriteLine($"{ap} draws a card");
            player.DrawCard();
            printHand(player.Hand);

            Console.WriteLine("Choose a card to attack with:");
            string? input = Console.ReadLine();
            if (int.TryParse(input, out int index))
            {
                if (index < 0 || index > player.Hand.Count)
                {
                    Console.WriteLine("Sorry that's not an option. Please enter a number from your hand.");
                    // continue;
                }
                var card = player.Hand[index - 1];
                Console.WriteLine($"\n{ap} attacks with {card.Name} (AT {card.Attack})");
                var attackIntent = new DeclareAttackIntent(ap, card);
                var result = gameState.DeclareAttack(attackIntent);

                if (result.Success)
                {
                    Console.WriteLine("Attack successful");
                    Console.WriteLine($"Energy reduced by {card.Cost}. Max energy increased by {card.Charge} for a total of {player.MaxEnergy}\n");
                }

                Console.WriteLine($"{op} chance to defend!\n");
                printStatus(ap, op, player, opponent);
                Console.WriteLine($"{op}'s hand");
                printHand(opponent.Hand);
                Console.WriteLine("0. Pass");

                Console.WriteLine("Choose a card to defend with:");
                input = Console.ReadLine();

                if (int.TryParse(input, out index))
                {
                    var opCard = index == 0 ? null : opponent.Hand[index - 1];
                    if (index == 0)
                    {
                        Console.WriteLine($"{op} doesn't defend");
                    }
                    else
                    {
                        Console.WriteLine($"{op} defends with {opCard.Name}");
                    }
                    var defenseIntent = new DeclareDefenseIntent(op, opCard);
                    result = gameState.DeclareDefense(defenseIntent);
                    var damageEvent = result.Events.OfType<DamageDealt>().FirstOrDefault();
                    if (damageEvent != null)
                    {
                        Console.WriteLine($"{damageEvent.Player} took {damageEvent.DamageValue} damage!\n");
                    }
                    Console.WriteLine("-------------------");
                    Console.WriteLine("-----Next turn-----");
                    Console.WriteLine("-------------------\n");
                }
                else
                {
                    Console.WriteLine("Sorry that's not an option. Please enter a number from your hand.");
                    // continue;
                }
            }
            else
            {
                {
                    Console.WriteLine("Sorry that's not an option. Please enter a number from your hand.");
                    // continue;
                }
            }
        }

        if (playerB.CurrentHealth == 0)
        {
            Console.WriteLine("Congrats Player A! You Win!");
        }
        else
        {
            Console.WriteLine("Congrats Player B! You Win!");
        }
    }

    private static List<BattleCard> CreateDummyDeck(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new BattleCard($"card{i}", Element.Scor, 1, 1, 4, 1))
            .ToList();
    }

    private static void printCard(BattleCard card, int index)
    {
        Console.WriteLine($"{index + 1}. {card.Name} | EL: {card.Element} | CT: {card.Cost} | CH: {card.Charge} | AT: {card.Attack} | DF: {card.Defense}");
    }

    private static void printHand(IReadOnlyList<BattleCard> hand)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            printCard(hand[i], i);
        }
        Console.WriteLine();
    }

    private static void printStatus(PlayerId ap, PlayerId op, PlayerState player, PlayerState opponent)
    {
        Console.WriteLine($"{ap}'s Health: {player.CurrentHealth}/10");
        Console.WriteLine($"{ap}'s Energy: {player.CurrentEnergy}/{player.MaxEnergy}");
        Console.WriteLine($"{op}'s Health: {opponent.CurrentHealth}/10");
        Console.WriteLine($"{op}'s Energy: {opponent.CurrentEnergy}/{opponent.MaxEnergy}\n");
    }

}
