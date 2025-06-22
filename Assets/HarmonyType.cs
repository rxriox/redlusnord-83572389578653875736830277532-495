using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Harmony", menuName = "Autobattler/Harmony Type")]
public class HarmonyType : ScriptableObject
{
    public string harmonyName;
    [TextArea(3, 5)]
    public string description;

    // AÑADIDO: Campos para los iconos de la UI
    [Header("UI Display")]
    [Tooltip("Icono que se muestra cuando la armonía está activa.")]
    public Sprite activeIcon;
    [Tooltip("Icono que se muestra cuando la armonía está inactiva (en progreso).")]
    public Sprite inactiveIcon;

    [System.Serializable]
    public class HarmonyTier
    {
        public int unitsRequired;
        public string tierDescription;
    }

    public List<HarmonyTier> tiers = new List<HarmonyTier>();
}