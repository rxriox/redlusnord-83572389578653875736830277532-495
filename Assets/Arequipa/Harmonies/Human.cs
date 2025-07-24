using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class Harmony_Human : MonoBehaviour
{
    private UnitController unitController;

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    public float GetAttackSpeedBonus()
    {
        if (GameManager.Instance == null) return 0f;

        HarmonyType humanHarmony = GameManager.Instance.FindHarmonyByName("Human");
        if (humanHarmony == null) return 0f;

        if (GameManager.Instance.IsHarmonyActiveForTeam(humanHarmony, unitController.teamID))
        {
            var harmonyCounts = GameManager.Instance.GetHarmonyCountsForTeam(unitController.teamID);
            if (harmonyCounts.TryGetValue(humanHarmony, out int unitCount))
            {                
                // Bonificación base al activar la armonía (a partir de 3 unidades).
                float bonus = 2.5f;

                // Bonificación adicional por cada unidad por encima de 3.
                if (unitCount > 3)
                {
                    int extraUnits = unitCount - 3;
                    bonus += extraUnits * 0.4f;
                }
                
                Debug.Log($"{unitController.unitStats.unitName} recibe +{bonus} de velocidad de ataque de la armonía Human.");
                return bonus;
            }
        }

        return 0f; // No hay bonificación
    }
}