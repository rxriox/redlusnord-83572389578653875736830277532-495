using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Placement, Combat, Result }
    public GameState CurrentState { get; private set; }
    
    // Lista centralizada de todas las unidades en el campo
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

    // Las unidades se registran y des-registran a sí mismas
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
            Debug.Log("¡El Combate ha comenzado!");
            StartCoroutine(CombatLoop());
        }
    }

    /// <summary>
    /// El nuevo bucle de combate centralizado.
    /// </summary>
    IEnumerator CombatLoop()
    {
        while (CurrentState == GameState.Combat)
        {
            // Filtramos las unidades que están vivas para la siguiente ronda de acciones
            List<UnitController> activeUnits = allUnits.Where(u => u.CurrentHealth > 0).ToList();

            if (activeUnits.Count(u => u.teamID == 0) == 0 || activeUnits.Count(u => u.teamID == 1) == 0)
            {
                // Si un equipo ha sido eliminado, termina el combate.
                CurrentState = GameState.Result;
                Debug.Log("¡Combate terminado!");
                break;
            }

            // --- LÓGICA DE PRIORIDAD ---
            // Ordenamos las unidades que pueden actuar por su proximidad al enemigo más cercano.
            // La unidad más cercana a CUALQUIER enemigo actuará primero.
            List<UnitController> orderedUnits = activeUnits
                .OrderBy(u => {
                    // Encontramos el enemigo más cercano a ESTA unidad 'u'
                    UnitController closestEnemy = activeUnits
                        .Where(e => e.teamID != u.teamID)
                        .OrderBy(e => Vector3.Distance(u.transform.position, e.transform.position))
                        .FirstOrDefault();
                    
                    // Si no hay enemigos, la distancia es infinita.
                    return closestEnemy == null ? float.MaxValue : Vector3.Distance(u.transform.position, closestEnemy.transform.position);
                })
                .ToList();

            // Damos turno a cada unidad en el orden de prioridad calculado
            foreach (var unit in orderedUnits)
            {
                unit.TakeAction();
            }

            // Esperamos un pequeño instante antes de la siguiente "ronda" de turnos.
            yield return new WaitForSeconds(0.1f);
        }
    }
}