using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuSettingsController : MonoBehaviour
{
    public static MainMenuSettingsController Instance;

    //Settings
    [SerializeField] private Slider MainVolumeSlider;
    [SerializeField] private Slider SFXVolumeSlider;
    [SerializeField] private Toggle FullscreenToggle;
    [Header("Menu Music")]
    [SerializeField] private AudioClip menuMusicClip;
    [SerializeField] private string menuMusicResourcesPath = "Audio/MenuMusic";
    [SerializeField, Range(0f, 1f)] private float menuMusicVolume = 1f;

    //Keybinds
    [SerializeField] private TextMeshProUGUI TurnLeftButtonText;
    [SerializeField] private TextMeshProUGUI TurnRightButtonText;
    [SerializeField] private TextMeshProUGUI CameraButtonText;

    [SerializeField] private MainMenuController menu;

    Resolution[] resolutions;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    int keybind;
    KeyCode tempKey;
    bool waitingForKey = false;
    private AudioSource menuMusicSource;

    enum Keybind
    {
        TurnLeft, //0
        TurnRight, //1
        Camera //2
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        resolutions = Screen.resolutions;
        BindSceneReferences();

        var options = new System.Collections.Generic.List<string>();
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            int hz = (int)resolutions[i].refreshRateRatio.value;
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + hz + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
                currentResolutionIndex = i;
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
            resolutionDropdown.RefreshShownValue();
        }

        LoadSettings();
        InitializeMenuMusic();
        Debug.Log("MainVolume: " + PlayerPrefs.GetFloat("MainVolume"));
        Debug.Log("SFXVolume: " + PlayerPrefs.GetFloat("SFXVolume"));
        Debug.Log("Fullscreen: " + PlayerPrefs.GetInt("Fullscreen"));
        Debug.Log("Resolution: " + PlayerPrefs.GetInt("ResolutionWidth") + "x" + PlayerPrefs.GetInt("ResolutionHeight") + "@" + PlayerPrefs.GetInt("ResolutionRefreshRate") + "Hz");
        Debug.Log("Resolution index: " + PlayerPrefs.GetInt("ResolutionIndex"));
        Debug.Log("Turn Left Key: " + (KeyCode)PlayerPrefs.GetInt("TurnLeft"));
        Debug.Log("Turn Right Key: " + (KeyCode)PlayerPrefs.GetInt("TurnRight"));
        Debug.Log("Change Camera Key: " + (KeyCode)PlayerPrefs.GetInt("Camera"));
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        MatchInitializer.OnMatchStart -= StopMenuMusic;
        Instance = null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (waitingForKey)
            WaitForUserInput(keybind);
    }

    private void InitializeMenuMusic()
    {
        menuMusicSource = GetComponent<AudioSource>();
        if (menuMusicSource == null)
            menuMusicSource = gameObject.AddComponent<AudioSource>();

        menuMusicSource.playOnAwake = false;
        menuMusicSource.loop = true;
        menuMusicSource.volume = menuMusicVolume;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        MatchInitializer.OnMatchStart -= StopMenuMusic;
        MatchInitializer.OnMatchStart += StopMenuMusic;

        PlayMenuMusic();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindSceneReferences();
        LoadSettings();

        if (FindFirstObjectByType<MatchInitializer>(FindObjectsInactive.Include) == null)
        {
            PlayMenuMusic();
            return;
        }

        StopMenuMusic();
    }

    private void PlayMenuMusic()
    {
        if (menuMusicClip == null && !string.IsNullOrWhiteSpace(menuMusicResourcesPath))
            menuMusicClip = Resources.Load<AudioClip>(menuMusicResourcesPath);

        if (menuMusicClip == null || menuMusicSource == null)
            return;

        menuMusicSource.clip = menuMusicClip;
        menuMusicSource.volume = menuMusicVolume;
        menuMusicSource.loop = true;

        if (!menuMusicSource.isPlaying)
            menuMusicSource.Play();
    }

    private void StopMenuMusic()
    {
        if (menuMusicSource != null)
            menuMusicSource.Stop();
    }

    public void StartRebind(int k)
    {
        BindSceneReferences();

        switch (k)
        {
            case 0:
                if (TurnLeftButtonText != null)
                    TurnLeftButtonText.text = "...";
                waitingForKey = true;
                keybind = 0;
                break;
            case 1:
                if (TurnRightButtonText != null)
                    TurnRightButtonText.text = "...";
                waitingForKey = true;
                keybind = 1;
                break;
            case 2:
                if (CameraButtonText != null)
                    CameraButtonText.text = "...";
                waitingForKey = true;
                keybind = 2;
                break;
        }
    }

    public void WaitForUserInput(int keybind)
    {
        if (Input.anyKeyDown)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    ApplyKey(key, keybind);
                    waitingForKey = false;
                    break;
                }
            }
        }
    }

    void ApplyKey(KeyCode key, int keybind)
    {
        switch (keybind)
        {
            case 0:
                PlayerPrefs.SetInt("TurnLeft", (int)key);
                if (TurnLeftButtonText != null)
                    TurnLeftButtonText.text = key.ToString();
                break;
            case 1:
                PlayerPrefs.SetInt("TurnRight", (int)key);
                if (TurnRightButtonText != null)
                    TurnRightButtonText.text = key.ToString();
                break;
            case 2:
                PlayerPrefs.SetInt("Camera", (int)key);
                if (CameraButtonText != null)
                    CameraButtonText.text = key.ToString();
                break;
        }

        PlayerPrefs.Save();
        if (InputManager.Instance != null)
            InputManager.Instance.LoadKeybinds();
    }

    //Ustawienia gracza
    private void LoadSettings()
    {
        BindSceneReferences();

        float mainVolume = PlayerPrefs.GetFloat("MainVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 0) == 1;

        AudioListener.volume = mainVolume;
        if (MainVolumeSlider != null)
            MainVolumeSlider.SetValueWithoutNotify(mainVolume);

        if (SFXVolumeSlider != null)
            SFXVolumeSlider.SetValueWithoutNotify(sfxVolume);

        Screen.fullScreenMode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        if (FullscreenToggle != null)
            FullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        RefreshRate refresh = new RefreshRate();
        refresh.numerator = (uint)PlayerPrefs.GetInt("ResolutionRefreshRate", 60);
        refresh.denominator = 1;
        Screen.SetResolution(PlayerPrefs.GetInt("ResolutionWidth", 800), PlayerPrefs.GetInt("ResolutionHeight", 600), Screen.fullScreenMode, refresh);

        if (resolutionDropdown != null && resolutionDropdown.options.Count > 0)
            resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(PlayerPrefs.GetInt("ResolutionIndex", 0), 0, resolutionDropdown.options.Count - 1));
        
        tempKey = (KeyCode)PlayerPrefs.GetInt("TurnLeft", (int)KeyCode.A);
        if (TurnLeftButtonText != null)
            TurnLeftButtonText.text = tempKey.ToString();

        tempKey = (KeyCode)PlayerPrefs.GetInt("TurnRight", (int)KeyCode.D);
        if (TurnRightButtonText != null)
            TurnRightButtonText.text = tempKey.ToString();

        tempKey = (KeyCode)PlayerPrefs.GetInt("Camera", (int)KeyCode.R);
        if (CameraButtonText != null)
            CameraButtonText.text = tempKey.ToString();
    }

    public void SetResolution(int index)
    {
        if (resolutions == null || resolutions.Length == 0)
            resolutions = Screen.resolutions;

        if (resolutions == null || resolutions.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, resolutions.Length - 1);
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
        SaveResolution(res.width, res.height, (int)res.refreshRateRatio.value, index);
    }

    private void BindSceneReferences()
    {
        if (menu == null)
            menu = FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);

        if (MainVolumeSlider == null)
            MainVolumeSlider = FindComponentByName<Slider>("Slider", "SoundSettingsScreen");

        if (SFXVolumeSlider == null)
            SFXVolumeSlider = FindComponentByName<Slider>("Slider", "SoundSettingsScreen", MainVolumeSlider);

        if (FullscreenToggle == null)
            FullscreenToggle = FindComponentByName<Toggle>("FullscreenToggle", "GraphicsSettingsScreen");

        if (resolutionDropdown == null)
            resolutionDropdown = FindFirstObjectByType<TMP_Dropdown>(FindObjectsInactive.Include);

        TurnLeftButtonText ??= FindKeybindButtonLabel("Keybind_TurnLeft");
        TurnRightButtonText ??= FindKeybindButtonLabel("Keybind_TurnRight");
        CameraButtonText ??= FindKeybindButtonLabel("Keybind_Camera");
    }

    private static TextMeshProUGUI FindKeybindButtonLabel(string keybindObjectName)
    {
        Transform keybind = FindTransformByName(keybindObjectName);
        if (keybind == null)
            return null;

        Button button = keybind.GetComponentInChildren<Button>(true);
        return button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }

    private static T FindComponentByName<T>(string objectName, string parentName, T exclude = null) where T : Component
    {
        T[] components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component == null || component == exclude || component.gameObject.name != objectName)
                continue;

            if (string.IsNullOrEmpty(parentName) || HasParentNamed(component.transform, parentName))
                return component;
        }

        return null;
    }

    private static Transform FindTransformByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform != null && transform.name == objectName)
                return transform;
        }

        return null;
    }

    private static bool HasParentNamed(Transform transform, string parentName)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == parentName)
                return true;

            current = current.parent;
        }

        return false;
    }

    public void SaveMainVolume(float value)
    {
        PlayerPrefs.SetFloat("MainVolume", value);
        PlayerPrefs.Save();
    }

    public void SaveSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    public void SaveFullscreen(bool value)
    {
        if (value)
            PlayerPrefs.SetInt("Fullscreen", 1);
        else
            PlayerPrefs.SetInt("Fullscreen", 0);
        PlayerPrefs.Save();
    }

    public void SaveResolution(int width, int height, int refreshRateRatio, int index)
    {
        PlayerPrefs.SetInt("ResolutionWidth", width);
        PlayerPrefs.SetInt("ResolutionHeight", height);
        PlayerPrefs.SetInt("ResolutionRefreshRate", refreshRateRatio);
        PlayerPrefs.SetInt("ResolutionIndex", index);
        PlayerPrefs.Save();
    }

    public void SaveTurnLeftKeybind()
    {
        PlayerPrefs.Save();
    }

    public void SaveTurnRightKeybind()
    {
        PlayerPrefs.Save();
    }

    public void SaveCameraKeybind()
    {
        PlayerPrefs.Save();
    }
}
