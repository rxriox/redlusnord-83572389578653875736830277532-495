using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Unit Stats", menuName = "Autobattler/Unit Stats")]
public class UnitStats : ScriptableObject
{
    public enum UnitCategory { Fabulosa, Magnifica, Suprema }

    public enum UnitType { Melee, Ranged }

    [Header("Unit Configuration")]
    [Tooltip("La categoría de la unidad, que afecta a los límites en el tablero.")]
    public UnitCategory category = UnitCategory.Fabulosa;
    
    [Tooltip("Define if the unit is melee or ranged.")]
    public UnitType unitType = UnitType.Melee;

    [Tooltip("The harmonies this unit naturally belongs to.")]
    public List<HarmonyType> naturalHarmonies = new List<HarmonyType>();

    [Header("Visuals & Prefabs")]
    [Tooltip("El Prefab del modelo del personaje que se instancia en el tablero.")]
    public GameObject characterPrefab;

    [Tooltip("El Prefab del proyectil que dispara la unidad si es de Rango.")]
    public GameObject projectilePrefab;

    [Header("Combat Statistics")]
    public string unitName = "New Unit";
    [Tooltip("Vida máxima de la unidad para los niveles 1, 2 y 3.")]
    public List<int> maxHealthByLevel = new List<int> { 100, 150, 225 };
    [Tooltip("Daño de ataque de la unidad para los niveles 1, 2 y 3.")]
    public List<int> attackDamageByLevel = new List<int> { 10, 15, 25 };
    public float attackSpeed = 1.0f;
    public float attackRange = 1.5f;

    [Header("Movement Statistics")]
    public float moveSpeed = 3f;
}