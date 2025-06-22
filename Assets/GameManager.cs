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
    [Tooltip("El número máximo de unidades que cada equipo puede tener en el tablero.")]
    public int maxUnitsPerTeam = 10;

    // --- NUEVO SISTEMA DE LÍMITES POR CATEGORÍA ---
    [System.Serializable]
    public class CategoryLimit
    {
        public UnitStats.UnitCategory category;
        public int limit;
    }

    [Tooltip("Define los límites de unidades por cada categoría.")]
    public CategoryLimit[] categoryLimits;

    private Dictionary<UnitStats.UnitCategory, int> categoryLimitsDict;
    private Dictionary<UnitStats.UnitCategory, int> playerCategoryCount = new Dictionary<UnitStats.UnitCategory, int>();
    // --- FIN NUEVO SISTEMA ---

    private List<UnitController> allUnits = new List<UnitController>();
    private Dictionary<int, int> teamUnitCount = new Dictionary<int, int>();

    private Dictionary<HarmonyType, Dictionary<int, int>> harmonyCounts = new Dictionary<HarmonyType, Dictionary<int, int>>();
    private Dictionary<HarmonyType, Dictionary<int, int>> activeHarmonyTiers = new Dictionary<HarmonyType, Dictionary<int, int>>();

    private Coroutine combatCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        categoryLimitsDict = new Dictionary<UnitStats.UnitCategory, int>();
        foreach (var limitInfo in categoryLimits)
        {
            categoryLimitsDict[limitInfo.category] = limitInfo.limit;
        }
    }

    public void RegisterUnit(UnitController unit)
    {
        if (allUnits.Contains(unit)) return;

        allUnits.Add(unit);

        int team = unit.teamID;
        if (!teamUnitCount.ContainsKey(team)) teamUnitCount[team] = 0;
        teamUnitCount[team]++;

        if (team == 0)
        {
            UnitStats.UnitCategory category = unit.unitStats.category;
            if (!playerCategoryCount.ContainsKey(category)) playerCategoryCount[category] = 0;
            playerCategoryCount[category]++;
            Debug.Log($"Unidad de categoría '{category}' registrada. Total: {playerCategoryCount[category]}");
        }

        UpdateHarmonyBonuses(unit, true);
    }

    public void UnregisterUnit(UnitController unit)
    {
        if (!allUnits.Contains(unit)) return;

        int team = unit.teamID;
        if (teamUnitCount.ContainsKey(team)) teamUnitCount[team]--;

        if (team == 0)
        {
            UnitStats.UnitCategory category = unit.unitStats.category;
            if (playerCategoryCount.ContainsKey(category))
            {
                playerCategoryCount[category]--;
                Debug.Log($"Unidad de categoría '{category}' des-registrada. Total: {playerCategoryCount[category]}");
            }
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
            Debug.Log($"Límite total de {maxUnitsPerTeam} unidades alcanzado para equipo {teamID}.");
            return false;
        }

        if (teamID == 0)
        {
            UnitStats.UnitCategory category = stats.category;
            int currentCategoryCount = 0;
            playerCategoryCount.TryGetValue(category, out currentCategoryCount);

            int limitForCategory = 0;
            if (categoryLimitsDict.TryGetValue(category, out limitForCategory))
            {
                if (currentCategoryCount >= limitForCategory)
                {
                    Debug.Log($"Límite de {limitForCategory} unidades para la categoría '{category}' alcanzado.");
                    return false;
                }
            }
        }

        return true;
    }

    public void StartCombatButton()
    {
        if (CurrentState == GameState.Placement)
        {
            CurrentState = GameState.Combat;
            combatCoroutine = StartCoroutine(CombatLoop());
        }
    }

    private IEnumerator CombatLoop()
    {
        while (CurrentState == GameState.Combat)
        {
            foreach (var unit in allUnits.ToList())
            {
                if (unit != null)
                    unit.EvaluateAction();
            }
            yield return null;
        }
    }

    public void ResetBoardButton()
    {
        if (CurrentState == GameState.Combat && combatCoroutine != null)
        {
            StopCoroutine(combatCoroutine);
            combatCoroutine = null;
        }

        List<UnitController> unitsToDestroy = new List<UnitController>(allUnits);
        foreach (UnitController unit in unitsToDestroy)
        {
            if (unit != null)
                unit.Die();
        }

        allUnits.Clear();
        harmonyCounts.Clear();
        activeHarmonyTiers.Clear();
        teamUnitCount.Clear();
        playerCategoryCount.Clear();

        UnitIconController[] icons = FindObjectsByType<UnitIconController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UnitIconController icon in icons)
        {
            icon.ResetIcon();
        }

        CurrentState = GameState.Placement;
        Debug.Log("Tablero reiniciado. Cuentas de unidades a cero.");
    }

    // Métodos de armonía aún pendientes o placeholders
    private void UpdateHarmonyBonuses(UnitController unit, bool isAdding) { }

    public void UpdateAllUnitBonuses() { }

    public bool IsHarmonyTierActive(UnitController unit, int tierIndex) { return false; }
}
