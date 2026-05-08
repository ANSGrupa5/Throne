using UnityEngine;
using UnityEngine.SceneManagement;

public class SingleplayerLobby : MonoBehaviour
{
    [Header("Default Config Assets")]
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private BotsSettings botsSettings;
    [SerializeField] private PlayerLook playerLook;

    public void LoadScene(string sceneName)
    {
        InitializeGame();
        SceneManager.LoadScene(sceneName);
    }

    public void InitializeGame()
    {
        var session = GameSessionRuntime.FromDefaults(gameSettings, botsSettings, playerLook);
        session.isSingleplayer = true;
        GameSessionBootstrap.SetSession(session);
    }
}
