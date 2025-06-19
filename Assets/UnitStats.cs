using UnityEngine;

/// <summary>
/// Define las estadísticas base de una unidad usando un ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "New Unit Stats", menuName = "Autobattler/Unit Stats")]
public class UnitStats : ScriptableObject
{
    // Enumeración para definir el tipo de unidad (cuerpo a cuerpo o a distancia).
    public enum UnitType { Melee, Ranged } 

    [Header("Configuración de Unidad")]
    [Tooltip("Define si la unidad es de cuerpo a cuerpo o a distancia.")]
    public UnitType unitType = UnitType.Melee; // Por defecto, las unidades son cuerpo a cuerpo.

    [Header("Estadísticas de Combate")]
    [Tooltip("Nombre de la unidad, útil para depuración o UI.")]
    public string unitName = "New Unit";
    [Tooltip("Salud máxima de la unidad.")]
    public int maxHealth = 100;
    [Tooltip("Daño infligido por la unidad en cada ataque.")]
    public int attackDamage = 10;
    [Tooltip("Velocidad de ataque de la unidad (ataques por segundo).")]
    public float attackSpeed = 1.0f; // Ataques por segundo
    [Tooltip("Rango de ataque de la unidad. Unidades cuerpo a cuerpo tendrán un rango pequeño, las a distancia uno mayor.")]
    public float attackRange = 1.5f;

    [Header("Configuración de Ataque a Distancia")]
    [Tooltip("Prefab del proyectil que esta unidad disparará si es de tipo Ranged.")]
    public GameObject projectilePrefab; // Prefab del proyectil para unidades a distancia

    [Header("Estadísticas de Movimiento")]
    [Tooltip("Velocidad de movimiento de la unidad (unidades de cuadrícula por segundo).")]
    public float moveSpeed = 3f; // Unidades por segundo (Cada unidad puede tener su propia velocidad)
}

