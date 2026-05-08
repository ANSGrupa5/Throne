using UnityEngine;

[CreateAssetMenu(fileName = "PlayerLook", menuName = "Game/Settings/PlayerLook")]
public class PlayerLook : ScriptableObject
{
    public GameObject playerPrefab;
    public string displayName = "Player";
    public Color trailColor = Color.white;
    // Add visual customization fields here (materials, colors, decals)
}
