using UnityEngine;
using System.Collections;

[RequireComponent(typeof(UnitController))]
public class Harmony_Fatebender : MonoBehaviour
{
    private UnitController unitController;
    
    // ===== MODIFICACIÓN AQUÍ: Define el retraso base como una constante =====
    private const float BASE_DAMAGE_DELAY = 1.5f; // <-- Cambia este valor (antes 2.5f)
    // ===== FIN DE LA MODIFICACIÓN =====

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    // El UnitController llamará a este método para iniciar el retraso del daño.
    public void DelayDamage(float damageAmount, UnitController attacker, UnitController.DamageType damageType)
    {
        float delay = GetDamageDelay();
        StartCoroutine(ApplyDelayedDamageCoroutine(damageAmount, attacker, damageType, delay));
    }

    private IEnumerator ApplyDelayedDamageCoroutine(float damageAmount, UnitController attacker, UnitController.DamageType damageType, float delay)
    {
        Debug.Log($"{unitController.unitStats.unitName} retrasa {damageAmount:F1} de daño durante {delay}s.");
        yield return new WaitForSeconds(delay);

        // Si el estado del juego ya no es de combate, aborta la aplicación del daño.
        if (GameManager.Instance.CurrentState == GameManager.GameState.Result || 
            GameManager.Instance.CurrentState == GameManager.GameState.Placement ||
            GameManager.Instance.CurrentState == GameManager.GameState.Overtime)
        {
            Debug.Log($"El daño retrasado a {unitController.unitStats.unitName} ha sido cancelado porque la batalla cambió de fase.");
            yield break; // Termina la corrutina aquí.
        }

        // Llama a TakeDamage de nuevo, pero con la señal de 'bypass' para evitar un bucle infinito.
        unitController.TakeDamage(damageAmount, attacker, damageType, true);
    }

    private float GetDamageDelay()
    {
        if (GameManager.Instance == null) return BASE_DAMAGE_DELAY;

        HarmonyType fatebenderHarmony = GameManager.Instance.FindHarmonyByName("Fatebender");
        if (fatebenderHarmony == null) return BASE_DAMAGE_DELAY;

        var harmonyCounts = GameManager.Instance.GetHarmonyCountsForTeam(unitController.teamID);
        if (harmonyCounts.TryGetValue(fatebenderHarmony, out int unitCount))
        {
            // El retraso base ahora se toma de la constante.
            float delay = BASE_DAMAGE_DELAY;

            // Por cada unidad por encima del mínimo requerido, añade 1 segundo.
            int requiredUnits = 2; // ¡IMPORTANTE: Ajusta este número al de tu ScriptableObject!
            if (unitCount > requiredUnits)
            {
                int extraUnits = unitCount - requiredUnits;
                delay += extraUnits * 0.5f;
            }
            return delay;
        }
        
        return BASE_DAMAGE_DELAY; // Devuelve el base si algo falla.
    }
}