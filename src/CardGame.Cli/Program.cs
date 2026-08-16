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

        // Loop
        Console.WriteLine("");
    }

    private static List<BattleCard> CreateDummyDeck(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new BattleCard($"card{i}", Element.Scor, 1, 1, 2, 1))
            .ToList();
    }


}
