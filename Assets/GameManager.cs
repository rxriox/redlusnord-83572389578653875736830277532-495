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

    private List<UnitController> allUnits = new List<UnitController>();
    private Dictionary<int, int> teamUnitCount = new Dictionary<int, int>();
    private Dictionary<HarmonyType, Dictionary<int, int>> harmonyCounts = new Dictionary<HarmonyType, Dictionary<int, int>>();
    private Dictionary<HarmonyType, Dictionary<int, int>> activeHarmonyTiers = new Dictionary<HarmonyType, Dictionary<int, int>>();

    private Coroutine combatCoroutine; // Referencia a la corrutina de combate

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterUnit(UnitController unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);

            int team = unit.teamID;
            if (!teamUnitCount.ContainsKey(team))
            {
                teamUnitCount[team] = 0;
            }
            teamUnitCount[team]++;
            Debug.Log($"Unidad registrada para equipo {team}. Total: {teamUnitCount[team]} / {maxUnitsPerTeam}");

            UpdateHarmonyBonuses(unit, true);
        }
    }

    public void UnregisterUnit(UnitController unit)
    {
        if (allUnits.Contains(unit))
        {
            int team = unit.teamID;
            if (teamUnitCount.ContainsKey(team))
            {
                teamUnitCount[team]--;
                Debug.Log($"Unidad des-registrada del equipo {team}. Total: {teamUnitCount[team]} / {maxUnitsPerTeam}");
            }

            allUnits.Remove(unit);
            UpdateHarmonyBonuses(unit, false);
        }
    }

    public bool CanPlaceUnit(int teamID)
    {
        int currentCount = 0;
        teamUnitCount.TryGetValue(teamID, out currentCount);
        bool canPlace = currentCount < maxUnitsPerTeam;

        if (!canPlace)
        {
            Debug.Log($"Intento de colocar unidad para equipo {teamID} denegado. Límite alcanzado.");
        }

        return canPlace;
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

        UnitIconController[] icons = FindObjectsByType<UnitIconController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UnitIconController icon in icons)
        {
            icon.ResetIcon();
        }

        CurrentState = GameState.Placement;
        Debug.Log("Tablero reiniciado. Cuentas de unidades a cero.");
    }

    // Placeholders o ganchos si deseas expandir luego
    private void UpdateHarmonyBonuses(UnitController unit, bool isAdding) { }
    public void UpdateAllUnitBonuses() { }
    public bool IsHarmonyTierActive(UnitController unit, int tierIndex) { return false; }
}
