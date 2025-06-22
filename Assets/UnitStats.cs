using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Unit Stats", menuName = "Autobattler/Unit Stats")]
public class UnitStats : ScriptableObject
{
    public enum UnitType { Melee, Ranged }

    [Header("Unit Configuration")]
    [Tooltip("Defines if the unit is melee or ranged.")]
    public UnitType unitType = UnitType.Melee;

    [Tooltip("The harmonies this unit naturally belongs to.")]
    public List<HarmonyType> naturalHarmonies = new List<HarmonyType>();

    [Header("Visuals & Prefabs")]
    [Tooltip("El Prefab del modelo del personaje que se instancia en el tablero.")]
    public GameObject characterPrefab; // <-- VARIABLE AÑADIDA PARA EL MODELO

    [Tooltip("Projectile prefab this unit will fire if it's a Ranged type.")]
    public GameObject projectilePrefab;

    [Header("Combat Statistics")]
    [Tooltip("Unit's name, useful for debugging or UI.")]
    public string unitName = "New Unit";
    [Tooltip("Maximum health of the unit.")]
    public int maxHealth = 100;
    [Tooltip("Maximum mana of the unit.")]
    public int maxMana = 100; // <-- VARIABLE AÑADIDA PARA EL MANÁ
    [Tooltip("Damage dealt by the unit per attack.")]
    public int attackDamage = 10;
    [Tooltip("Unit's attack speed (attacks per second).")]
    public float attackSpeed = 1.0f;
    [Tooltip("Unit's attack range.")]
    public float attackRange = 1.5f;

    [Header("Movement Statistics")]
    [Tooltip("Unit's movement speed (grid units per second).")]
    public float moveSpeed = 3f;
}