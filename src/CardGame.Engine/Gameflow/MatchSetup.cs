namespace CardGame.Engine;

public class MatchSetup
{
    public static GameState StartGame(
        PlayerState playerA,
        PlayerState playerB,
        PlayerId startingPlayer
    )
    {
        return new GameState(playerA, playerB, startingPlayer);
    }
}
