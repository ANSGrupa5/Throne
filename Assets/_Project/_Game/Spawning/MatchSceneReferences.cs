using UnityEngine;

public sealed class MatchSceneReferences
{
    public MatchSceneReferences(
        GameStartTimer gameStartTimer,
        GameTimer gameTimer,
        EndGameController endGameController,
        Transform botMapCenter)
    {
        GameStartTimer = gameStartTimer;
        GameTimer = gameTimer;
        EndGameController = endGameController;
        BotMapCenter = botMapCenter;
    }

    public GameStartTimer GameStartTimer { get; }
    public GameTimer GameTimer { get; }
    public EndGameController EndGameController { get; }
    public Transform BotMapCenter { get; }
}
