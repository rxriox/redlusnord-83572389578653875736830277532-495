using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Placement, Combat, Result }
    public GameState CurrentState { get; private set; }
    
    private List<UnitController> allUnits = new List<UnitController>();
    private Dictionary<HarmonyType, Dictionary<int, int>> harmonyCounts = new Dictionary<HarmonyType, Dictionary<int, int>>();
    private Dictionary<HarmonyType, Dictionary<int, int>> activeHarmonyTiers = new Dictionary<HarmonyType, Dictionary<int, int>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterUnit(UnitController unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
            // UpdateHarmonyBonuses(unit, true); // Asumo que esta lógica existe
        }
    }

    public void UnregisterUnit(UnitController unit)
    {
        if (allUnits.Contains(unit))
        {
            allUnits.Remove(unit);
            // UpdateHarmonyBonuses(unit, false); // Asumo que esta lógica existe
        }
    }

    public void StartCombatButton()
    {
        if (CurrentState == GameState.Placement)
        {
            CurrentState = GameState.Combat;
            StartCoroutine(CombatLoop());
        }
    }

    private IEnumerator CombatLoop()
    {
        while (CurrentState == GameState.Combat)
        {
            foreach (var unit in allUnits.ToList())
            {
                if (unit != null) unit.EvaluateAction();
            }
            yield return null; 
        }
    }

    public void ResetBoardButton()
    {
        if (CurrentState == GameState.Combat)
        {
            if (CombatLoop() != null)
            {
                StopCoroutine(CombatLoop());
            }
        }

        List<UnitController> unitsToDestroy = new List<UnitController>(allUnits);
        foreach (UnitController unit in unitsToDestroy)
        {
            if (unit != null)
            {
                unit.Die(); 
            }
        }
        allUnits.Clear(); 
        harmonyCounts.Clear();
        activeHarmonyTiers.Clear();

        // --- LÍNEA CORREGIDA ---
        UnitIconController[] icons = FindObjectsByType<UnitIconController>(FindObjectsSortMode.None);
        foreach (UnitIconController icon in icons)
        {
            icon.ResetIcon();
        }

        CurrentState = GameState.Placement;
        Debug.Log("Board Reset! Ready for Unit Placement.");
    }

    // Mantengo estas funciones por si existen en tu código original
    private void UpdateHarmonyBonuses(UnitController unit, bool isAdding) {}
    public void UpdateAllUnitBonuses() {}
    public bool IsHarmonyTierActive(UnitController unit, int tierIndex) { return false; }
    public void EvaluateAction() {} // Placeholder por si es llamado desde el loop
}