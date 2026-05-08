using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SingleplayerLobby : MonoBehaviour
{
    [Header("Default Config Assets")]
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private BotsSettings botsSettings;
    [SerializeField] private PlayerLook playerLook;
    [Header("Bots")]
    [SerializeField] private TMP_Text botCountText;
    [Header("Player Color")]
    [SerializeField] private Color playerTrailColor = Color.white;

    private int _botCount;

    private void Awake()
    {
        if (playerLook != null)
            playerTrailColor = playerLook.trailColor;

        ApplyPlayerTrailColor();
        _botCount = GetDefaultBotCount();
        RefreshBotCountUI();
    }

    public void LoadScene(string sceneName)
    {
        InitializeGame();
        SceneManager.LoadScene(sceneName);
    }

    public void AddBot()
    {
        SetBotCount(_botCount + 1);
    }

    public void RemoveBot()
    {
        SetBotCount(_botCount - 1);
    }

    public void SetPlayerTrailColor(Color color)
    {
        playerTrailColor = color;
        ApplyPlayerTrailColor();
    }

    public void SetPlayerTrailColorFromPaletteIndex(int index)
    {
        if (gameSettings == null || gameSettings.trailColorPalette == null || gameSettings.trailColorPalette.Count == 0)
            return;

        index = Mathf.Clamp(index, 0, gameSettings.trailColorPalette.Count - 1);
        SetPlayerTrailColor(gameSettings.trailColorPalette[index]);
    }

    public void InitializeGame()
    {
        _botCount = Mathf.Clamp(_botCount, 0, GetMaxBotCount());
        ApplyPlayerTrailColor();

        var session = GameSessionRuntime.FromDefaults(gameSettings, botsSettings, playerLook, _botCount);
        session.isSingleplayer = true;
        GameSessionBootstrap.SetSession(session);
    }

    private void SetBotCount(int value)
    {
        _botCount = Mathf.Clamp(value, 0, GetMaxBotCount());
        RefreshBotCountUI();
    }

    private int GetDefaultBotCount()
    {
        var session = GameSessionRuntime.FromDefaults(gameSettings, botsSettings, playerLook);
        int totalBots = 0;

        for (int i = 0; i < session.bots.Count; i++)
        {
            totalBots += Mathf.Max(0, session.bots[i].count);
        }

        return Mathf.Clamp(totalBots, 0, GetMaxBotCount());
    }

    private int GetMaxBotCount()
    {
        int maxPlayers = gameSettings != null ? gameSettings.maxPlayers : 2;
        bool hasPlayerPrefab = playerLook != null && playerLook.playerPrefab != null;
        return Mathf.Max(0, maxPlayers - (hasPlayerPrefab ? 1 : 0));
    }

    private void RefreshBotCountUI()
    {
        if (botCountText != null)
            botCountText.text = _botCount.ToString();
    }

    private void ApplyPlayerTrailColor()
    {
        if (playerLook != null)
            playerLook.trailColor = playerTrailColor;
    }
}
