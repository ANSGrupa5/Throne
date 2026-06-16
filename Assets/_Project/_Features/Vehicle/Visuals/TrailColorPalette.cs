using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Throne/Vehicle/Trail Color Palette")]
public sealed class TrailColorPalette : ScriptableObject
{
    [SerializeField] private Color[] colors;

    public IReadOnlyList<Color> Colors => colors;
    public int Count => colors != null ? colors.Length : 0;

    public Color GetDefaultColor(Color fallback)
    {
        return colors != null && colors.Length > 0
            ? SanitizeColor(colors[0], fallback)
            : SanitizeColor(fallback, Color.white);
    }

    public bool TryGetColor(int index, out Color color)
    {
        if (colors != null && index >= 0 && index < colors.Length)
        {
            color = SanitizeColor(colors[index], Color.white);
            return true;
        }

        color = Color.white;
        return false;
    }

    public Color GetColorWrapped(int index, Color fallback)
    {
        if (colors == null || colors.Length == 0)
            return SanitizeColor(fallback, Color.white);

        int wrappedIndex = index % colors.Length;
        if (wrappedIndex < 0)
            wrappedIndex += colors.Length;

        return SanitizeColor(colors[wrappedIndex], fallback);
    }

    public static Color SanitizeColor(Color color, Color fallback)
    {
        if (color.a <= 0.01f)
            color = fallback;

        color.a = 1f;
        return color;
    }
}
