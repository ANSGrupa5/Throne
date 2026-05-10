using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BotsSettings", menuName = "Game/Settings/BotsSettings")]
public class BotsSettings : ScriptableObject
{
    [System.Serializable]
    public class BotEntry
    {
        public GameObject prefab;
        [Min(1)] public int count = 1;
    }

    public List<BotEntry> bots = new List<BotEntry>();
}
