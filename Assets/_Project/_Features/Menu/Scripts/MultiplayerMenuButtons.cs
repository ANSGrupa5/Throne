using UnityEngine;

public class MultiplayerMenuButtons : MonoBehaviour
{
    [Header("Optional: assign scene UI panels to avoid GameObject.Find")]
    public GameObject ConnectionTypePanel;
    public GameObject HostPanel;

    public void HostGame()
    {
        if (ConnectionTypePanel != null)
            ConnectionTypePanel.SetActive(false);
        if (HostPanel != null)
            HostPanel.SetActive(true);

        MultiplayerRuntimeBootstrap.Instance?.HostGame();
    }

    public void JoinGame()
    {
        if (ConnectionTypePanel != null)
            ConnectionTypePanel.SetActive(false);
        if (HostPanel != null)
            HostPanel.SetActive(false);

        MultiplayerRuntimeBootstrap.Instance?.JoinGame();
    }

    public void BackToMainMenu()
    {
        MultiplayerRuntimeBootstrap.Instance?.BackToMainMenu();
    }
}