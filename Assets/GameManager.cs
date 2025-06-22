using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum GameState { Placement, Combat, Result }
    public GameState CurrentState { get; private set; }

    [Header("Reglas del Juego")]
    public int maxUnitsPerTeam = 10;
    
    // --- CORRECCIÓN ---
    // Hemos eliminado la clase 'CategoryLimit' y el array 'categoryLimits[]'
    // que aparecían en el Inspector.

    // El diccionario ahora es privado y se rellenará desde el código.
    private Dictionary<UnitStats.UnitCategory, int> categoryLimitsDict;
    
    private List<UnitController> allUnits = new List<UnitController>();
    private Dictionary<int, int> teamUnitCount = new Dictionary<int, int>();
    private Dictionary<int, Dictionary<UnitStats.UnitCategory, int>> teamCategoryCounts;

    private Dictionary<HarmonyType, Dictionary<int, int>> harmonyCounts = new Dictionary<HarmonyType, Dictionary<int, int>>();
    private Dictionary<HarmonyType, Dictionary<int, int>> activeHarmonyTiers = new Dictionary<HarmonyType, Dictionary<int, int>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        teamCategoryCounts = new Dictionary<int, Dictionary<UnitStats.UnitCategory, int>>();

        // --- CORRECCIÓN ---
        // Aquí es donde definimos los límites fijos para cada categoría.
        // Si en el futuro quieres cambiar un límite, solo tienes que modificar el número aquí.
        categoryLimitsDict = new Dictionary<UnitStats.UnitCategory, int>
        {
            { UnitStats.UnitCategory.Fabulosa, 5 },
            { UnitStats.UnitCategory.Magnifica, 4 },
            { UnitStats.UnitCategory.Suprema, 1 }
        };
    }

    // El resto del script no necesita ningún cambio, ya que utiliza el 'categoryLimitsDict'
    // que ahora rellenamos de forma fija.

    #region Funciones sin cambios
    public void RegisterUnit(UnitController unit)
    {
        if (allUnits.Contains(unit)) return;
        allUnits.Add(unit);
        
        int team = unit.teamID;
        UnitStats.UnitCategory category = unit.unitStats.category;

        if (!teamUnitCount.ContainsKey(team)) teamUnitCount[team] = 0;
        teamUnitCount[team]++;

        if (!teamCategoryCounts.ContainsKey(team))
        {
            teamCategoryCounts[team] = new Dictionary<UnitStats.UnitCategory, int>();
        }
        if (!teamCategoryCounts[team].ContainsKey(category))
        {
            teamCategoryCounts[team][category] = 0;
        }
        teamCategoryCounts[team][category]++;
        
        UpdateHarmonyBonuses(unit, true);
    }

    public void UnregisterUnit(UnitController unit)
    {
        if (!allUnits.Contains(unit)) return;

        int team = unit.teamID;
        UnitStats.UnitCategory category = unit.unitStats.category;

        if (teamUnitCount.ContainsKey(team)) teamUnitCount[team]--;

        if (teamCategoryCounts.ContainsKey(team) && teamCategoryCounts[team].ContainsKey(category))
        {
            teamCategoryCounts[team][category]--;
        }

        allUnits.Remove(unit);
        UpdateHarmonyBonuses(unit, false);
    }

    public bool CanPlaceUnit(int teamID, UnitStats stats)
    {
        int currentTotalCount = 0;
        teamUnitCount.TryGetValue(teamID, out currentTotalCount);
        if (currentTotalCount >= maxUnitsPerTeam)
        {
            Debug.Log($"LÍMITE TOTAL ALCANZADO: Equipo {teamID} ya tiene {currentTotalCount}/{maxUnitsPerTeam} unidades.");
            return false;
        }
        
        UnitStats.UnitCategory category = stats.category;
        int currentCategoryCount = 0;
        
        if (teamCategoryCounts.ContainsKey(teamID))
        {
            teamCategoryCounts[teamID].TryGetValue(category, out currentCategoryCount);
        }
        
        int limitForCategory = 0;
        if(categoryLimitsDict.TryGetValue(category, out limitForCategory))
        {
            if (currentCategoryCount >= limitForCategory)
            {
                Debug.LogWarning($"LÍMITE DE CATEGORÍA ALCANZADO: Equipo {teamID} ya tiene el máximo de unidades '{category}'.");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"No se encontró un límite definido para la categoría '{category}'. Se permitirá la colocación.");
        }
        
        return true;
    }

    public void ResetBoardButton()
    {
        if (CurrentState == GameState.Combat && CombatLoop() != null) StopCoroutine(CombatLoop());
        
        List<UnitController> unitsToDestroy = new List<UnitController>(allUnits);
        foreach (UnitController unit in unitsToDestroy) { if (unit != null) unit.Die(); }
        
        allUnits.Clear();
        harmonyCounts.Clear();
        activeHarmonyTiers.Clear();
        teamUnitCount.Clear();
        teamCategoryCounts.Clear(); 

        UnitIconController[] icons = FindObjectsByType<UnitIconController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UnitIconController icon in icons) icon.ResetIcon();
        
        CurrentState = GameState.Placement;
    }
    
    private void UpdateHarmonyBonuses(UnitController unit, bool isAdding) { }
    public void StartCombatButton() { }
    private IEnumerator CombatLoop() { yield return null; }
    #endregion
}