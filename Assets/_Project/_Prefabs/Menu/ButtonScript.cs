using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Throne/UI/Button Feedback")]
public sealed class ButtonScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    private const float ClickDeduplicationSeconds = 0.05f;

    [Header("References")]
    [SerializeField] private Graphic targetGraphic;

    [Header("Audio")]
    [FormerlySerializedAs("hooverSound")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField, Min(0f)] private float hoverCooldownSeconds = 0.7f;
    [SerializeField, Min(0f)] private float audioFadeDuration = 0.2f;

    [Header("Color Fade")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.gray;
    [SerializeField, Min(0f)] private float colorFadeDuration = 0.2f;

    private AudioSource _audioSource;
    private Selectable _selectable;
    private Coroutine _audioCoroutine;
    private Coroutine _colorCoroutine;
    private bool _isPointerInside;
    private bool _isSelected;
    private bool _isHighlighted;
    private float _lastHoverSoundTime = -Mathf.Infinity;
    private float _lastClickSoundTime = -Mathf.Infinity;

    public AudioClip HoverSound
    {
        get => hoverSound;
        set => hoverSound = value;
    }

    public AudioClip ClickSound
    {
        get => clickSound;
        set => clickSound = value;
    }

    public float HoverCooldownSeconds
    {
        get => hoverCooldownSeconds;
        set => hoverCooldownSeconds = Mathf.Max(0f, value);
    }

    public float AudioFadeDuration
    {
        get => audioFadeDuration;
        set => audioFadeDuration = Mathf.Max(0f, value);
    }

    public Color NormalColor
    {
        get => normalColor;
        set
        {
            normalColor = value;
            ApplyCurrentColorInstantly();
        }
    }

    public Color HoverColor
    {
        get => hoverColor;
        set
        {
            hoverColor = value;
            ApplyCurrentColorInstantly();
        }
    }

    public float ColorFadeDuration
    {
        get => colorFadeDuration;
        set => colorFadeDuration = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        CacheComponents();
        ConfigureAudioSource();
    }

    private void OnEnable()
    {
        CacheComponents();
        ConfigureAudioSource();
        ApplyCurrentColorInstantly();
    }

    private void OnDisable()
    {
        StopRunningCoroutines();
        _isPointerInside = false;
        _isSelected = false;
        _isHighlighted = false;
    }

    private void Reset()
    {
        CacheComponents();
        ConfigureAudioSource();
        ApplyCurrentColorInstantly();
    }

    private void OnValidate()
    {
        hoverCooldownSeconds = Mathf.Max(0f, hoverCooldownSeconds);
        audioFadeDuration = Mathf.Max(0f, audioFadeDuration);
        colorFadeDuration = Mathf.Max(0f, colorFadeDuration);

        CacheComponents();
        ConfigureAudioSource();

        if (!Application.isPlaying)
            ApplyCurrentColorInstantly();
    }

    public void ApplyStyleFrom(ButtonScript source, bool includeColors = true)
    {
        if (source == null || source == this)
            return;

        hoverSound = source.hoverSound;
        clickSound = source.clickSound;
        hoverCooldownSeconds = source.hoverCooldownSeconds;
        audioFadeDuration = source.audioFadeDuration;
        colorFadeDuration = source.colorFadeDuration;

        if (includeColors)
            SetColors(source.normalColor, source.hoverColor);
    }

    public void SetColors(Color normal, Color hover, bool applyImmediately = true)
    {
        normalColor = normal;
        hoverColor = hover;

        if (applyImmediately)
            ApplyCurrentColorInstantly();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerInside = true;
        RefreshHighlightState(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerInside = false;
        RefreshHighlightState(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        PlayClickFeedback();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Pointer click fires on release. Feedback is already played on press.
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        RefreshHighlightState(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        RefreshHighlightState(false);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClickFeedback();
    }

    public void OnHoverEnter()
    {
        _isPointerInside = true;
        RefreshHighlightState(true);
    }

    public void OnHoverExit()
    {
        _isPointerInside = false;
        RefreshHighlightState(false);
    }

    public void OnClick()
    {
        // Legacy EventTrigger hook. Pointer feedback is handled on press.
    }

    private void CacheComponents()
    {
        if (_selectable == null)
            _selectable = GetComponent<Selectable>();

        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (targetGraphic == null)
            targetGraphic = _selectable != null && _selectable.targetGraphic != null
                ? _selectable.targetGraphic
                : GetComponent<Graphic>();
    }

    private void ConfigureAudioSource()
    {
        if (_audioSource == null)
            return;

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
        _audioSource.ignoreListenerPause = true;
    }

    private void RefreshHighlightState(bool playHoverSound)
    {
        bool shouldHighlight = CanPlayFeedback() && (_isPointerInside || _isSelected);
        if (_isHighlighted == shouldHighlight)
            return;

        _isHighlighted = shouldHighlight;
        StartColorFade(_isHighlighted ? hoverColor : normalColor);

        if (_isHighlighted && playHoverSound)
            TryPlayHoverSound();
    }

    private bool CanPlayFeedback()
    {
        return isActiveAndEnabled && (_selectable == null || (_selectable.isActiveAndEnabled && _selectable.interactable));
    }

    private void PlayClickFeedback()
    {
        if (!CanPlayFeedback())
            return;

        float now = Time.unscaledTime;
        if (now - _lastClickSoundTime < ClickDeduplicationSeconds)
            return;

        _lastClickSoundTime = now;
        PersistentUiAudioPlayer.PlayOneShot(clickSound);
    }

    private void TryPlayHoverSound()
    {
        float now = Time.unscaledTime;
        if (now - _lastHoverSoundTime < hoverCooldownSeconds)
            return;

        _lastHoverSoundTime = now;
        StartAudioFadeIn(hoverSound);
    }

    private void StartColorFade(Color targetColor)
    {
        if (targetGraphic == null)
            return;

        if (_colorCoroutine != null)
            StopCoroutine(_colorCoroutine);

        if (colorFadeDuration <= 0f || !Application.isPlaying)
        {
            targetGraphic.color = targetColor;
            _colorCoroutine = null;
            return;
        }

        _colorCoroutine = StartCoroutine(FadeColor(targetColor));
    }

    private IEnumerator FadeColor(Color targetColor)
    {
        Color startColor = targetGraphic.color;
        float elapsed = 0f;

        while (elapsed < colorFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            targetGraphic.color = Color.Lerp(startColor, targetColor, elapsed / colorFadeDuration);
            yield return null;
        }

        targetGraphic.color = targetColor;
        _colorCoroutine = null;
    }

    private void StartAudioFadeIn(AudioClip clip)
    {
        if (clip == null)
            return;

        if (_audioSource == null)
        {
            PersistentUiAudioPlayer.PlayOneShot(clip);
            return;
        }

        if (_audioCoroutine != null)
            StopCoroutine(_audioCoroutine);

        if (audioFadeDuration <= 0f)
        {
            _audioSource.volume = 1f;
            _audioSource.PlayOneShot(clip);
            _audioCoroutine = null;
            return;
        }

        _audioCoroutine = StartCoroutine(FadeInSound(clip));
    }

    private IEnumerator FadeInSound(AudioClip clip)
    {
        _audioSource.clip = clip;
        _audioSource.volume = 0f;
        _audioSource.Play();

        float elapsed = 0f;
        while (elapsed < audioFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _audioSource.volume = Mathf.Lerp(0f, 1f, elapsed / audioFadeDuration);
            yield return null;
        }

        _audioSource.volume = 1f;
        _audioCoroutine = null;
    }

    private void ApplyCurrentColorInstantly()
    {
        CacheComponents();

        if (targetGraphic != null)
            targetGraphic.color = _isHighlighted ? hoverColor : normalColor;
    }

    private void StopRunningCoroutines()
    {
        if (_colorCoroutine != null)
        {
            StopCoroutine(_colorCoroutine);
            _colorCoroutine = null;
        }

        if (_audioCoroutine != null)
        {
            StopCoroutine(_audioCoroutine);
            _audioCoroutine = null;
        }
    }
}
