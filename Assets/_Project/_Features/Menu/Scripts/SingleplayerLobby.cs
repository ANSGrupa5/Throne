using TMPro;
using UnityEngine;
using System.Linq;
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
    private bool SuddenDeath = true;

    private void Awake()
    {
        if (playerLook != null)
            playerTrailColor = playerLook.trailColor;

        ApplyPlayerTrailColor();

        // Initialize bot count to 0, allowing the player to add them from scratch.
        // The previous implementation loaded a default value from an asset, which was confusing.
        _botCount = 0;
        RefreshBotCountUI(); // This can be modified later to update your visual "plus" icons.
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

    private void InitializeGame()
    {
        _botCount = Mathf.Clamp(_botCount, 0, GetMaxBotCount());
        
        // Używamy natywnego wsparcia dla określonej liczby botów,
        // która automatycznie pobierze prefab z pliku BotsSettings.
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
        if (botsSettings == null || botsSettings.bots == null)
            return 0;
        
        // Sum the counts directly from the settings asset.
        return botsSettings.bots.Sum(bot => bot?.count ?? 0);
    }

    private int GetMaxBotCount()
    {
        int maxPlayers = 6; // Absolute max limit from GameSettings
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

    public void isSuddenDeath()
    {
        SuddenDeath = !SuddenDeath;
    }

    public void tempLog()
    {
        Debug.Log("Powinno dodać się " + _botCount + " botów");
        Debug.Log("Tryb Sudden death: " + SuddenDeath);
    }
}
