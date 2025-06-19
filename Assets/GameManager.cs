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

    // NUEVO: Diccionario para rastrear la cuenta de unidades por tipo de armonía para cada equipo.
    // Key: HarmonyType, Value: Dictionary<TeamID, Count>
    private Dictionary<HarmonyType, Dictionary<int, int>> harmonyCounts = new Dictionary<HarmonyType, Dictionary<int, int>>();

    // NUEVO: Diccionario para rastrear los tiers de armonía activos por equipo.
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
        // ACCESO CORREGIDO: Directamente desde la instancia de UnitController
        UnitStats unitStats = unit.baseStats; // Ahora baseStats es public en UnitController
        
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

                // Asegura que el diccionario para este equipo exista en activeHarmonyTiers
                if (!activeHarmonyTiers.ContainsKey(harmony))
                {
                    activeHarmonyTiers.Add(harmony, new Dictionary<int, int>());
                }
                if (!activeHarmonyTiers[harmony].ContainsKey(teamID))
                {
                    activeHarmonyTiers[harmony].Add(teamID, -1); // -1 significa ningún tier activo
                }

                int oldActiveTierIndex = activeHarmonyTiers[harmony][teamID];
                int newActiveTierIndex = -1;

                // Encuentra el tier más alto que se cumple con las unidades actuales
                for (int i = harmony.bonusTiers.Count - 1; i >= 0; i--)
                {
                    if (currentUnitsOfHarmony >= harmony.bonusTiers[i].unitsRequired)
                    {
                        newActiveTierIndex = i;
                        break;
                    }
                }

                // Si el tier activo ha cambiado
                if (newActiveTierIndex != oldActiveTierIndex)
                {
                    // Desactiva el bono antiguo si había uno
                    if (oldActiveTierIndex != -1)
                    {
                        Debug.Log($"Armonía {harmony.harmonyName} Tier {harmony.bonusTiers[oldActiveTierIndex].unitsRequired} desactivado para Equipo {teamID}.");
                        // Lógica para REMOVER bonificaciones del tier viejo
                        ApplyHarmonyBonus(harmony, oldActiveTierIndex, teamID, false);
                    }

                    // Activa el nuevo bono si hay uno
                    if (newActiveTierIndex != -1)
                    {
                        Debug.Log($"Armonía {harmony.harmonyName} Tier {harmony.bonusTiers[newActiveTierIndex].unitsRequired} ACTIVADO para Equipo {teamID}: {harmony.bonusTiers[newActiveTierIndex].bonusDescription}");
                        // Lógica para APLICAR bonificaciones del tier nuevo
                        ApplyHarmonyBonus(harmony, newActiveTierIndex, teamID, true);
                    }
                    activeHarmonyTiers[harmony][teamID] = newActiveTierIndex; // Actualiza el tier activo
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
        // This is the part where you would apply the REAL effects of the bonus to units.
        // For example, you might iterate over `allUnits` and apply the bonus to units
        // that belong to the `teamID` given and that are of `harmony` type.

        // Example: Increase attack damage for units of this harmony in this team
        // foreach (UnitController unit in allUnits.Where(u => u.teamID == teamID && u.unitStats.naturalHarmonies.Contains(harmony)))
        // {
        //     if (apply)
        //     {
        //         unit.ApplyDamageBonus(harmony.bonusTiers[tierIndex].damageBonus);
        //     }
        //     else
        //     {
        //         unit.RemoveDamageBonus(harmony.bonusTiers[tierIndex].damageBonus);
        //     }
        // }
        
        Debug.Log($"Applying/Removing bonus: {harmony.harmonyName} Tier {harmony.bonusTiers[tierIndex].unitsRequired} for Team {teamID}. Action: {(apply ? "Apply" : "Remove")}");
    }


    /// <summary>
    /// Method called by a button or event to start the combat phase.
    /// </summary>
    public void StartCombatButton()
    {
        if (CurrentState == GameState.Placement)
        {
            CurrentState = GameState.Combat; // Change state to combat.
            StartCoroutine(CombatLoop()); // Start the combat loop coroutine.
        }
    }

    /// <summary>
    /// Main coroutine that manages the combat loop turn by turn.
    /// </summary>
    IEnumerator CombatLoop()
    {
        while (CurrentState == GameState.Combat)
        {
            // Filter active (alive) units on the battlefield.
            List<UnitController> activeUnits = allUnits.Where(u => u != null && u.CurrentHealth > 0).ToList();

            // Check end combat conditions: if one team has lost all its units.
            if (activeUnits.Count(u => u.teamID == 0) == 0 || activeUnits.Count(u => u.teamID == 1) == 0)
            {
                CurrentState = GameState.Result; // Change state to result.
                Debug.Log("Combat ended!"); // End combat message.
                break; // Exit combat loop.
            }

            // Order units to determine action order in this combat "tick".
            // Current priority is the one closest to an enemy for quick action.
            List<UnitController> orderedUnits = activeUnits
                .OrderBy(u => {
                    UnitController closestEnemy = activeUnits
                        .Where(e => e.teamID != u.teamID) // Find enemies from other team.
                        .OrderBy(e => Vector3.Distance(u.transform.position, e.transform.position)) // Order by distance.
                        .FirstOrDefault(); // Select the closest.
                    
                    // If no enemies (unusual, but to avoid errors), return a high value.
                    return closestEnemy == null ? float.MaxValue : Vector3.Distance(u.transform.position, closestEnemy.transform.position);
                })
                .ToList();

            // Each active unit evaluates and performs its action.
            foreach (var unit in orderedUnits)
            {
                if (unit != null) unit.EvaluateAction();
            }
            
            yield return null; // Wait a frame before next combat evaluation.
        }
    }
}
