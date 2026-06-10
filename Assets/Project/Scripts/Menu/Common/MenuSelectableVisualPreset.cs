using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Throne/UI/Menu Selectable Visual Preset")]
public sealed class MenuSelectableVisualPreset : ScriptableObject
{
    public Color NormalColor = Color.white;
    public Color HighlightedColor = Color.white;
    public Color PressedColor = Color.white;
    public Color SelectedColor = Color.white;
    public Color DisabledColor = Color.white;

    public float ColorMultiplier = 1f;
    public float FadeDuration = 0.08f;

    public ColorBlock ToColorBlock()
    {
        return new ColorBlock
        {
            normalColor = NormalColor,
            highlightedColor = HighlightedColor,
            pressedColor = PressedColor,
            selectedColor = SelectedColor,
            disabledColor = DisabledColor,
            colorMultiplier = ColorMultiplier,
            fadeDuration = FadeDuration
        };
    }
}
