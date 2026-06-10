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
    //public TMP_Dropdown qualityDropdown;
    GameObject[] screens = new GameObject[6];

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
        SceneTransitionLoader.LoadScene(sceneName);
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
        ShowScreen((int)ScreenType.Keybinds);
    }

    public void ShowStatisticsScreen()
    {
        PlayerProfileStats.Instance.LoadStats();
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
