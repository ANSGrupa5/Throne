using System.Linq;
using TMPro;
using UnityEditor;
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

    [Header("Match Duration")]
    [SerializeField] private TMP_Text minutes;
    [SerializeField] private TMP_Text seconds;

    [Header("Trail Length")]
    [SerializeField] private TMP_Text trailLengthText;

    private int _botCount;
    private bool SuddenDeath = true;
    private int min, sec, maxmin, minmin;
    private float timeInSecs;
    private string arenaSceneName;
    private int trailLength;
    private int trailColor;

    private void Awake()
    {
        if (gameSettings != null)
            arenaSceneName = gameSettings.arenaSceneName;

        if (playerLook != null)
            playerTrailColor = playerLook.trailColor;

        ApplyPlayerTrailColor();

        // Initialize bot count to 0, allowing the player to add them from scratch.
        // The previous implementation loaded a default value from an asset, which was confusing.
        _botCount = 0;
        RefreshBotCountUI(); // This can be modified later to update your visual "plus" icons.

        min = 1; //curently selected minutes
        sec = 0; //currently selected seconds
        maxmin = 10; //max selectable minutes
        minmin = 1; //min selectable minutes
        timeInSecs = 60f;
        minutes.text = min.ToString("00");
        seconds.text = sec.ToString("00");

        trailLength = gameSettings.trailLength;
        switch (trailLength)
        {
            case 0:
                trailLengthText.text = "Short";
                break;
            case 1:
                trailLengthText.text = "Medium";
                break;
            case 2:
                trailLengthText.text = "Long";
                break;
            case 3:
                trailLengthText.text = "Permanent";
                break;
        }
    }

    public void LoadScene(string sceneName)
    {
        arenaSceneName = sceneName;
        InitializeGame();
        SceneManager.LoadScene(sceneName);
    }
    public void LoadScene()
    {
        InitializeGame();
        SceneManager.LoadScene(arenaSceneName);
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
        gameSettings.maxPlayers = 1 + _botCount;
        gameSettings.matchDuration = timeInSecs;
        gameSettings.isSuddenDeath = SuddenDeath;
        gameSettings.arenaSceneName = arenaSceneName;

        SetPlayerTrailColorFromPaletteIndex(trailColor);

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
        Debug.Log("Czas trwania meczu w sekundach: " + timeInSecs);
        Debug.Log("Trail length: " + trailLength);
        Debug.Log("Trail color: " + trailColor);
    }

    public void ShowGameTime()
    {
        minutes.text = min.ToString("00");
        seconds.text = sec.ToString("00");
    }

    private void UpdateTimeInSeconds()
    {
        timeInSecs = (float)(min * 60) + (float)sec;
    }

    public void IncreaseMin()
    {
        min++;
        if(min > maxmin)
            min = minmin;
        UpdateTimeInSeconds();
        ShowGameTime();
    }

    public void DecreaseMin()
    {
        min--;
        if (min < minmin)
        {
            min = maxmin;
            sec = 0;
        }
        UpdateTimeInSeconds();
        ShowGameTime();
    }

    public void IncreaseSec()
    {
        sec = sec + 5;
        if (sec >= 60)
            sec = 0;
        if (min == 10)
            sec = 0;
        UpdateTimeInSeconds();
        ShowGameTime();
    }

    public void DecreaseSec()
    {
        sec = sec - 5;
        if (sec < 0)
            sec = 55;
        if (min == 10)
            sec = 0;
        UpdateTimeInSeconds();
        ShowGameTime();
    }

    public void ChangeTrailLength()
    {
        trailLength++;
        if (trailLength > gameSettings.GetMaxTrailLength())
            trailLength = gameSettings.GetMinTrailLength();

        gameSettings.trailLength = trailLength;

        switch(trailLength)
        {
            case 0:
                trailLengthText.text = "Short";
                break;
            case 1:
                trailLengthText.text = "Medium";
                break;
            case 2:
                trailLengthText.text = "Long";
                break;
            case 3:
                trailLengthText.text = "Permanent";
                break;
        }
    }

    public void SetTrailColor(int value)
    {
        trailColor = value;
    }
}
