using UnityEngine;

[CreateAssetMenu(fileName = "PlayerLook", menuName = "Game/Settings/PlayerLook")]
public class PlayerLook : ScriptableObject
{
    public GameObject playerPrefab;
    public string displayName = "Player";
    // Add visual customization fields here (materials, colors, decals)
}
