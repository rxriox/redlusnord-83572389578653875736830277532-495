using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class GameManager : MonoBehaviour

{
    public float battleTimer;
    public const float BATTLE_TIME_LIMIT = 40f;
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
    private GameState _currentState;
    public GameState CurrentState
    {
        get { return _currentState; }
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnGameStateChanged?.Invoke(_currentState);
            }
        }
    }
    public static event System.Action<GameState> OnGameStateChanged;

    [Header("Reglas del Juego")]
    public int maxUnitsPerTeam;
    [Header("Referencias de UI (Contadores)")]
    public TextMeshProUGUI allyUnitCountText;
    public TextMeshProUGUI enemyUnitCountText;
    public TextMeshProUGUI activeArtifactsCountText;

    private Dictionary<UnitStats.UnitCategory, int> categoryLimitsDict = new Dictionary<UnitStats.UnitCategory, int>();
    private Dictionary<UnitStats.UnitCategory, int> categoryLevelsDict = new Dictionary<UnitStats.UnitCategory, int>();

    private List<UnitController> allUnits = new List<UnitController>();
    private Dictionary<int, int> teamUnitCount = new Dictionary<int, int>();
    private Dictionary<int, Dictionary<UnitStats.UnitCategory, int>> teamCategoryCounts;

    private Dictionary<HarmonyType, Dictionary<int, int>> harmonyCounts = new Dictionary<HarmonyType, Dictionary<int, int>>();
    private Dictionary<HarmonyType, Dictionary<int, int>> activeHarmonyTiers = new Dictionary<HarmonyType, Dictionary<int, int>>();
    [HideInInspector] public int maxArtifacts;
    private Dictionary<ArtifactCategory, int> artifactCategoryLimitsDict = new Dictionary<ArtifactCategory, int>();
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
        CurrentState = GameState.Placement;
    }

    public void ApplyRoundSettings(RoundSettings settings)
    {
        if (settings == null)
        {
            Debug.LogError("Se intentó aplicar una configuración de ronda nula.");
            return;
        }

        maxUnitsPerTeam = settings.maxTotalUnits;

        categoryLimitsDict.Clear();
        categoryLimitsDict[UnitStats.UnitCategory.Fabulosa] = settings.fabulosaLimit;
        categoryLimitsDict[UnitStats.UnitCategory.Magnifica] = settings.magnificaLimit;
        categoryLimitsDict[UnitStats.UnitCategory.Suprema] = settings.supremaLimit;

        categoryLevelsDict.Clear();
        categoryLevelsDict[UnitStats.UnitCategory.Fabulosa] = settings.fabulosaLevel;
        categoryLevelsDict[UnitStats.UnitCategory.Magnifica] = settings.magnificaLevel;
        categoryLevelsDict[UnitStats.UnitCategory.Suprema] = settings.supremaLevel;

        maxArtifacts = settings.maxArtifacts;
        artifactCategoryLimitsDict.Clear();
        artifactCategoryLimitsDict[ArtifactCategory.Categoria1] = settings.artifactCat1Limit;
        artifactCategoryLimitsDict[ArtifactCategory.Categoria2] = settings.artifactCat2Limit;
        artifactCategoryLimitsDict[ArtifactCategory.Categoria3] = settings.artifactCat3Limit;
        artifactCategoryLimitsDict[ArtifactCategory.Categoria4] = settings.artifactCat4Limit;

        if (ArtifactManager.Instance != null)
        {
            ArtifactManager.Instance.ValidateActiveArtifacts();
        }

        Debug.Log($"Límites de tablero actualizados a: {settings.roundName}. Total: {maxUnitsPerTeam}, Fabulosa: {settings.fabulosaLimit}, Magnífica: {settings.magnificaLimit}, Suprema: {settings.supremaLimit}");

        UpdateAllCountsUI();
    }

    public int GetCurrentLevelForUnit(UnitStats stats)
    {
        if (stats != null && categoryLevelsDict.TryGetValue(stats.category, out int level))
        {
            return level;
        }
        return 1;
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

        UpdateAllCountsUI();
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

        UpdateAllCountsUI();
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

        if (teamCategoryCounts != null && teamCategoryCounts.ContainsKey(teamID))
        {
            teamCategoryCounts[teamID].TryGetValue(category, out currentCategoryCount);
        }

        if (categoryLimitsDict.TryGetValue(category, out int limitForCategory))
        {
            if (currentCategoryCount >= limitForCategory)
            {
                Debug.LogWarning($"LÍMITE DE CATEGORÍA ALCANZADO: Equipo {teamID} ya tiene el máximo de unidades '{category}' ({currentCategoryCount}/{limitForCategory}).");
                return false;
            }
        }

        return true;
    }

    public void ResetBoardButton()
    {
        if (CurrentState == GameState.Combat)
        {
            StopAllCoroutines();
        }

        foreach (var unit in allUnits.ToList())
        {
            if (unit != null)
            {
                Destroy(unit.gameObject);
            }
        }

        if (gridManager != null && gridManager.grid != null)
        {
            foreach (Node node in gridManager.grid)
            {
                if (node != null)
                {
                    node.isWalkable = true;
                }
            }
        }

        allUnits.Clear();
        teamUnitCount.Clear();
        if (teamCategoryCounts != null) teamCategoryCounts.Clear();
        harmonyCounts.Clear();
        activeHarmonyTiers.Clear();
        unitsAtCombatStart.Clear();

        UnitIconController[] icons = FindObjectsByType<UnitIconController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UnitIconController icon in icons)
        {
            icon.ResetIcon();
        }

        CurrentState = GameState.Placement;
        OnHarmoniesUpdated?.Invoke();

        if (PlacementUIManager.Instance != null) PlacementUIManager.Instance.HideDetailsPanel();

        if (ArtifactManager.Instance != null)
        {
            ArtifactManager.Instance.ResetAllArtifactIconsState();
        }

        Debug.Log("Tablero completamente limpiado por el botón.");

        UpdateAllCountsUI();
    }

    public void CheckForCombatEnd()
    {
        int team0Count = allUnits.Count(u => u.teamID == 0);
        int team1Count = allUnits.Count(u => u.teamID == 1);
        if (CurrentState != GameState.Combat) return;
        if (team0Count == 0 || team1Count == 0)
        {
            EndCombatImmediately("Un equipo ha sido eliminado.");
        }
    }

    public void EndCombatImmediately(string reason)
    {
        if (CurrentState != GameState.Combat) return;
        Debug.Log($"Combate finalizado: {reason}");

        StopAllCoroutines();

        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ResetAllPools();
        }

        ResetBoardAfterCombat();
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
            battleTimer = 0f;
            foreach (var unit in allUnits)
            {
                if (unit != null)
                {
                    unitsAtCombatStart.Add(new CombatStartInfo
                    {
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
        while (CurrentState == GameState.Combat && battleTimer < BATTLE_TIME_LIMIT)
        {
            battleTimer += Time.deltaTime;
            foreach (var unit in allUnits.ToList())
            {
                if (unit != null)
                {
                    unit.EvaluateAction();
                }
            }
            yield return null;
        }

        if (CurrentState == GameState.Combat)
        {
            EndCombatImmediately("Límite de tiempo alcanzado.");
        }
    }
    public void EndCombatEarly()
    {
        if (CurrentState == GameState.Combat)
        {
            Debug.Log("El jugador ha terminado el combate manualmente. Reseteando tablero...");
            StopAllCoroutines();
            ResetBoardAfterCombat();
        }
        else
        {
            Debug.LogWarning("Se intentó terminar el combate, pero no hay ninguno en curso.");
        }
    }

    private void ResetBoardAfterCombat()
    {
        CurrentState = GameState.Result;
        Debug.Log("Reconstruyendo tablero para la siguiente ronda...");
        teamUnitCount.Clear();
        if (teamCategoryCounts != null) teamCategoryCounts.Clear();
        harmonyCounts.Clear();
        activeHarmonyTiers.Clear();
        foreach (var unit in allUnits.ToList())
        {
            if (unit != null) Destroy(unit.gameObject);
        }
        allUnits.Clear();
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

        if (ArtifactManager.Instance != null)
        {
            ArtifactManager.Instance.ResetAllArtifactIconsState();
        }

        OnHarmoniesUpdated?.Invoke();
        if (PlacementUIManager.Instance != null) PlacementUIManager.Instance.HideDetailsPanel();
        CurrentState = GameState.Placement;
        Debug.Log("Fase de colocación reanudada.");
    }

    public UnitController GetUnitAtNode(Node node)
    {
        if (node == null) return null;

        foreach (UnitController unit in allUnits)
        {
            if (unit != null && unit.currentNode == node)
            {
                return unit;
            }
        }

        return null;
    }
    public int GetArtifactLimitForCategory(ArtifactCategory category)
    {
        if (artifactCategoryLimitsDict.TryGetValue(category, out int limit))
        {
            return limit;
        }
        return 0;
    }
    public List<UnitController> GetAllUnits()
    {
        return allUnits;
    }

    public void UpdateAllCountsUI()
    {
        if (allyUnitCountText != null)
        {
            int allyCount = GetUnitCountForTeam(0);
            allyUnitCountText.text = $"{allyCount}/{maxUnitsPerTeam}";
        }

        if (enemyUnitCountText != null)
        {
            int enemyCount = GetUnitCountForTeam(1);
            enemyUnitCountText.text = $"{enemyCount}/{maxUnitsPerTeam}";
        }

        if (activeArtifactsCountText != null && ArtifactManager.Instance != null)
        {
            int artifactCount = ArtifactManager.Instance.GetActiveArtifactCount();
            activeArtifactsCountText.text = $"{artifactCount}/{maxArtifacts}";
        }
    }
}