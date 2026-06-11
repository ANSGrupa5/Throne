using UnityEngine;
using UnityEngine.UI;

public sealed class TrailColorSelectionPanelBinding : MonoBehaviour
{
    [SerializeField] private TrailColorButtonView[] trailColorButtons;

    public TrailColorButtonView[] TrailColorButtons => trailColorButtons;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (trailColorButtons != null && trailColorButtons.Length > 0)
            return;

        EditorRebind();
    }

    public void EditorRebind()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (buttons == null || buttons.Length == 0)
            return;

        trailColorButtons = new TrailColorButtonView[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            Transform buttonTransform = button.transform;
            Transform preview = buttonTransform.Find("ColorPreview");
            Transform frame = buttonTransform.Find("SelectionFrame");

            trailColorButtons[i] = new TrailColorButtonView();
            trailColorButtons[i].EditorBind(
                button,
                preview != null ? preview.GetComponent<Image>() : button.GetComponent<Image>(),
                frame != null ? frame.gameObject : null,
                button.GetComponent<CanvasGroup>());
        }
    }
#endif
}
