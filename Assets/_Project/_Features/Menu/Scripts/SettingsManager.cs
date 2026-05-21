using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    //Settings
    [SerializeField] private Slider MainVolumeSlider;
    [SerializeField] private Slider SFXVolumeSlider;
    [SerializeField] private Toggle FullscreenToggle;
    //Keybinds
    [SerializeField] private TextMeshProUGUI TurnLeftButtonText;
    [SerializeField] private TextMeshProUGUI TurnRightButtonText;
    [SerializeField] private TextMeshProUGUI CameraButtonText;

    [SerializeField] private Menu menu;

    int keybind;
    KeyCode tempKey;
    bool waitingForKey = false;

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

        LoadSettings();
        Debug.Log("MainVolume: " + PlayerPrefs.GetFloat("MainVolume"));
        Debug.Log("SFXVolume: " + PlayerPrefs.GetFloat("SFXVolume"));
        Debug.Log("Fullscreen: " + PlayerPrefs.GetInt("Fullscreen"));
        Debug.Log("Resolution: " + PlayerPrefs.GetInt("ResolutionWidth") + "x" + PlayerPrefs.GetInt("ResolutionHeight") + "@" + PlayerPrefs.GetInt("ResolutionRefreshRate") + "Hz");
        Debug.Log("Turn Left Key: " + (KeyCode)PlayerPrefs.GetInt("TurnLeft"));
        Debug.Log("Turn Right Key: " + (KeyCode)PlayerPrefs.GetInt("TurnRight"));
        Debug.Log("Change Camera Key: " + (KeyCode)PlayerPrefs.GetInt("Camera"));
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

    public void StartRebind(int k)
    {
        switch (k)
        {
            case 0:
                TurnLeftButtonText.text = "...";
                waitingForKey = true;
                keybind = 0;
                break;
            case 1:
                TurnRightButtonText.text = "...";
                waitingForKey = true;
                keybind = 1;
                break;
            case 2:
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
                TurnLeftButtonText.text = key.ToString();
                break;
            case 1:
                PlayerPrefs.SetInt("TurnRight", (int)key);
                TurnRightButtonText.text = key.ToString();
                break;
            case 2:
                PlayerPrefs.SetInt("Camera", (int)key);
                CameraButtonText.text = key.ToString();
                break;
        }

        PlayerPrefs.Save();
        InputManager.Instance.LoadKeybinds();
    }

    //Ustawienia gracza
    private void LoadSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("MainVolume", 1f);
        MainVolumeSlider.value = PlayerPrefs.GetFloat("MainVolume", 1f);

        menu.SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
        SFXVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (PlayerPrefs.GetInt("Fullscreen", 0) == 1)
        {
            menu.FullScreen(true);
            FullscreenToggle.isOn = true;
        }
        else
        {
            menu.FullScreen(false);
            FullscreenToggle.isOn = false;
        }

        RefreshRate refresh = new RefreshRate();
        refresh.numerator = (uint)PlayerPrefs.GetInt("ResolutionRefreshRate", 60);
        refresh.denominator = 1;
        Screen.SetResolution(PlayerPrefs.GetInt("ResolutionWidth", 800), PlayerPrefs.GetInt("ResolutionHeight", 600), Screen.fullScreenMode, refresh);

        tempKey = (KeyCode)PlayerPrefs.GetInt("TurnLeft", (int)KeyCode.A);
        TurnLeftButtonText.text = tempKey.ToString();

        tempKey = (KeyCode)PlayerPrefs.GetInt("TurnRight", (int)KeyCode.D);
        TurnRightButtonText.text = tempKey.ToString();

        tempKey = (KeyCode)PlayerPrefs.GetInt("Camera", (int)KeyCode.R);
        CameraButtonText.text = tempKey.ToString();
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

    public void SaveResolution(int width, int height, int refreshRateRatio)
    {
        PlayerPrefs.SetInt("ResolutionWidth", width);
        PlayerPrefs.SetInt("ResolutionHeight", height);
        PlayerPrefs.SetInt("ResolutionRefreshRate", refreshRateRatio);
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
