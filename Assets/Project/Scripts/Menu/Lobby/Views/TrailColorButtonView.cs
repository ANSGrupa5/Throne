using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class TrailColorButtonView
{
    [SerializeField] private Button button;
    [SerializeField] private Image colorImage;
    [SerializeField] private GameObject selectionFrame;
    [SerializeField] private CanvasGroup availabilityGroup;
    [SerializeField] private Color selectionFrameFill = new(0f, 0.996f, 0.925f, 7.0f);
    [SerializeField] private Color selectionFrameOutline = new(0f, 0.996f, 0.925f, 1.0f);
    [SerializeField, Range(0f, 1f)] private float availableAlpha = 0.75f;
    [SerializeField, Range(0f, 1f)] private float selectedAlpha = 0.95f;
    [SerializeField, Range(0f, 1f)] private float unavailableAlpha = 0.35f;

    public Button Button => button;

    public void SetColor(Color color)
    {
        if (colorImage == null && button != null)
            colorImage = ResolveColorImage(button);

        if (colorImage != null)
            colorImage.color = color;
    }

    public void Initialize()
    {
        if (button != null && colorImage == null)
            colorImage = ResolveColorImage(button);

        if (selectionFrame != null && selectionFrame.TryGetComponent(out Graphic frameGraphic))
        {
            frameGraphic.raycastTarget = false;
            Color color = selectionFrameFill;
            color.a = 7f;
            frameGraphic.color = color;
        }

        if (selectionFrame != null && selectionFrame.TryGetComponent(out Outline frameOutline))
        {
            frameOutline.effectColor = selectionFrameOutline;
            frameOutline.effectDistance = new Vector2(1f, -1f);
        }
    }

    public void ApplyState(bool selected, bool unavailable, bool canInteract = true)
    {
        if (selectionFrame != null)
            selectionFrame.SetActive(selected);

        if (button != null)
            button.interactable = canInteract && !unavailable;

        float alpha = unavailable ? unavailableAlpha : selected ? selectedAlpha : availableAlpha;
        if (availabilityGroup != null)
            availabilityGroup.alpha = alpha;

        if (colorImage != null)
        {
            colorImage.transform.localScale = Vector3.one;
            Color color = colorImage.color;
            color.a = alpha;
            colorImage.color = color;
        }
    }

    public void Validate(UnityEngine.Object owner, int index)
    {
        if (button == null)
            Debug.LogError($"{nameof(LobbyController)} is missing trail color button reference at index {index}.", owner);
        if (selectionFrame == null)
            Debug.LogError($"{nameof(LobbyController)} is missing trail color selection frame at index {index}.", owner);
    }

    private static Image ResolveColorImage(Button sourceButton)
    {
        if (sourceButton == null)
            return null;

        Transform preview = sourceButton.transform.Find("ColorPreview");
        if (preview != null && preview.TryGetComponent(out Image previewImage))
            return previewImage;

        return sourceButton.GetComponent<Image>();
    }

#if UNITY_EDITOR
    internal void EditorBind(Button button, Image colorImage, GameObject selectionFrame, CanvasGroup availabilityGroup)
    {
        this.button = button;
        this.colorImage = colorImage;
        this.selectionFrame = selectionFrame;
        this.availabilityGroup = availabilityGroup;
        selectionFrameFill = new Color(0f, 0.996f, 0.925f, 7f);
        selectionFrameOutline = new Color(0f, 0.996f, 0.925f, 1f);
        availableAlpha = 1f;
        selectedAlpha = 1f;
        unavailableAlpha = 0.35f;
    }
#endif
}
