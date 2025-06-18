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

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        CurrentState = GameState.Placement;
    }

    public void RegisterUnit(UnitController unit)
    {
        if (!allUnits.Contains(unit)) allUnits.Add(unit);
    }

    public void UnregisterUnit(UnitController unit)
    {
        if (allUnits.Contains(unit)) allUnits.Remove(unit);
    }

    public void StartCombatButton()
    {
        if (CurrentState == GameState.Placement)
        {
            CurrentState = GameState.Combat;
            StartCoroutine(CombatLoop());
        }
    }

    IEnumerator CombatLoop()
    {
        while (CurrentState == GameState.Combat)
        {
            List<UnitController> activeUnits = allUnits.Where(u => u != null && u.CurrentHealth > 0).ToList();

            if (activeUnits.Count(u => u.teamID == 0) == 0 || activeUnits.Count(u => u.teamID == 1) == 0)
            {
                CurrentState = GameState.Result;
                Debug.Log("¡Combate terminado!");
                break;
            }

            List<UnitController> orderedUnits = activeUnits
                .OrderBy(u => {
                    UnitController closestEnemy = activeUnits
                        .Where(e => e.teamID != u.teamID)
                        .OrderBy(e => Vector3.Distance(u.transform.position, e.transform.position))
                        .FirstOrDefault();
                    
                    return closestEnemy == null ? float.MaxValue : Vector3.Distance(u.transform.position, closestEnemy.transform.position);
                })
                .ToList();

            foreach (var unit in orderedUnits)
            {
                if (unit != null) unit.EvaluateAction();
            }
            
            yield return null; 
        }
    }
}