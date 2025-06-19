using UnityEngine;
using System.Collections.Generic; // Necessary for List

/// <summary>
/// Define las estadísticas base de una unidad usando un ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "New Unit Stats", menuName = "Autobattler/Unit Stats")]
public class UnitStats : ScriptableObject
{
    // Enumeration for unit type (melee or ranged).
    public enum UnitType { Melee, Ranged } 

    [Header("Unit Configuration")]
    [Tooltip("Defines if the unit is melee or ranged.")]
    public UnitType unitType = UnitType.Melee; // Default to melee.

    [Tooltip("The harmonies this unit naturally belongs to.")]
    public List<HarmonyType> naturalHarmonies = new List<HarmonyType>(); // NEW: List of unit harmonies

    [Header("Combat Statistics")]
    [Tooltip("Unit's name, useful for debugging or UI.")]
    public string unitName = "New Unit";
    [Tooltip("Maximum health of the unit.")]
    public int maxHealth = 100;
    [Tooltip("Damage dealt by the unit per attack.")]
    public int attackDamage = 10;
    [Tooltip("Unit's attack speed (attacks per second).")]
    public float attackSpeed = 1.0f; // Attacks per second
    [Tooltip("Unit's attack range. Melee units will have a small range, ranged units a larger one.")]
    public float attackRange = 1.5f;

    [Header("Ranged Attack Configuration")]
    [Tooltip("Projectile prefab this unit will fire if it's a Ranged type.")]
    public GameObject projectilePrefab; // Projectile prefab for ranged units

    [Header("Movement Statistics")]
    [Tooltip("Unit's movement speed (grid units per second).")]
    public float moveSpeed = 3f; // Units per second (Each unit can have its own speed)
}