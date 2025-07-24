using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class Harmony_Human : MonoBehaviour
{
    private UnitController unitController;

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    // El UnitController llamará a este método para obtener la bonificación.
    public float GetAttackSpeedBonus()
    {
        if (GameManager.Instance == null) return 0f;

        HarmonyType humanHarmony = GameManager.Instance.FindHarmonyByName("Human");
        if (humanHarmony == null) return 0f;

        // Comprueba si la armonía está activa para el equipo de esta unidad.
        if (GameManager.Instance.IsHarmonyActiveForTeam(humanHarmony, unitController.teamID))
        {
            // Obtiene el número de unidades Humanas en el tablero.
            var harmonyCounts = GameManager.Instance.GetHarmonyCountsForTeam(unitController.teamID);
            if (harmonyCounts.TryGetValue(humanHarmony, out int unitCount))
            {
                // --- Lógica de la bonificación ---
                // Bonificación base al activar la armonía (4 unidades).
                float bonus = 0.25f; // Ejemplo: +25% de velocidad de ataque

                // Bonificación adicional por cada unidad por encima de 4.
                if (unitCount > 4)
                {
                    int extraUnits = unitCount - 4;
                    bonus += extraUnits * 0.10f; // Ejemplo: +10% por cada unidad extra
                }
                
                Debug.Log($"{unitController.unitStats.unitName} recibe +{bonus * 100}% de velocidad de ataque de la armonía Human.");
                return bonus;
            }
        }

        return 0f; // No hay bonificación
    }
}