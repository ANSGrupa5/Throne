using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class Menu : MonoBehaviour
{
    public GameObject menu;
    public TMP_Dropdown resolutionDropdown;
    //public TMP_Dropdown qualityDropdown;
    GameObject[] screens = new GameObject[6];
    Resolution[] resolutions;

    //Settings
    [SerializeField] private Slider MainVolumeSlider;
    [SerializeField] private Slider SFXVolumeSlider;
    [SerializeField] private Toggle FullscreenToggle;
    //Keybinds
    [SerializeField] private TextMeshProUGUI TurnLeftButtonText;
    [SerializeField] private TextMeshProUGUI TurnRightButtonText;
    [SerializeField] private TextMeshProUGUI CameraButtonText;

    //private KeyCode TurnLeft;
    //private KeyCode TurnRight;
    //private KeyCode Camera;
    int keybind;
    bool waitingForKey = false;

    enum ScreenType
    {
        Main,      //0
        Options,   //1
        Sound,     //2
        Graphics,  //3
        Keybinds,  //4
        Statistics //5
        //Camera     //6
    }

    enum Keybind
    {
        TurnLeft, //0
        TurnRight, //1
        Camera //2
    }

    void Awake()
    {
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
        screens[(int)ScreenType.Main] = menu.transform.Find("MainScreen").gameObject;
        screens[(int)ScreenType.Main].SetActive(true);

        screens[(int)ScreenType.Options] = menu.transform.Find("OptionsScreen").gameObject;
        screens[(int)ScreenType.Options].SetActive(false);

        screens[(int)ScreenType.Sound] = menu.transform.Find("SoundScreen").gameObject;
        screens[(int)ScreenType.Sound].SetActive(false);

        screens[(int)ScreenType.Graphics] = menu.transform.Find("GraphicsScreen").gameObject;
        screens[(int)ScreenType.Graphics].SetActive(false);

        screens[(int)ScreenType.Keybinds] = menu.transform.Find("KeybindsScreen").gameObject;
        screens[(int)ScreenType.Keybinds].SetActive(false);

        screens[(int)ScreenType.Statistics] = menu.transform.Find("StatisticsScreen").gameObject;
        screens[(int)ScreenType.Statistics].SetActive(false);


        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        int currentResolutionIndex = 0;
        for (int i = 0; i<resolutions.Length; i++)
        {
            int hz = (int)resolutions[i].refreshRateRatio.value;
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + hz + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
                currentResolutionIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();


        //qualityDropdown.ClearOptions();
        //qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        //qualityDropdown.value = QualitySettings.GetQualityLevel();
        //qualityDropdown.RefreshShownValue();
    }

    // Update is called once per frame
    void Update()
    {
        if (waitingForKey)
            WaitForUserInput(keybind);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void ShowScreen(int screenNumber)
    {
        for (int i = 0; i < screens.Length; i++)
        {
            if (i == screenNumber)
                screens[i].SetActive(true);
            else
                screens[i].SetActive(false);
        }
    }

    public void GoBackToMainScreen()
    {
        ShowScreen((int)ScreenType.Main);
    }

    public void ShowOptions()
    {
        ShowScreen((int)ScreenType.Options);
    }

    public void ShowSoundSettings()
    {
        ShowScreen((int)ScreenType.Sound);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        SaveMainVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        SaveSFXVolume(value);
    }

    public void ShowGraphicsSettings()
    {
        ShowScreen((int)ScreenType.Graphics);
    }


    public void FullScreen(bool value)
    {
        Screen.fullScreenMode = value ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        SaveFullscreen(value);
    }

    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
        SaveResolution(res.width, res.height, (int)res.refreshRateRatio.value);
    }

    public void ShowKeybindsSettings()
    {
        ShowScreen((int)ScreenType.Keybinds);
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

    public void ShowStatisticsScreen()
    {
        ShowScreen((int)ScreenType.Statistics);
    }

    //narazie nieuzywane
    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    //Ustawienia gracza
    private void LoadSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("MainVolume", 1f);
        MainVolumeSlider.value = PlayerPrefs.GetFloat("MainVolume", 1f);

        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
        SFXVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (PlayerPrefs.GetInt("Fullscreen", 0) == 1)
        {
            FullScreen(true);
            FullscreenToggle.isOn = true;
        }
        else
        {
            FullScreen(false);
            FullscreenToggle.isOn = false;
        }

        RefreshRate refresh = new RefreshRate();
        refresh.numerator = (uint)PlayerPrefs.GetInt("ResolutionRefreshRate", 60);
        refresh.denominator = 1;
        Screen.SetResolution(PlayerPrefs.GetInt("ResolutionWidth", 800), PlayerPrefs.GetInt("ResolutionHeight", 600), Screen.fullScreenMode, refresh);

        ApplyKey((KeyCode)PlayerPrefs.GetInt("TurnLeft", (int)KeyCode.A), 0);
        ApplyKey((KeyCode)PlayerPrefs.GetInt("TurnRight", (int)KeyCode.D), 1);
        ApplyKey((KeyCode)PlayerPrefs.GetInt("Camera", (int)KeyCode.R), 2);
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
        //PlayerPrefs.SetInt("TurnLeft", (int)TurnLeft);
        PlayerPrefs.Save();
    }

    public void SaveTurnRightKeybind()
    {
        //PlayerPrefs.SetInt("TurnRight", (int)TurnRight);
        PlayerPrefs.Save();
    }

    public void SaveCameraKeybind()
    {
        //PlayerPrefs.SetInt("Camera", (int)Camera);
        PlayerPrefs.Save();
    }

    public void Exit()
    {
        Application.Quit();
    }
}