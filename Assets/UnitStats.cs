using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Unit Stats", menuName = "Autobattler/Unit Stats")]
public class UnitStats : ScriptableObject
{
    public enum UnitCategory { Fabulosa, Magnifica, Suprema }
    public enum UnitType { Melee, Ranged }

    [Header("Unit Configuration")]
    public UnitCategory category = UnitCategory.Fabulosa;
    public UnitType unitType = UnitType.Melee;
    public List<HarmonyType> naturalHarmonies = new List<HarmonyType>();

    [Header("Visuals & UI")]
    public GameObject characterPrefab;
    public GameObject projectilePrefab;
    public Sprite portrait;

    [Header("Level-Based Statistics")]
    public string unitName = "New Unit";
    
    // --- MODIFICADO: Ahora son arrays para 3 niveles/rondas ---
    [Tooltip("Vida máxima en cada nivel (Ronda 1, Ronda 2, Ronda 3).")]
    public int[] maxHealthByLevel = new int[3] { 100, 150, 200 };
    
    [Tooltip("Daño de ataque en cada nivel (Ronda 1, Ronda 2, Ronda 3).")]
    public int[] attackDamageByLevel = new int[3] { 10, 15, 20 };

    // --- Las siguientes estadísticas se mantienen fijas por ahora ---
    [Header("Fixed Statistics")]
    public int attackDamage = 10;
    public float attackSpeed = 1.0f;
    public int attackRange = 1;
    public float moveSpeed = 3f;
}