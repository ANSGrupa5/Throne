using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public KeyCode TurnLeft;
    public KeyCode TurnRight;
    public KeyCode Camera;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadKeybinds();
    }

    public void LoadKeybinds()
    {
        TurnLeft = (KeyCode)PlayerPrefs.GetInt("TurnLeft", (int)KeyCode.A);
        TurnRight = (KeyCode)PlayerPrefs.GetInt("TurnRight", (int)KeyCode.D);
        Camera = (KeyCode)PlayerPrefs.GetInt("Camera", (int)KeyCode.R);
    }
}
