using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menu;

    private readonly GameObject[] screens = new GameObject[6];
    private readonly string[] screenNames =
    {
        "MainScreen",
        "MainMenuSettingsScreen",
        "SoundSettingsScreen",
        "GraphicsSettingsScreen",
        "ControlsSettingsScreen",
        "PlayerStatsScreen"
    };

    private enum ScreenType
    {
        Main,
        Settings,
        Sound,
        Graphics,
        Controls,
        Statistics
    }

    private void Start()
    {
        InitializeScreens();
        ShowScreen((int)ScreenType.Main);
    }

    public void LoadScene(string sceneName)
    {
        if (LobbyLaunchContext.IsSharedLobbySceneName(sceneName))
        {
            LoadSingleplayer();
            return;
        }

        SceneTransitionLoader.LoadScene(sceneName);
    }

    public void LoadSingleplayer()
    {
        LobbyLaunchContext.RequestSingleplayer();
        SceneTransitionLoader.LoadScene(LobbyLaunchContext.SharedLobbySceneName);
    }

    public void LoadMultiplayerConnection()
    {
        SceneTransitionLoader.LoadScene("MultiplayerConnection");
    }

    public void ShowScreen(int screenNumber)
    {
        if (!IsValidScreenIndex(screenNumber))
        {
            Debug.LogError($"[MainMenu] Cannot show screen index {screenNumber}. Valid range is 0-{screens.Length - 1}.");
            return;
        }

        if (!EnsureScreensInitialized())
            return;

        GameObject targetScreen = screens[screenNumber];
        if (targetScreen == null)
        {
            Debug.LogError($"[MainMenu] Cannot show '{screenNames[screenNumber]}' because it was not found under '{GetMenuPath()}'.");
            return;
        }

        for (int i = 0; i < screens.Length; i++)
        {
            GameObject screen = screens[i];
            if (screen == null)
                continue;

            screen.SetActive(i == screenNumber);
        }
    }

    private void InitializeScreens()
    {
        if (menu == null)
        {
            Debug.LogError("[MainMenu] Cannot initialize screens because the menu root reference is missing.");
            return;
        }

        Transform menuTransform = menu.transform;
        for (int i = 0; i < screenNames.Length; i++)
        {
            Transform screen = menuTransform.Find(screenNames[i]);
            if (screen == null)
            {
                screens[i] = null;
                Debug.LogError($"[MainMenu] Missing required screen '{screenNames[i]}' under '{GetMenuPath()}'.");
                continue;
            }

            screens[i] = screen.gameObject;
        }
    }

    private bool EnsureScreensInitialized()
    {
        if (menu == null)
        {
            Debug.LogError("[MainMenu] Cannot switch screens because the menu root reference is missing.");
            return false;
        }

        bool initialized = false;
        for (int i = 0; i < screens.Length; i++)
        {
            if (screens[i] != null)
            {
                initialized = true;
                break;
            }
        }

        if (!initialized)
            InitializeScreens();

        return true;
    }

    private bool IsValidScreenIndex(int screenNumber)
    {
        return screenNumber >= 0 && screenNumber < screens.Length;
    }

    private string GetMenuPath()
    {
        if (menu == null)
            return "<missing menu root>";

        Transform current = menu.transform;
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    public void GoBackToMainScreen()
    {
        ShowScreen((int)ScreenType.Main);
    }

    public void GoBackToSettingsScreen()
    {
        ShowScreen((int)ScreenType.Settings);
    }

    public void ShowSettings()
    {
        Debug.Log("[MainMenu] Settings clicked.");
        ShowScreen((int)ScreenType.Settings);
    }

    public void ShowOptions()
    {
        ShowSettings();
    }

    public void ShowSoundSettings()
    {
        ShowScreen((int)ScreenType.Sound);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        MainMenuSettingsController.Instance.SaveMainVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        MainMenuSettingsController.Instance.SaveSFXVolume(value);
    }

    public void ShowGraphicsSettings()
    {
        ShowScreen((int)ScreenType.Graphics);
    }


    public void FullScreen(bool value)
    {
        Screen.fullScreenMode = value ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        MainMenuSettingsController.Instance.SaveFullscreen(value);
    }

    public void ShowKeybindsSettings()
    {
        ShowScreen((int)ScreenType.Controls);
    }

    public void ShowStatisticsScreen()
    {
        PlayerProfileStats.Instance.LoadStats();
        ShowScreen((int)ScreenType.Statistics);
    }

    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        Debug.Log("[MainMenu] Exit requested. Stopping play mode in editor.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("[MainMenu] Exit requested.");
        Application.Quit();
#endif
    }
}
