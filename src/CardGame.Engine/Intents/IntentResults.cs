namespace CardGame.Engine;

public record IntentResult(bool Success, IReadOnlyList<IGameEvent> Events);
