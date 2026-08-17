using CardGame.Engine;

namespace CardGame.Cli;

public record TurnContext(
    PlayerState Player,
    PlayerState Opponent,
    PlayerId Ap,
    PlayerId Op,
    GameState GameState
);
