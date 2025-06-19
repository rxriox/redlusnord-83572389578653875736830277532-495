using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Define un tipo de armonía (sinergia) en el juego.
/// Cada unidad puede pertenecer a uno o más tipos de armonía.
/// Las armonías otorgan bonificaciones cuando se alcanzan ciertos umbrales de unidades.
/// </summary>
[CreateAssetMenu(fileName = "New Harmony Type", menuName = "Autobattler/Harmony Type")]
public class HarmonyType : ScriptableObject
{
    [Tooltip("Nombre de la armonía (ej. Guerrero, Mago, Pícaro).")]
    public string harmonyName;

    [Tooltip("Icono visual para representar esta armonía en la UI.")]
    public Sprite icon;

    [Tooltip("Descripción de la armonía.")]
    [TextArea(3, 5)]
    public string description;

    [System.Serializable]
    public class HarmonyBonusTier
    {
        [Tooltip("Número de unidades requeridas para activar este tier de bonificación.")]
        public int unitsRequired;
        [Tooltip("Descripción del bono que se otorga en este tier.")]
        [TextArea(2, 3)]
        public string bonusDescription;
        // You can add actual effect variables here (e.g., float attackDamageBonus; int healthBonus;)
        // For now, these are just descriptive.
    }

    [Tooltip("Lista de tiers de bonificación para esta armonía. Ordena de menor a mayor unidades requeridas.")]
    public List<HarmonyBonusTier> bonusTiers = new List<HarmonyBonusTier>();
}