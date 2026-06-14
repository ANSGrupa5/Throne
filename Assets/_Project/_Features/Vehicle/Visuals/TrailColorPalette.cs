using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Throne/Vehicle/Trail Color Palette")]
public sealed class TrailColorPalette : ScriptableObject
{
    [SerializeField] private Color[] colors;

    public IReadOnlyList<Color> Colors => colors;

    public Color GetDefaultColor(Color fallback)
    {
        return colors != null && colors.Length > 0 ? colors[0] : fallback;
    }
}
