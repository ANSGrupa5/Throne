using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[Obsolete("Use MenuSelectable instead.")]
[DisallowMultipleComponent]
[AddComponentMenu("Throne/UI/Button Feedback")]
public sealed class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerDownHandler, ISubmitHandler
{
    [Header("Audio")]
    [FormerlySerializedAs("hooverSound")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;

    private Selectable _selectable;
    private int _lastHoverFrame = -1;
    private int _lastClickFrame = -1;

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

    public float HoverVolume
    {
        get => hoverVolume;
        set => hoverVolume = Mathf.Clamp01(value);
    }

    public float ClickVolume
    {
        get => clickVolume;
        set => clickVolume = Mathf.Clamp01(value);
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();
    }

    private void Reset()
    {
        CacheComponents();
    }

    private void OnValidate()
    {
        hoverVolume = Mathf.Clamp01(hoverVolume);
        clickVolume = Mathf.Clamp01(clickVolume);
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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        TryPlayClickSound();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        TryPlayClickSound();
    }

    private void CacheComponents()
    {
        if (_selectable == null)
            _selectable = GetComponent<Selectable>();
    }

    private bool CanPlayFeedback()
    {
        return isActiveAndEnabled && (_selectable == null || (_selectable.isActiveAndEnabled && _selectable.interactable));
    }

    private void TryPlayClickSound()
    {
        if (!CanPlayFeedback() || _lastClickFrame == Time.frameCount)
            return;

        _lastClickFrame = Time.frameCount;
        PersistentUiAudioPlayer.PlayOneShot(clickSound, clickVolume);
    }

    private void TryPlayHoverSound()
    {
        if (!CanPlayFeedback() || _lastHoverFrame == Time.frameCount)
            return;

        _lastHoverFrame = Time.frameCount;
        PersistentUiAudioPlayer.PlayOneShot(hoverSound, hoverVolume);
    }
}
