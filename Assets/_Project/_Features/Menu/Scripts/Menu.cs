using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public GameObject menu;
    GameObject[] screens = new GameObject[4];

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
        Screen.fullScreen = value;
    }

    public void Exit()
    {
        Application.Quit();
    }
}