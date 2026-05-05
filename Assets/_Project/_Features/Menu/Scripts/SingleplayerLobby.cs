using UnityEngine;
using UnityEngine.SceneManagement;

public class SingleplayerLobby : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    public void InitializeGame()
    {
        // Initialize game settings, spawn points, bots, etc. here
        // This is where you would set up the singleplayer game based on your GameSettings and BotsSettings
    }
}
