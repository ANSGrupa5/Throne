using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
[AddComponentMenu("Throne/UI/Menu Selectable")]
public sealed class MenuSelectable : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerDownHandler, ISubmitHandler
{
    [SerializeField] private Selectable selectable;
    [SerializeField] private Graphic targetGraphicOverride;
    [SerializeField] private MenuSelectableVisualPreset visualPreset;
    [SerializeField] private MenuSelectableAudioPreset audioPreset;
    [SerializeField] private bool useColorTint = true;
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private bool playClickSound = true;

    private int _lastHoverFrame = -1;
    private int _lastClickFrame = -1;

    private void Awake()
    {
        CacheSelectable();
        ApplyVisualPreset();
    }

    private void OnEnable()
    {
        CacheSelectable();
        ApplyVisualPreset();
    }

    private void Reset()
    {
        CacheSelectable();
        if (selectable != null)
            targetGraphicOverride = selectable.targetGraphic;
    }

    private void OnValidate()
    {
        CacheSelectable();
        ApplyVisualPreset();
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

    private void CacheSelectable()
    {
        if (selectable == null)
            selectable = GetComponent<Selectable>();
    }

    private void ApplyVisualPreset()
    {
        if (selectable == null)
            return;

        if (targetGraphicOverride != null)
            selectable.targetGraphic = targetGraphicOverride;

        if (!useColorTint)
        {
            selectable.transition = Selectable.Transition.None;
            return;
        }

        if (visualPreset == null)
            return;

        selectable.transition = Selectable.Transition.ColorTint;
        selectable.colors = visualPreset.ToColorBlock();
    }

    private bool CanPlayFeedback()
    {
        return isActiveAndEnabled &&
               selectable != null &&
               selectable.isActiveAndEnabled &&
               selectable.interactable;
    }

    private void TryPlayHoverSound()
    {
        if (!playHoverSound || !CanPlayFeedback() || _lastHoverFrame == Time.frameCount)
            return;

        AudioClip clip = audioPreset != null ? audioPreset.HoverSound : null;
        if (clip == null)
            return;

        _lastHoverFrame = Time.frameCount;
        PersistentUiAudioPlayer.PlayOneShot(clip, audioPreset != null ? audioPreset.HoverVolume : 1f);
    }

    private void TryPlayClickSound()
    {
        if (!playClickSound || !CanPlayFeedback() || _lastClickFrame == Time.frameCount)
            return;

        AudioClip clip = audioPreset != null ? audioPreset.ClickSound : null;
        if (clip == null)
            return;

        _lastClickFrame = Time.frameCount;
        PersistentUiAudioPlayer.PlayOneShot(clip, audioPreset != null ? audioPreset.ClickVolume : 1f);
    }
}
