using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Dropdown))]
[AddComponentMenu("Throne/UI/Menu Dropdown Feedback")]
public sealed class MenuDropdownFeedback : MonoBehaviour, IPointerDownHandler, ISubmitHandler
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip selectSound;
    [Range(0f, 2f)] [SerializeField] private float openVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float selectVolume = 1f;

    private int _lastOpenFrame = -1;
    private int _lastSelectFrame = -1;

    private void Awake()
    {
        CacheDropdown();
    }

    private void OnEnable()
    {
        CacheDropdown();
        if (dropdown != null)
            dropdown.onValueChanged.AddListener(HandleValueChanged);
    }

    private void OnDisable()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(HandleValueChanged);
    }

    private void Reset()
    {
        CacheDropdown();
    }

    private void OnValidate()
    {
        CacheDropdown();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        TryPlayOpenSound();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        TryPlayOpenSound();
    }

    private void CacheDropdown()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();
    }

    private void HandleValueChanged(int _)
    {
        if (!CanPlayFeedback() || _lastSelectFrame == Time.frameCount || selectSound == null)
            return;

        _lastSelectFrame = Time.frameCount;
        PersistentUiAudioPlayer.PlayOneShot(selectSound, selectVolume);
    }

    private void TryPlayOpenSound()
    {
        if (!CanPlayFeedback() || _lastOpenFrame == Time.frameCount || openSound == null)
            return;

        _lastOpenFrame = Time.frameCount;
        PersistentUiAudioPlayer.PlayOneShot(openSound, openVolume);
    }

    private bool CanPlayFeedback()
    {
        return isActiveAndEnabled &&
               dropdown != null &&
               dropdown.isActiveAndEnabled &&
               dropdown.interactable;
    }
}
