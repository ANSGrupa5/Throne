using UnityEngine;

public class EndGameCheat : MonoBehaviour
{
    private EndGameController _endGameController;

    private void Start()
    {
        _endGameController = FindObjectOfType<EndGameController>();
        if (_endGameController == null)
            Debug.LogWarning("[EndGameCheat] EndGameController not found in scene.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            TriggerEndGame();
        }
    }

    private void TriggerEndGame()
    {
        if (_endGameController == null)
        {
            _endGameController = FindObjectOfType<EndGameController>();
            if (_endGameController == null)
            {
                Debug.LogError("[EndGameCheat] Cannot find EndGameController!");
                return;
            }
        }

        Debug.Log("[EndGameCheat] F4 pressed - triggering end game manually!");
        _endGameController.BeginEndSequence("Manual");
    }
}
