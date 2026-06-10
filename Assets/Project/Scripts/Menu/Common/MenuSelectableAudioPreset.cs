using UnityEngine;

[CreateAssetMenu(menuName = "Throne/UI/Menu Selectable Audio Preset")]
public sealed class MenuSelectableAudioPreset : ScriptableObject
{
    public AudioClip HoverSound;
    public AudioClip ClickSound;

    [Range(0f, 1f)] public float HoverVolume = 0.55f;
    [Range(0f, 1f)] public float ClickVolume = 1f;
}
