using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Throne/UI/Button Feedback")]
public sealed class ButtonScript : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private const float ClickDeduplicationSeconds = 0.05f;

    [Header("Audio")]
    [FormerlySerializedAs("hooverSound")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField, Min(0f)] private float hoverCooldownSeconds = 0.7f;

    private Button _button;
    private Selectable _selectable;
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

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();
        if (_button != null)
            _button.onClick.AddListener(PlayClickFeedback);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(PlayClickFeedback);
    }

    private void Reset()
    {
        CacheComponents();
    }

    private void OnValidate()
    {
        hoverCooldownSeconds = Mathf.Max(0f, hoverCooldownSeconds);
        CacheComponents();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TryPlayHoverSound();
    }

    public void OnSelect(BaseEventData eventData)
    {
        TryPlayHoverSound();
    }

    private void CacheComponents()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_selectable == null)
            _selectable = GetComponent<Selectable>();
    }

    private bool CanPlayFeedback()
    {
        return isActiveAndEnabled && (_selectable == null || (_selectable.isActiveAndEnabled && _selectable.interactable));
    }

    private void PlayClickFeedback()
    {
        float now = Time.unscaledTime;
        if (now - _lastClickSoundTime < ClickDeduplicationSeconds)
            return;

        _lastClickSoundTime = now;
        PersistentUiAudioPlayer.PlayOneShot(clickSound);
    }

    private void TryPlayHoverSound()
    {
        if (!CanPlayFeedback())
            return;

        float now = Time.unscaledTime;
        if (now - _lastHoverSoundTime < hoverCooldownSeconds)
            return;

        _lastHoverSoundTime = now;
        PersistentUiAudioPlayer.PlayOneShot(hoverSound);
    }
}
