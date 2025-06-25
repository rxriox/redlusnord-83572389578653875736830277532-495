using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour

{
    [Header("Referencias del Sistema")]
    public GridManager gridManager;
    private struct CombatStartInfo
    {
        public UnitStats stats;
        public int teamID;
        public Node startingNode;
        public UnitIconController originatingIcon;
    }
    private List<CombatStartInfo> unitsAtCombatStart = new List<CombatStartInfo>();
    public static event System.Action OnHarmoniesUpdated;
    public static GameManager Instance { get; private set; }
    public enum GameState { Placement, Combat, Result }
    public GameState CurrentState { get; private set; }

    [Header("Reglas del Juego")]
    public int maxUnitsPerTeam = 10;
    private Dictionary<UnitStats.UnitCategory, int> categoryLimitsDict;

    private List<UnitController> allUnits = new List<UnitController>();
    private Dictionary<int, int> teamUnitCount = new Dictionary<int, int>();
    private Dictionary<int, Dictionary<UnitStats.UnitCategory, int>> teamCategoryCounts;

    private Dictionary<HarmonyType, Dictionary<int, int>> harmonyCounts = new Dictionary<HarmonyType, Dictionary<int, int>>();
    private Dictionary<HarmonyType, Dictionary<int, int>> activeHarmonyTiers = new Dictionary<HarmonyType, Dictionary<int, int>>();
    public int GetUnitCountForTeam(int teamID)
    {
        teamUnitCount.TryGetValue(teamID, out int count);
        return count;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        teamCategoryCounts = new Dictionary<int, Dictionary<UnitStats.UnitCategory, int>>();
        categoryLimitsDict = new Dictionary<UnitStats.UnitCategory, int>
        {
// CHARACTER CATEGORY LIMIT IN BOARD
            { UnitStats.UnitCategory.Fabulosa, 5 },
            { UnitStats.UnitCategory.Magnifica, 4 },
            { UnitStats.UnitCategory.Suprema, 1 }
        };
    }

    public Dictionary<HarmonyType, int> GetHarmonyCountsForTeam(int teamID)
    {
        var counts = new Dictionary<HarmonyType, int>();
        foreach (var harmonyPair in harmonyCounts)
        {
            if (harmonyPair.Value.ContainsKey(teamID) && harmonyPair.Value[teamID] > 0)
            {
                counts[harmonyPair.Key] = harmonyPair.Value[teamID];
            }
        }
        return counts;
    }
    public bool IsHarmonyActiveForTeam(HarmonyType harmony, int teamID)
    {
        if (activeHarmonyTiers.ContainsKey(harmony))
        {
            return activeHarmonyTiers[harmony].ContainsKey(teamID);
        }
        return false;
    }

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
        OnHarmoniesUpdated?.Invoke();
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
        OnHarmoniesUpdated?.Invoke();
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
        if (categoryLimitsDict.TryGetValue(category, out limitForCategory))
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
        if (CurrentState == GameState.Combat)
        {
            StopAllCoroutines();
        }
        List<UnitController> unitsToDestroy = new List<UnitController>(allUnits);
        foreach (UnitController unit in unitsToDestroy)
        {
            if (unit != null) unit.Die(null); 
        }
        allUnits.Clear();
        harmonyCounts.Clear();
        activeHarmonyTiers.Clear();
        teamUnitCount.Clear();
        if (teamCategoryCounts != null) teamCategoryCounts.Clear();
        if (PlacementUIManager.Instance != null)
        {
            PlacementUIManager.Instance.ShowAllyBench();
        }

        UnitIconController[] icons = FindObjectsByType<UnitIconController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UnitIconController icon in icons) icon.ResetIcon();
        CurrentState = GameState.Placement;
        OnHarmoniesUpdated?.Invoke();
        unitsAtCombatStart.Clear();
        Debug.Log("Tablero Reiniciado. Fase de Colocación activada.");
    }

    private void UpdateHarmonyBonuses(UnitController unit, bool isAdding)
    {
        int teamID = unit.teamID;
        int change = isAdding ? 1 : -1;
        foreach (HarmonyType harmony in unit.unitStats.naturalHarmonies)
        {
            if (!harmonyCounts.ContainsKey(harmony))
                harmonyCounts[harmony] = new Dictionary<int, int>();
            if (!harmonyCounts[harmony].ContainsKey(teamID))
                harmonyCounts[harmony][teamID] = 0;
            harmonyCounts[harmony][teamID] += change;
            int currentCount = harmonyCounts[harmony][teamID];
            int highestActiveTierIndex = -1;
            for (int i = 0; i < harmony.tiers.Count; i++)
            {
                if (currentCount >= harmony.tiers[i].unitsRequired)
                {
                    highestActiveTierIndex = i;
                }
            }

            if (!activeHarmonyTiers.ContainsKey(harmony))
                activeHarmonyTiers[harmony] = new Dictionary<int, int>();

            if (highestActiveTierIndex != -1)
            {
                activeHarmonyTiers[harmony][teamID] = highestActiveTierIndex;
            }
            else
            {
                activeHarmonyTiers[harmony].Remove(teamID);
            }
        }
    }
    public void StartCombatButton()
    {
        if (CurrentState == GameState.Placement)
        {
            unitsAtCombatStart.Clear();
            foreach (var unit in allUnits)
            {
                if (unit != null)
                {
                    unitsAtCombatStart.Add(new CombatStartInfo {
                        stats = unit.unitStats,
                        teamID = unit.teamID,
                        startingNode = unit.currentNode,
                        originatingIcon = unit.originatingIcon
                    });
                }
            }
            
            CurrentState = GameState.Combat;
            StartCoroutine(CombatLoop());
        }
    }
    private IEnumerator CombatLoop()
    {
        yield return new WaitForSeconds(1.0f);

        while (CurrentState == GameState.Combat && allUnits.Any(u => u.teamID == 0) && allUnits.Any(u => u.teamID == 1))
        {
            foreach (var unit in allUnits.ToList())
            {
                if (unit != null) unit.EvaluateAction();
            }
            yield return null;
        }
        
        yield return new WaitForSeconds(1.5f);
        ResetBoardAfterCombat();
    }

    private void ResetBoardAfterCombat()
    {
        CurrentState = GameState.Result;
        Debug.Log("Reconstruyendo tablero para la siguiente ronda...");
        foreach (var unit in allUnits.ToList())
        {
            if (unit != null) Destroy(unit.gameObject);
        }
        
        allUnits.Clear();
        harmonyCounts.Clear();
        activeHarmonyTiers.Clear();

        if (gridManager != null && gridManager.grid != null)
        {
            foreach (Node node in gridManager.grid)
            {
                node.isWalkable = true;
            }
        }
        
        foreach (var unitInfo in unitsAtCombatStart)
        {
            if (gridManager != null && unitInfo.startingNode != null)
            {
                gridManager.SpawnUnit(unitInfo.stats, unitInfo.teamID, unitInfo.startingNode, unitInfo.originatingIcon);
            }
        }
        
        if(OnHarmoniesUpdated != null) OnHarmoniesUpdated.Invoke();
        CurrentState = GameState.Placement;
        Debug.Log("Fase de colocación reanudada.");
    }

    #endregion
    public List<UnitController> GetAllUnits()
    {
        return allUnits;
    }
}