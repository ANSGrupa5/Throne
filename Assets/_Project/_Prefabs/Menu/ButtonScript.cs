using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonScript : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip hooverSound;
    public AudioClip clickSound;
    public float hoverCooldownSeconds = 0.7f;
    public float audioFadeDuration = 0.2f;

    [Header("Color Fade")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.gray;
    public float colorFadeDuration = 0.2f;

    private AudioSource audioSource;
    private Button button;
    private Image buttonImage;

    private float lastPlayedHoverSoundTime = -Mathf.Infinity;
    private Coroutine colorCoroutine;
    private Coroutine audioCoroutine;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        button = GetComponent<Button>();
        buttonImage = button.GetComponent<Image>();

        buttonImage.color = normalColor;
    }

    public void OnHoverEnter()
    {
        // Color fade in
        StartColorFade(hoverColor);

        // Audio fade in with cooldown
        if (Time.time - lastPlayedHoverSoundTime >= hoverCooldownSeconds)
        {
            lastPlayedHoverSoundTime = Time.time;
            StartAudioFadeIn(hooverSound);
        }
    }

    public void OnHoverExit()
    {
        // Color fade out
        StartColorFade(normalColor);
    }

    public void OnClick()
    {
        audioSource.PlayOneShot(clickSound);
    }

    void StartColorFade(Color targetColor)
    {
        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);

        colorCoroutine = StartCoroutine(FadeColor(targetColor));
    }

    IEnumerator FadeColor(Color target)
    {
        Color start = buttonImage.color;
        float time = 0f;

        while (time < colorFadeDuration)
        {
            time += Time.deltaTime;
            buttonImage.color = Color.Lerp(start, target, time / colorFadeDuration);
            yield return null;
        }

        buttonImage.color = target;
    }

    void StartAudioFadeIn(AudioClip clip)
    {
        if (audioCoroutine != null)
            StopCoroutine(audioCoroutine);

        audioCoroutine = StartCoroutine(FadeInSound(clip));
    }

    IEnumerator FadeInSound(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.volume = 0f;
        audioSource.Play();

        float time = 0f;

        while (time < audioFadeDuration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, 1f, time / audioFadeDuration);
            yield return null;
        }

        audioSource.volume = 1f;
    }
}