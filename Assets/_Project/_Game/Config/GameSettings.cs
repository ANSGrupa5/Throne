using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Settings/GameSettings")]
public class GameSettings : ScriptableObject
{
    [Min(2)] public int maxPlayers = 4;
    [Min(10f)] public float matchDuration = 180f;
    [Min(1f)] public float respawnTime = 5f;
    public bool isSuddenDeath = false;
}
