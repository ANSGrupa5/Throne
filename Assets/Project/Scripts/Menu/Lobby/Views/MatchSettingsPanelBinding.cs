using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchSettingsPanelBinding : MonoBehaviour
{
    [SerializeField] private TMP_Text minutesText;
    [SerializeField] private TMP_Text secondsText;
    [SerializeField] private Button minDownButton;
    [SerializeField] private Button minUpButton;
    [SerializeField] private Button secDownButton;
    [SerializeField] private Button secUpButton;
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private Toggle suddenDeathToggle;
    [SerializeField] private TMP_Text trailLengthText;
    [SerializeField] private Button trailLengthButton;

    public TMP_Text MinutesText => minutesText;
    public TMP_Text SecondsText => secondsText;
    public Button MinDownButton => minDownButton;
    public Button MinUpButton => minUpButton;
    public Button SecDownButton => secDownButton;
    public Button SecUpButton => secUpButton;
    public TMP_Dropdown GameModeDropdown => gameModeDropdown;
    public Toggle SuddenDeathToggle => suddenDeathToggle;
    public TMP_Text TrailLengthText => trailLengthText;
    public Button TrailLengthButton => trailLengthButton;

    private void Awake()
    {
        ResolveReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif

    public void ResolveReferences()
    {
        if (minutesText == null)
            minutesText = FindComponentByName<TMP_Text>("TimePreviewMin");
        if (secondsText == null)
            secondsText = FindComponentByName<TMP_Text>("TimePreviewSec");
        if (minDownButton == null)
            minDownButton = FindComponentByName<Button>("MinDownButton");
        if (minUpButton == null)
            minUpButton = FindComponentByName<Button>("MinUpButton");
        if (secDownButton == null)
            secDownButton = FindComponentByName<Button>("SecDownButton");
        if (secUpButton == null)
            secUpButton = FindComponentByName<Button>("SecUpButton");
        if (gameModeDropdown == null)
            gameModeDropdown = FindComponentByName<TMP_Dropdown>("GameModeSelector");
        if (suddenDeathToggle == null)
            suddenDeathToggle = FindComponentByName<Toggle>("SuddenDeathToggle");
        if (trailLengthText == null)
            trailLengthText = FindComponentByName<TMP_Text>("TrailLengthButtonValueText");
        if (trailLengthText == null)
            trailLengthText = FindComponentByName<TMP_Text>("TrailLengthValueText");
        if (trailLengthButton == null)
            trailLengthButton = FindComponentByName<Button>("TrailLengthButton");
    }

    private T FindComponentByName<T>(string objectName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component != null && component.name == objectName)
                return component;
        }

        return null;
    }
}
