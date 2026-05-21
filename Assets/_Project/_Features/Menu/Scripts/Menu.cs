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
        SettingsManager.Instance.SaveMainVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        SettingsManager.Instance.SaveSFXVolume(value);
    }

    public void ShowGraphicsSettings()
    {
        ShowScreen((int)ScreenType.Graphics);
    }


    public void FullScreen(bool value)
    {
        Screen.fullScreenMode = value ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        SettingsManager.Instance.SaveFullscreen(value);
    }

    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
        SettingsManager.Instance.SaveResolution(res.width, res.height, (int)res.refreshRateRatio.value);
    }

    public void ShowKeybindsSettings()
    {
        ShowScreen((int)ScreenType.Keybinds);
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

    public void Exit()
    {
        Application.Quit();
    }
}