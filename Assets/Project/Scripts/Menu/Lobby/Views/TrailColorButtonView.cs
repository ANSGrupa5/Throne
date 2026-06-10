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
    [SerializeField] private Color selectionFrameFill = new(0f, 0.996f, 0.925f, 1.0f);
    [SerializeField] private Color selectionFrameOutline = new(0f, 0.996f, 0.925f, 1.0f);
    [SerializeField, Range(0f, 1f)] private float availableAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float unavailableAlpha = 0.35f;

    public Button Button => button;

    public void SetColor(Color color)
    {
        if (colorImage == null && button != null)
            colorImage = button.GetComponent<Image>();

        if (colorImage != null)
            colorImage.color = color;
    }

    public void Initialize()
    {
        if (button != null && colorImage == null)
            colorImage = button.GetComponent<Image>();

        if (selectionFrame != null && selectionFrame.TryGetComponent(out Graphic frameGraphic))
        {
            frameGraphic.raycastTarget = false;
            frameGraphic.color = selectionFrameFill;
        }

        if (selectionFrame != null && selectionFrame.TryGetComponent(out Outline frameOutline))
        {
            frameOutline.effectColor = selectionFrameOutline;
            frameOutline.effectDistance = new Vector2(2f, -2f);
        }
    }

    public void ApplyState(bool selected, bool unavailable, bool canInteract = true)
    {
        if (selectionFrame != null)
            selectionFrame.SetActive(selected);

        if (button != null)
            button.interactable = canInteract && !unavailable;

        float alpha = unavailable ? unavailableAlpha : availableAlpha;
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
}
