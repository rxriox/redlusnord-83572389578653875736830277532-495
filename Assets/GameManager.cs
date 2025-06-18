using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Placement, Combat, Result }
    public GameState CurrentState { get; private set; }
    
    // El GameManager vuelve a necesitar la lista de unidades para dirigirlas
    private List<UnitController> allUnits = new List<UnitController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        CurrentState = GameState.Placement;
    }

    // Las unidades se registran y des-registran
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

    IEnumerator CombatLoop()
    {
        while (CurrentState == GameState.Combat)
        {
            // Obtenemos solo las unidades que siguen vivas
            List<UnitController> activeUnits = allUnits.Where(u => u != null && u.CurrentHealth > 0).ToList();

            // Comprobamos si el combate debe terminar
            if (activeUnits.Count(u => u.teamID == 0) == 0 || activeUnits.Count(u => u.teamID == 1) == 0)
            {
                CurrentState = GameState.Result;
                Debug.Log("¡Combate terminado!");
                break;
            }

            // --- LÓGICA DE PRIORIDAD CLAVE ---
            // En cada ciclo, ordenamos las unidades que pueden actuar.
            // La prioridad la tiene la unidad que esté más cerca de CUALQUIER enemigo.
            List<UnitController> orderedUnits = activeUnits
                .OrderBy(u => {
                    UnitController closestEnemy = activeUnits
                        .Where(e => e.teamID != u.teamID)
                        .OrderBy(e => Vector3.Distance(u.transform.position, e.transform.position))
                        .FirstOrDefault();
                    
                    return closestEnemy == null ? float.MaxValue : Vector3.Distance(u.transform.position, closestEnemy.transform.position);
                })
                .ToList();

            // Le damos "turno" a cada unidad en el orden de prioridad.
            // La unidad más cercana tomará su decisión y reservará su casilla ANTES que una lejana.
            foreach (var unit in orderedUnits)
            {
                if (unit != null) unit.EvaluateAction();
            }

            // Esperamos un pequeño instante antes del siguiente ciclo de decisiones.
            // Este valor es clave para el "feeling" del juego.
            yield return new WaitForSeconds(0.15f);
        }
    }
}