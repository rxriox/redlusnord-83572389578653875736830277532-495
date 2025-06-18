using UnityEngine;

/// <summary>
/// Define las estadísticas base de una unidad usando un ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "New Unit Stats", menuName = "Autobattler/Unit Stats")]
public class UnitStats : ScriptableObject
{
    [Header("Estadísticas de Combate")]
    public string unitName = "New Unit";
    public int maxHealth = 100;
    public int attackDamage = 10;
    public float attackSpeed = 1.0f; // Ataques por segundo
    public float attackRange = 1.5f;
}