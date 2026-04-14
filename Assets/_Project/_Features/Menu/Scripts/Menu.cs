using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public GameObject menu;
    public TMP_Dropdown resolutionDropdown;
    //public TMP_Dropdown qualityDropdown;
    GameObject[] screens = new GameObject[4];
    Resolution[] resolutions;

    enum ScreenType
    {
        Main,     //0
        Options,  //1
        Sound,    //2
        Graphics, //3
        //Camera,   //4
        //Keybinds  //5
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        screens[(int)ScreenType.Main] = menu.transform.Find("MainScreen").gameObject;

        screens[(int)ScreenType.Options] = menu.transform.Find("OptionsScreen").gameObject;
        screens[(int)ScreenType.Options].SetActive(false);

        screens[(int)ScreenType.Sound] = menu.transform.Find("SoundScreen").gameObject;
        screens[(int)ScreenType.Sound].SetActive(false);

        screens[(int)ScreenType.Graphics] = menu.transform.Find("GraphicsScreen").gameObject;
        screens[(int)ScreenType.Graphics].SetActive(false);


        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        int currentResolutionIndex = 0;
        for(int i=0; i<resolutions.Length; i++)
        {
            int hz = (int)resolutions[i].refreshRateRatio.value;
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + hz + "Hz";
            options.Add(option);

            if(resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
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
    }

    public void ShowGraphicsSettings()
    {
        ShowScreen((int)ScreenType.Graphics);
    }

    public void FullScreen(bool value)
    {
        Screen.fullScreenMode = value ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
    }

    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
    }

    //narazie nieu¿ywane
    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    public void Exit()
    {
        Application.Quit();
    }
}