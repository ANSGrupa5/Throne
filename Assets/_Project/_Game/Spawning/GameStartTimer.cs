using TMPro;
using UnityEngine;

public class GameStartTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Color countdownColor = Color.white;
    [SerializeField] private Color goColor = Color.red;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip countdownTickClip;
    [SerializeField] private AudioClip matchStartClip;

    private void Awake()
    {
        Hide();
    }

    public void ShowCount(int secondsLeft)
    {
        if (timerText == null)
            return;

        timerText.gameObject.SetActive(true);
        timerText.color = countdownColor;
        timerText.text = secondsLeft.ToString();
        PlayOneShot(countdownTickClip);
    }

    public void ShowGo()
    {
        if (timerText == null)
            return;

        timerText.gameObject.SetActive(true);
        timerText.color = goColor;
        timerText.text = "GO";
        PlayOneShot(matchStartClip);
    }

    public void Hide()
    {
        if (timerText == null)
            return;

        timerText.gameObject.SetActive(false);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}
