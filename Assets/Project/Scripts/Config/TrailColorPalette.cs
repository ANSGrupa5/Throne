using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrailColorPalette", menuName = "Game/Settings/Trail Color Palette")]
public sealed class TrailColorPalette : ScriptableObject
{
    [Header("Colors")]
    [SerializeField] private List<Color> colors = new();

    public IReadOnlyList<Color> Colors => colors;
    public int Count => colors?.Count ?? 0;

    public Color GetColorOrDefault(int index, Color fallback)
    {
        if (colors == null || colors.Count == 0)
            return fallback;

        return colors[Mathf.Clamp(index, 0, colors.Count - 1)];
    }
}
