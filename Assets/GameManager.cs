using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gestiona el estado general del juego (colocación, combate, resultado)
/// y el ciclo de combate. Es un Singleton para fácil acceso.
/// También gestiona el seguimiento y activación de armonías (sinergias).
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

    // Diccionario para rastrear la cuenta de unidades por tipo de armonía para cada equipo.
    // Key: HarmonyType, Value: Dictionary<TeamID, Count>
    private Dictionary<HarmonyType, Dictionary<int, int>> harmonyCounts = new Dictionary<HarmonyType, Dictionary<int, int>>();

    // Diccionario para rastrear los tiers de armonía activos por equipo.
    // Key: HarmonyType, Value: Dictionary<TeamID, ActivatedTierIndex>
    private Dictionary<HarmonyType, Dictionary<int, int>> activeHarmonyTiers = new Dictionary<HarmonyType, Dictionary<int, int>>();


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
    /// Registra una unidad en el GameManager y actualiza las cuentas de armonía.
    /// </summary>
    /// <param name="unit">La unidad a registrar.</param>
    public void RegisterUnit(UnitController unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
            UpdateHarmonyCounts(unit, 1); // Incrementa las armonías al registrar
        }
    }

    /// <summary>
    /// Desregistra una unidad del GameManager (normalmente cuando muere) y actualiza las cuentas de armonía.
    /// </summary>
    /// <param name="unit">La unidad a desregistrar.</param>
    public void UnregisterUnit(UnitController unit)
    {
        if (allUnits.Contains(unit))
        {
            allUnits.Remove(unit);
            UpdateHarmonyCounts(unit, -1); // Decrementa las armonías al desregistrar
        }
    }

    /// <summary>
    /// Actualiza las cuentas de armonía cuando una unidad es registrada o desregistrada.
    /// </summary>
    /// <param name="unit">La unidad que activó la actualización.</param>
    /// <param name="changeAmount">Cantidad a sumar (1 para añadir, -1 para quitar).</param>
    private void UpdateHarmonyCounts(UnitController unit, int changeAmount)
    {
        UnitStats unitStats = unit.baseStats; 
        
        if (unitStats == null)
        {
            Debug.LogError($"Unit {unit.name} is missing baseStats to update harmony counts.");
            return;
        }

        foreach (HarmonyType harmony in unitStats.naturalHarmonies)
        {
            if (!harmonyCounts.ContainsKey(harmony))
            {
                harmonyCounts.Add(harmony, new Dictionary<int, int> { { 0, 0 }, { 1, 0 } }); // Inicializa para ambos equipos
            }

            harmonyCounts[harmony][unit.teamID] += changeAmount;
            harmonyCounts[harmony][unit.teamID] = Mathf.Max(0, harmonyCounts[harmony][unit.teamID]); // Asegura que no baje de 0
        }
        EvaluateActiveHarmonies(); // Re-evalúa las armonías activas después de cada cambio
    }

    /// <summary>
    /// Evalúa qué armonías están activas para cada equipo y aplica/remueve sus bonificaciones.
    /// </summary>
    private void EvaluateActiveHarmonies()
    {
        foreach (var harmonyEntry in harmonyCounts)
        {
            HarmonyType harmony = harmonyEntry.Key;

            foreach (var teamCount in harmonyEntry.Value)
            {
                int teamID = teamCount.Key;
                int currentUnitsOfHarmony = teamCount.Value;

                if (!activeHarmonyTiers.ContainsKey(harmony))
                {
                    activeHarmonyTiers.Add(harmony, new Dictionary<int, int>());
                }
                if (!activeHarmonyTiers[harmony].ContainsKey(teamID))
                {
                    activeHarmonyTiers[harmony].Add(teamID, -1); // -1 means no tier active
                }

                int oldActiveTierIndex = activeHarmonyTiers[harmony][teamID];
                int newActiveTierIndex = -1;

                // Find the highest tier that is met by current units
                for (int i = harmony.bonusTiers.Count - 1; i >= 0; i--)
                {
                    if (currentUnitsOfHarmony >= harmony.bonusTiers[i].unitsRequired)
                    {
                        newActiveTierIndex = i;
                        break;
                    }
                }

                // If the active tier has changed
                if (newActiveTierIndex != oldActiveTierIndex)
                {
                    // Deactivate old bonus if there was one
                    if (oldActiveTierIndex != -1)
                    {
                        Debug.Log($"Armonía {harmony.harmonyName} Tier {harmony.bonusTiers[oldActiveTierIndex].unitsRequired} desactivado para Equipo {teamID}.");
                        ApplyHarmonyBonus(harmony, oldActiveTierIndex, teamID, false);
                    }

                    // Activate new bonus if there is one
                    if (newActiveTierIndex != -1)
                    {
                        Debug.Log($"Armonía {harmony.harmonyName} Tier {harmony.bonusTiers[newActiveTierIndex].unitsRequired} ACTIVADO para Equipo {teamID}: {harmony.bonusTiers[newActiveTierIndex].bonusDescription}");
                        ApplyHarmonyBonus(harmony, newActiveTierIndex, teamID, true);
                    }
                    activeHarmonyTiers[harmony][teamID] = newActiveTierIndex; // Update active tier
                }
            }
        }
    }

    /// <summary>
    /// Aplica o remueve las bonificaciones de una armonía para un equipo específico.
    /// (Actualmente solo imprime un mensaje. Aquí iría la lógica real de aplicar efectos).
    /// </summary>
    /// <param name="harmony">El tipo de armonía.</param>
    /// <param name="tierIndex">El índice del tier de bonificación.</param>
    /// <param name="teamID">El ID del equipo.</param>
    /// <param name="apply">True para aplicar, false para remover.</param>
    private void ApplyHarmonyBonus(HarmonyType harmony, int tierIndex, int teamID, bool apply)
    {
        Debug.Log($"Applying/Removing bonus: {harmony.harmonyName} Tier {harmony.bonusTiers[tierIndex].unitsRequired} for Team {teamID}. Action: {(apply ? "Apply" : "Remove")}");
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
            List<UnitController> activeUnits = allUnits.Where(u => u != null && u.CurrentHealth > 0).ToList();

            if (activeUnits.Count(u => u.teamID == 0) == 0 || activeUnits.Count(u => u.teamID == 1) == 0)
            {
                CurrentState = GameState.Result;
                Debug.Log("Combat ended!");
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

    /// <summary>
    /// Borra todas las unidades del tablero y restablece el juego al estado de colocación.
    /// </summary>
    public void ResetBoardButton()
    {
        // Si el combate está en curso, lo detenemos primero.
        if (CurrentState == GameState.Combat)
        {
            StopCoroutine(CombatLoop()); // Detiene la corutina del combate
        }

        // Destruye todas las unidades en el tablero.
        // Creamos una copia de la lista para evitar problemas al modificarla mientras iteramos.
        List<UnitController> unitsToDestroy = new List<UnitController>(allUnits);
        foreach (UnitController unit in unitsToDestroy)
        {
            if (unit != null)
            {
                // La función Die() de UnitController ya se encarga de desregistrar la unidad
                // del GameManager y de limpiar su nodo.
                unit.Die(); 
            }
        }
        allUnits.Clear(); // Asegúrate de que la lista esté vacía después de destruir.

        // Limpia las cuentas de armonía y los tiers activos.
        harmonyCounts.Clear();
        activeHarmonyTiers.Clear();

        // Restablece el estado del juego a Placement.
        CurrentState = GameState.Placement;
        Debug.Log("Board Reset! Ready for Unit Placement.");
    }
}
