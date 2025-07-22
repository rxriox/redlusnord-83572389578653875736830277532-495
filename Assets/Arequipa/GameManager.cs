using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class GameManager : MonoBehaviour

{
    public float battleTimer;
    public const float BATTLE_TIME_LIMIT = 15f;
    [Header("Referencias del Sistema")]
    public GridManager gridManager;
    private struct CombatStartInfo
    {
        public UnitStats stats;
        public int teamID;
        public Node startingNode;
        public UnitIconController originatingIcon;
        public Artifact EquippedArtifact;
    }
    private List<CombatStartInfo> unitsAtCombatStart = new List<CombatStartInfo>();
    public static event System.Action OnHarmoniesUpdated;
    public static GameManager Instance { get; private set; }
    public enum GameState { Placement, Combat, Result, Overtime }
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
    public static event System.Action OnUnitCountChanged;

    [Header("Reglas del Juego")]
    public int maxUnitsPerTeam;
    [Header("Referencias de UI (Mensajes)")]
    public GameObject placementErrorPanel;
    public CanvasGroup placementErrorCanvasGroup;
    public TextMeshProUGUI placementErrorText;

    [Header("Referencias de UI (Victoria)")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryText;


    private Coroutine hideErrorCoroutine;
    private Coroutine fadeErrorCoroutine;

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
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    public void ApplyRoundSettings(RoundSettings settings)
    {
        if (CurrentState == GameState.Combat || CurrentState == GameState.Overtime)
        {
            Debug.Log($"Cambio de ronda durante {CurrentState}. Restaurando tablero al estado pre-combate...");
            ResetBoardAfterCombat();
        }

        if (settings == null)
        {
            Debug.LogError("Se intentó aplicar una configuración de ronda nula.");
            return;
        }

        Debug.Log($"Aplicando nueva configuración de ronda: {settings.roundName}.");

        List<UnitController> unitsToProcess = new List<UnitController>(allUnits);

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

        foreach (var unit in unitsToProcess)
        {
            if (unit == null) continue;

            int categoryLimit = GetCategoryLimit(unit.unitStats.category);

            if (categoryLimit <= 0)
            {
                Debug.Log($"Eliminando unidad {unit.unitStats.unitName} porque su categoría ahora tiene un límite de 0.");

                if (unit.EquippedArtifact != null && ArtifactManager.Instance != null)
                {
                    ArtifactManager.Instance.FindAndClearEquippedIcon(unit.EquippedArtifact);
                }

                if (unit.originatingIcon != null)
                {
                    unit.originatingIcon.ResetIcon();
                }

                UnregisterUnit(unit);
                Destroy(unit.gameObject);
            }
            else
            {
                int newLevel = GetCurrentLevelForUnit(unit.unitStats);
                if (unit.CurrentLevel != newLevel)
                {
                    Debug.Log($"Actualizando nivel de {unit.unitStats.unitName} a {newLevel}.");
                    unit.Initialize(newLevel);
                }
            }
        }

        if (ArtifactManager.Instance != null)
        {
            ArtifactManager.Instance.ValidateActiveArtifacts();
        }

        UpdateAllCountsUI();
        OnHarmoniesUpdated?.Invoke();
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
        OnUnitCountChanged?.Invoke();
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
        OnUnitCountChanged?.Invoke();
    }

    public bool CanPlaceUnit(int teamID, UnitStats stats)
    {
        int currentTotalCount = GetUnitCountForTeam(teamID);
        if (currentTotalCount >= maxUnitsPerTeam)
        {
            string message = $"LÍMITE TOTAL ALCANZADO ({currentTotalCount}/{maxUnitsPerTeam})";
            ShowPlacementError(message);
            Debug.Log(message);
            return false;
        }

        UnitStats.UnitCategory category = stats.category;
        int currentCategoryCount = 0;
        if (teamCategoryCounts != null && teamCategoryCounts.ContainsKey(teamID))
        {
            teamCategoryCounts[teamID].TryGetValue(category, out currentCategoryCount);
        }

        if (categoryLimitsDict.TryGetValue(category, out int limitForCategory) && currentCategoryCount >= limitForCategory)
        {
            string message = $"LÍMITE DE UNIDADES '{category.ToString().ToUpper()}' ALCANZADO ({currentCategoryCount}/{limitForCategory})";
            ShowPlacementError(message);
            Debug.LogWarning(message);
            return false;
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

        ArtifactIconController[] artifactIcons = FindObjectsByType<ArtifactIconController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var icon in artifactIcons)
        {
            icon.ResetForAllTeams();
        }

        CurrentState = GameState.Placement;
        OnHarmoniesUpdated?.Invoke();

        if (PanelUnitDetails.Instance != null) PanelUnitDetails.Instance.HideDetailsPanel();

        if (ArtifactManager.Instance != null)
        {
            ArtifactManager.Instance.ResetAllArtifactIconsState();
        }

        OnUnitCountChanged?.Invoke();

        Debug.Log("Tablero completamente limpiado por el botón.");

        UpdateAllCountsUI();
    }

    public void CheckForCombatEnd()
    {
        if (CurrentState != GameState.Combat && CurrentState != GameState.Overtime) return;

        int team0Count = allUnits.Count(u => u.teamID == 0);
        int team1Count = allUnits.Count(u => u.teamID == 1);

        if (team0Count == 0 || team1Count == 0)
        {
            StopAllCoroutines();
            int winnerTeamID = (team0Count > 0) ? 0 : 1;
            StartCoroutine(ShowVictoryScreenAndReset(winnerTeamID));
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
                        originatingIcon = unit.originatingIcon,
                        EquippedArtifact = unit.EquippedArtifact
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
            Debug.Log("Limpiando proyectiles del tablero antes del tiempo extra.");
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ResetAllPools();
            }

            int team0Count = allUnits.Count(u => u.teamID == 0);
            int team1Count = allUnits.Count(u => u.teamID == 1);

            if (team0Count > 0 && team1Count > 0)
            {
                Debug.Log("El tiempo de batalla ha terminado. ¡Comienza el tiempo extra!");
                CurrentState = GameState.Overtime;
                StartCoroutine(OvertimeLoop());
            }
            else
            {
                EndCombatImmediately("Límite de tiempo alcanzado y un equipo fue victorioso.");
            }
        }
    }

    private IEnumerator OvertimeLoop()
    {
        Debug.Log("Iniciando tiempo extra. Todas las unidades recibirán daño porcentual.");
        const float DAMAGE_PERCENT_PER_SECOND = 0.075f; // 7.5% de la vida máxima por segundo

        while (CurrentState == GameState.Overtime)
        {
            foreach (var unit in allUnits.ToList())
            {
                if (unit != null)
                {
                    float damageThisFrame = unit.MaxHealth * DAMAGE_PERCENT_PER_SECOND * Time.deltaTime;
                    unit.TakeDamage(damageThisFrame, null, UnitController.DamageType.Absoluto);
                }
            }
            yield return null;
        }

        if (CurrentState == GameState.Overtime)
        {
            EndCombatImmediately("El tiempo extra ha finalizado.");
        }
    }

    private IEnumerator ShowVictoryScreenAndReset(int winnerTeamID)
    {
        CurrentState = GameState.Result;
        if (victoryPanel != null && victoryText != null)
        {
            string winnerTag = UnitController.GetTeamTag(winnerTeamID);
            victoryText.text = $"¡{winnerTag.ToUpper()} GANA!";
            victoryPanel.SetActive(true);
        }

        //Duracion de pausa de victoria
        yield return new WaitForSeconds(3f);

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        ResetBoardAfterCombat();
    }

    public void EndCombatEarly()
    {
        if (CurrentState == GameState.Combat || CurrentState == GameState.Overtime)
        {
            Debug.Log("El jugador ha terminado el combate manualmente. Restaurando tablero...");

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

        // Para q ArtifactManager desconecte los iconos de las unidades que seran borradas
        if (ArtifactManager.Instance != null)
        {
            ArtifactManager.Instance.DetachAllIconsFromUnits();
        }

        foreach (var unitInfo in unitsAtCombatStart)
        {
            if (gridManager != null && unitInfo.startingNode != null)
            {
                // si la unidad tenia un artefacto lo vuelve a conectar.
                UnitController newUnit = gridManager.SpawnUnit(unitInfo.stats, unitInfo.teamID, unitInfo.startingNode, unitInfo.originatingIcon);
                if (newUnit != null && unitInfo.EquippedArtifact != null && ArtifactManager.Instance != null)
                {
                    ArtifactManager.Instance.ReEquipArtifactToUnit(newUnit, unitInfo.EquippedArtifact);
                }
            }
        }

        OnHarmoniesUpdated?.Invoke();
        if (PanelUnitDetails.Instance != null) PanelUnitDetails.Instance.HideDetailsPanel();
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
        OnUnitCountChanged?.Invoke();
    }

    public void ShowPlacementError(string message)
    {
        if (hideErrorCoroutine != null)
        {
            StopCoroutine(hideErrorCoroutine);
        }
        if (fadeErrorCoroutine != null)
        {
            StopCoroutine(fadeErrorCoroutine);
        }

        if (placementErrorPanel != null && placementErrorText != null && placementErrorCanvasGroup != null)
        {
            placementErrorText.text = message;
            fadeErrorCoroutine = StartCoroutine(FadeCanvasGroup(placementErrorCanvasGroup, 1f, 0.2f));
        }

        hideErrorCoroutine = StartCoroutine(HideErrorPanelAfterDelay(1f));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
    {
        if (targetAlpha > 0)
        {
            cg.alpha = 0;
            cg.gameObject.SetActive(true);
        }

        float startAlpha = cg.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        cg.alpha = targetAlpha;

        if (targetAlpha == 0)
        {
            cg.gameObject.SetActive(false);
        }
    }

    private IEnumerator HideErrorPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (fadeErrorCoroutine != null)
        {
            StopCoroutine(fadeErrorCoroutine);
        }

        if (placementErrorPanel != null && placementErrorCanvasGroup != null)
        {
            fadeErrorCoroutine = StartCoroutine(FadeCanvasGroup(placementErrorCanvasGroup, 0f, 0.3f));
        }

        hideErrorCoroutine = null;
    }

    public int GetCategoryCountForTeam(UnitStats.UnitCategory category, int teamID)
    {
        if (teamCategoryCounts != null && teamCategoryCounts.ContainsKey(teamID) && teamCategoryCounts[teamID].ContainsKey(category))
        {
            return teamCategoryCounts[teamID][category];
        }
        return 0;
    }

    public int GetCategoryLimit(UnitStats.UnitCategory category)
    {
        if (categoryLimitsDict.TryGetValue(category, out int limit))
        {
            return limit;
        }
        return 0;
    }
    
    public void ResetAllActiveArtifactsButton()
    {
        if (CurrentState == GameState.Combat || CurrentState == GameState.Overtime)
        {
            Debug.LogWarning("No se pueden limpiar los artefactos durante el combate o el tiempo extra.");
            return;
        }
        
        if (ArtifactManager.Instance != null)
        {
            ArtifactManager.Instance.ResetAllActiveArtifacts();
        }
        else
        {
            Debug.LogWarning("No se encontró una instancia de ArtifactManager para limpiar los artefactos.");
        }
    }
}