using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Harmony", menuName = "Autobattler/Harmony Type")]
public class HarmonyType : ScriptableObject
{
    public string harmonyName;
    [TextArea(3, 5)]
    public string description;
    [Header("UI Display")]
    public Sprite activeIcon;
    public List<Sprite> inactiveIcons = new List<Sprite>();

    [System.Serializable]
    public class HarmonyTier
    {
        public int unitsRequired;
        public string tierDescription;
    }

    public List<HarmonyTier> tiers = new List<HarmonyTier>();
}