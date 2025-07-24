using UnityEngine;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(UnitController))]
public class Harmony_Defiant : MonoBehaviour
{
    private UnitController unitController;
    private int attackCounter = 0;
    private const int ATTACKS_FOR_CRIT = 3;
    private const float CRIT_MULTIPLIER = 1.45f; // 145%

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    // El UnitController llamará a este método en cada ciclo de evaluación.
    public void EvaluateBlink()
    {
        // No hacer nada si la unidad ya está desaparecida o no puede actuar.
        if (unitController.HasStatus(StatusEffect.Vanish) || unitController.HasStatus(StatusEffect.Dazed) || unitController.HasStatus(StatusEffect.Immobilized))
        {
            return;
        }

        // Comprueba si hay enemigos dentro del rango de 3 casillas.
        bool enemyNearby = GameManager.Instance.GetAllUnits()
            .Any(u => u != null && u.teamID != unitController.teamID && unitController.IsUnitWithinDistance(u, 3));

        // Si no hay enemigos cerca, inicia el blink.
        if (!enemyNearby)
        {
            StartCoroutine(BlinkSequence());
        }
    }

    private IEnumerator BlinkSequence()
    {
        // 1. Aplicar estado "Vanish" y desaparecer.
        unitController.ApplyStatus(StatusEffect.Vanish, 1.0f); // Duración un poco mayor por seguridad

        // 2. Esperar 0.8 segundos.
        yield return new WaitForSeconds(0.8f);
        
        // Si el estado fue removido por el Overtime, aborta el resto de la secuencia.
        if (!unitController.HasStatus(StatusEffect.Vanish))
        {
            yield break;
        }

        // 3. Encontrar el destino.
        Node destinationNode = FindBlinkDestination();
        
        // 4. Moverse y reaparecer.
        if (destinationNode != null)
        {
            unitController.currentNode.isWalkable = true; // Liberar la casilla vieja
            unitController.transform.position = destinationNode.worldPosition;
            unitController.currentNode = destinationNode;
            destinationNode.isWalkable = false; // Ocupar la nueva
        }
        
        // 5. Quitar el estado "Vanish".
        unitController.RemoveStatus(StatusEffect.Vanish);
    }

    private Node FindBlinkDestination()
    {
        GridManager gridManager = FindFirstObjectByType<GridManager>();

        // Encuentra el enemigo más lejano.
        UnitController farthestEnemy = GameManager.Instance.GetAllUnits()
            .Where(u => u != null && u.teamID != unitController.teamID)
            .OrderByDescending(u => Vector3.Distance(transform.position, u.transform.position))
            .FirstOrDefault();

        if (farthestEnemy == null) return null;

        // Busca casillas adyacentes y vacías.
        var availableNodes = gridManager.GetNeighbours(farthestEnemy.currentNode)
            .Where(n => n.isWalkable)
            .ToList();

        // Si hay casillas adyacentes, elige una.
        if (availableNodes.Count > 0)
        {
            return availableNodes[Random.Range(0, availableNodes.Count)];
        }
        else // Si no, busca la casilla vacía más cercana al enemigo.
        {
            return gridManager.FindNearestWalkableNode(farthestEnemy.currentNode);
        }
    }

    // El UnitController llamará a esto para saber si el ataque es crítico.
    public float GetDamageMultiplier()
    {
        attackCounter++;
        if (attackCounter <= ATTACKS_FOR_CRIT)
        {
            return 1.0f; // No es un golpe crítico todavía, daño normal.
        }

        // Es el cuarto golpe, resetea y calcula el daño crítico.
        attackCounter = 0;

        if (GameManager.Instance == null) return 1.0f;

        // 1. Encuentra el ScriptableObject de la armonía Defiant.
        HarmonyType defiantHarmony = GameManager.Instance.FindHarmonyByName("Defiant");
        if (defiantHarmony == null) return 1.0f;

        // 2. Obtén el número de unidades Defiant que tiene el equipo.
        var harmonyCounts = GameManager.Instance.GetHarmonyCountsForTeam(unitController.teamID);
        if (harmonyCounts.TryGetValue(defiantHarmony, out int unitCount))
        {
            // 3. Calcula el multiplicador de daño basado en el número de unidades.
            if (unitCount >= 3)
            {
                // Calcula cuántas unidades adicionales hay por encima del mínimo.
                int extraUnits = unitCount - 3;
                
                // El daño base es 145%, y se añade un 16% por cada unidad extra.
                float critMultiplier = 1.45f + (extraUnits * 0.16f);

                Debug.Log($"{unitController.unitStats.unitName} asesta un GOLPE CRÍTICO con {unitCount} unidades Defiant! Multiplicador: {critMultiplier * 100}%");
                return critMultiplier;
            }
        }

        // Si algo falla, devuelve el daño normal como respaldo.
        return 1.0f;
    }
}