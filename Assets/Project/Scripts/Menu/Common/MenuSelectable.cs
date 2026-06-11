using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
[AddComponentMenu("Throne/UI/Menu Selectable")]
public sealed class MenuSelectable : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
{
    [Header("Target")]
    [SerializeField] private Selectable selectable;
    [SerializeField] private Graphic targetGraphicOverride;

    [Header("Presets")]
    [SerializeField] private MenuSelectableVisualPreset visualPreset;
    [SerializeField] private MenuSelectableAudioPreset audioPreset;

    [Header("Behavior")]
    [SerializeField] private MenuSelectionPersistence selectionPersistence = MenuSelectionPersistence.None;
    [SerializeField] private bool clearEventSystemSelectionOnPointerUp = true;
    [SerializeField] private bool useColorTint = true;

    [Header("Audio")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private bool playClickSound = true;

    private int _lastHoverFrame = -1;
    private int _lastClickFrame = -1;
    private Coroutine _clearSelectionRoutine;

    public MenuSelectionPersistence SelectionPersistence => selectionPersistence;

    private void Awake()
    {
        CacheSelectable();
        ApplyVisualPreset();
        BindSelectionCallbacks();
    }

    private void OnEnable()
    {
        CacheSelectable();
        ApplyVisualPreset();
        BindSelectionCallbacks();
    }

    private void OnDisable()
    {
        UnbindSelectionCallbacks();
        if (_clearSelectionRoutine != null)
        {
            StopCoroutine(_clearSelectionRoutine);
            _clearSelectionRoutine = null;
        }
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

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        if (selectionPersistence == MenuSelectionPersistence.None && clearEventSystemSelectionOnPointerUp)
            ClearSelectionAtEndOfFrame();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        TryPlayClickSound();

        if (selectionPersistence == MenuSelectionPersistence.None && clearEventSystemSelectionOnPointerUp)
            ClearSelectionAtEndOfFrame();
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

    private void BindSelectionCallbacks()
    {
        if (selectable is TMPro.TMP_Dropdown dropdown)
        {
            dropdown.onValueChanged.RemoveListener(HandleDropdownValueChanged);
            dropdown.onValueChanged.AddListener(HandleDropdownValueChanged);
        }
    }

    private void UnbindSelectionCallbacks()
    {
        if (selectable is TMPro.TMP_Dropdown dropdown)
            dropdown.onValueChanged.RemoveListener(HandleDropdownValueChanged);
    }

    private void HandleDropdownValueChanged(int _)
    {
        if (selectionPersistence == MenuSelectionPersistence.WhileInteracting)
            ClearSelectionAtEndOfFrame();
    }

    private void ClearSelectionAtEndOfFrame()
    {
        if (!isActiveAndEnabled)
            return;

        if (_clearSelectionRoutine != null)
            StopCoroutine(_clearSelectionRoutine);

        _clearSelectionRoutine = StartCoroutine(ClearSelectionRoutine());
    }

    private IEnumerator ClearSelectionRoutine()
    {
        yield return null;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject == gameObject)
            eventSystem.SetSelectedGameObject(null);

        _clearSelectionRoutine = null;
    }
}
