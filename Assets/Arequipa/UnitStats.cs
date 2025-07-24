using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "New Unit Stats", menuName = "Autobattler/Unit Stats")]
public class UnitStats : ScriptableObject
{
    [System.Serializable]
    public class Ability
    {
        [Tooltip("El título de la habilidad.")]
        public string title;
        [Tooltip("El icono de la habilidad.")]
        public Sprite icon;
        [Tooltip("La descripción de la habilidad.")]
        [TextArea(3, 5)]
        public string description;
    }

    [System.Serializable]
    public class SpellCardInfo
    {
        [Tooltip("El título de la carta de hechizo.")]
        public string title;
        [Tooltip("El icono de la carta de hechizo.")]
        public Sprite icon;
        [Tooltip("La descripción de la carta de hechizo.")]
        [TextArea(3, 5)]
        public string description;
    }


    [Header("Unit Configuration")]
    [Tooltip("La categoría de la unidad, que afecta a los límites en el tablero.")]
    public UnitCategory category = UnitCategory.Fabulosa;
    public enum UnitCategory { Fabulosa, Magnifica, Suprema }

    [Tooltip("Define if the unit is melee or ranged.")]
    public UnitType unitType = UnitType.Melee;
    public enum UnitType { Melee, Ranged }

    [Tooltip("The harmonies this unit naturally belongs to.")]
    public List<HarmonyType> naturalHarmonies = new List<HarmonyType>();

    [Header("Visuals & Prefabs")]
    [Tooltip("Icono pequeño que representa a la unidad en la UI.")]
    public Sprite unitIcon;
    public Sprite portrait;
    public Sprite backgroundImage;
    
    [Tooltip("El Prefab del modelo del personaje que se instancia en el tablero.")]
    public GameObject characterPrefab;
    [Tooltip("El Prefab del proyectil que dispara la unidad si es de Rango.")]
    public GameObject projectilePrefab;

    [Header("Combat Statistics")]
    public LocalizedString unitName;
    [Tooltip("Vida máxima de la unidad para los niveles 1, 2 y 3.")]
    public List<int> maxHealthByLevel = new List<int> { 100, 150, 225 };
    [Tooltip("Daño de ataque de la unidad para los niveles 1, 2 y 3.")]
    public List<int> attackDamageByLevel = new List<int> { 10, 15, 25 };
    
    [Range(1, 10)]
    public float attackSpeed = 4f;
    public float attackRange = 1.5f;

    [Header("Movement Statistics")]
    public float moveSpeed = 3f;

    [Header("Unit Abilities")]
    [Tooltip("Lista de habilidades del personaje. Máximo 4.")]
    public List<Ability> abilities = new List<Ability>();

    [Header("Spell Cards")]
    [Tooltip("Cartas de hechizo que esta unidad podría generar o con las que podría interactuar.")]
    public List<SpellCardInfo> spellCards = new List<SpellCardInfo>();
}