using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gestiona el estado general del juego (colocación, combate, resultado)
/// y el ciclo de combate. Es un Singleton para fácil acceso.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Patrón Singleton para acceder fácilmente al GameManager desde otros scripts.
    public static GameManager Instance { get; private set; }

    // Enumeración para los diferentes estados del juego.
    public enum GameState { Placement, Combat, Result }
    // Propiedad para obtener el estado actual del juego.
    public GameState CurrentState { get; private set; }
    
    // Lista de todas las unidades activas en la escena.
    private List<UnitController> allUnits = new List<UnitController>();

    private void Awake()
    {
        // Implementación del Singleton: asegura que solo haya una instancia del GameManager.
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        // El juego comienza en la fase de colocación de unidades.
        CurrentState = GameState.Placement;
    }

    /// <summary>
    /// Registra una unidad en el GameManager para que pueda ser gestionada en el combate.
    /// </summary>
    /// <param name="unit">La unidad a registrar.</param>
    public void RegisterUnit(UnitController unit)
    {
        if (!allUnits.Contains(unit)) allUnits.Add(unit);
    }

    /// <summary>
    /// Desregistra una unidad del GameManager (normalmente cuando muere).
    /// </summary>
    /// <param name="unit">La unidad a desregistrar.</param>
    public void UnregisterUnit(UnitController unit)
    {
        if (allUnits.Contains(unit)) allUnits.Remove(unit);
    }

    /// <summary>
    /// Método llamado por un botón o evento para iniciar la fase de combate.
    /// </summary>
    public void StartCombatButton()
    {
        if (CurrentState == GameState.Placement)
        {
            CurrentState = GameState.Combat; // Cambia el estado a combate.
            StartCoroutine(CombatLoop()); // Inicia la corutina del ciclo de combate.
        }
    }

    /// <summary>
    /// Corutina principal que gestiona el ciclo de combate turno a turno.
    /// </summary>
    IEnumerator CombatLoop()
    {
        while (CurrentState == GameState.Combat)
        {
            // Filtra las unidades activas (vivas) en el campo de batalla.
            List<UnitController> activeUnits = allUnits.Where(u => u != null && u.CurrentHealth > 0).ToList();

            // Comprueba las condiciones de fin de combate: si un equipo ha perdido todas sus unidades.
            if (activeUnits.Count(u => u.teamID == 0) == 0 || activeUnits.Count(u => u.teamID == 1) == 0)
            {
                CurrentState = GameState.Result; // Cambia el estado a resultado.
                Debug.Log("¡Combate terminado!"); // Mensaje de fin de combate.
                break; // Sale del ciclo de combate.
            }

            // Ordena las unidades para determinar el orden de acción en este "tick" de combate.
            // La prioridad actual es el que esté más cerca de un enemigo para asegurar acción rápida.
            List<UnitController> orderedUnits = activeUnits
                .OrderBy(u => {
                    UnitController closestEnemy = activeUnits
                        .Where(e => e.teamID != u.teamID) // Encuentra enemigos de otro equipo.
                        .OrderBy(e => Vector3.Distance(u.transform.position, e.transform.position)) // Ordena por distancia.
                        .FirstOrDefault(); // Selecciona el más cercano.
                    
                    // Si no hay enemigos (situación extraña, pero para evitar errores), devuelve un valor alto.
                    return closestEnemy == null ? float.MaxValue : Vector3.Distance(u.transform.position, closestEnemy.transform.position);
                })
                .ToList();

            // Cada unidad activa evalúa y realiza su acción.
            foreach (var unit in orderedUnits)
            {
                if (unit != null) unit.EvaluateAction();
            }
            
            yield return null; // Espera un frame antes de la siguiente evaluación del combate.
        }
    }
}
