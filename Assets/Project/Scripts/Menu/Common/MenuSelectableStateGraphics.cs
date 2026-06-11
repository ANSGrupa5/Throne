using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
[AddComponentMenu("Throne/UI/Menu Selectable State Graphics")]
public sealed class MenuSelectableStateGraphics : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("State")]
    [SerializeField] private Selectable selectable;
    [SerializeField] private MenuSelectionPersistence selectionPersistence = MenuSelectionPersistence.None;

    [Header("Optional Graphics")]
    [SerializeField] private Graphic hoverGlow;
    [SerializeField] private Graphic selectedGlow;
    [SerializeField] private Graphic disabledOverlay;
    [SerializeField] private TMP_Text[] textTargets;

    [Header("Text")]
    [SerializeField] private bool overrideTextColor = true;
    [SerializeField] private Color normalTextColor = new(0.86f, 1f, 0.98f, 1f);
    [SerializeField] private Color disabledTextColor = new(0.28f, 0.42f, 0.44f, 0.78f);

    private bool _hovered;
    private bool _selected;
    private bool _pressed;
    private bool _lastInteractable;

    private void Awake()
    {
        CacheSelectable();
        _lastInteractable = IsInteractable();
        ApplyState();
    }

    private void OnEnable()
    {
        CacheSelectable();
        _lastInteractable = IsInteractable();
        ApplyState();
    }

    private void Update()
    {
        bool interactable = IsInteractable();
        if (interactable == _lastInteractable)
            return;

        _lastInteractable = interactable;
        if (!interactable)
        {
            _hovered = false;
            _selected = false;
            _pressed = false;
        }

        ApplyState();
    }

    private void Reset()
    {
        CacheSelectable();
        textTargets = GetComponentsInChildren<TMP_Text>(true);
    }

    private void OnValidate()
    {
        CacheSelectable();
        ApplyState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        ApplyState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        _pressed = false;
        ApplyState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _selected = true;
        ApplyState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _selected = false;
        _pressed = false;
        ApplyState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        _pressed = true;
        ApplyState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
        ApplyState();
    }

    private void CacheSelectable()
    {
        if (selectable == null)
            selectable = GetComponent<Selectable>();
    }

    private bool IsInteractable()
    {
        return selectable != null && selectable.IsInteractable();
    }

    private void ApplyState()
    {
        bool interactable = IsInteractable();
        bool showSelected = interactable &&
                            _selected &&
                            selectionPersistence != MenuSelectionPersistence.None;
        bool highlighted = interactable && (_hovered || _pressed || showSelected);

        SetGraphicActive(hoverGlow, highlighted);
        SetGraphicActive(selectedGlow, showSelected);
        SetGraphicActive(disabledOverlay, !interactable);

        if (!overrideTextColor || textTargets == null)
            return;

        Color textColor = interactable ? normalTextColor : disabledTextColor;
        for (int i = 0; i < textTargets.Length; i++)
        {
            TMP_Text text = textTargets[i];
            if (text != null)
                text.color = textColor;
        }
    }

    private static void SetGraphicActive(Graphic graphic, bool active)
    {
        if (graphic != null && graphic.gameObject.activeSelf != active)
            graphic.gameObject.SetActive(active);
    }
}
