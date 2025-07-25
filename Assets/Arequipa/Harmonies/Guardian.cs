using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class Harmony_Guardian : MonoBehaviour
{
    private UnitController unitController;

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    // El UnitController llamará a este método para obtener el porcentaje de reducción.
    public float GetMaterialDamageReduction()
    {
        if (GameManager.Instance == null) return 0f;

        HarmonyType guardianHarmony = GameManager.Instance.FindHarmonyByName("Guardian");
        if (guardianHarmony == null) return 0f;

        // Comprueba si la armonía está activa para el equipo de esta unidad.
        if (GameManager.Instance.IsHarmonyActiveForTeam(guardianHarmony, unitController.teamID))
        {
            // Obtiene el número de unidades Guardianas en el tablero.
            var harmonyCounts = GameManager.Instance.GetHarmonyCountsForTeam(unitController.teamID);
            if (harmonyCounts.TryGetValue(guardianHarmony, out int unitCount))
            {
                // --- Lógica de la bonificación ---
                // Bonificación base al activar la armonía (a partir de 3 unidades).
                float reduction = 0.35f; // 35%

                // Bonificación adicional por cada unidad por encima de 3.
                if (unitCount > 3)
                {
                    int extraUnits = unitCount - 3;
                    reduction += extraUnits * 0.07f; // 7% por cada unidad extra
                }

                Debug.Log($"{unitController.unitStats.unitName} recibe {reduction * 100}% de reducción de daño material.");
                return reduction;
            }
        }

        return 0f; // No hay reducción de daño.
    }
}