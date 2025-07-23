using UnityEngine;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(UnitController))]
public class Harmony_Defiant : MonoBehaviour
{
    private UnitController unitController;
    private float teleportCooldown = 6.0f;
    private float lastTeleportTime = -10.0f; // Permite el teletransporte inicial
    private int attackCounter = 0;
    private const int ATTACKS_FOR_CRIT = 3;
    private const float CRIT_MULTIPLIER = 1.8f; // 180%

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
    }

    // El GameManager llamará a esto para el teletransporte inicial
    public IEnumerator ActivateInitialTeleport()
    {
        // Devuelve la corrutina para que el GameManager pueda esperar a que termine
        yield return StartCoroutine(TeleportSequence(0.8f));
    }

    // Comprueba si el teletransporte está listo y si la unidad no está ya en combate
    public bool IsTeleportReady()
    {
        if (Time.time >= lastTeleportTime + teleportCooldown && !unitController.IsTargetInAttackRange())
        {
            return true;
        }
        return false;
    }

    // Inicia el teletransporte de re-posicionamiento
    public void TriggerTeleport()
    {
        StartCoroutine(TeleportSequence(0.8f)); // Sin retardo de desaparición
    }

    private IEnumerator TeleportSequence(float disappearDuration)
    {
        lastTeleportTime = Time.time;
        
        // 1. Desaparecer
        if (disappearDuration > 0)
        {
            unitController.SetVisibility(false);
            yield return new WaitForSeconds(disappearDuration);
        }

        // 2. Encontrar el nuevo destino
        Node targetNode = FindTeleportDestination();
        
        // 3. Reaparecer en la nueva posición
        if (targetNode != null)
        {
            unitController.currentNode.isWalkable = true; // Liberar la casilla vieja
            unitController.transform.position = targetNode.worldPosition;
            unitController.currentNode = targetNode;
            targetNode.isWalkable = false; // Ocupar la nueva
        }
        
        unitController.SetVisibility(true);
    }

    private Node FindTeleportDestination()
    {
        // Encuentra el enemigo más lejano
        UnitController farthestEnemy = GameManager.Instance.GetAllUnits()
            .Where(u => u != null && u.teamID != unitController.teamID)
            .OrderByDescending(u => Vector3.Distance(transform.position, u.transform.position))
            .FirstOrDefault();

        if (farthestEnemy == null) return null;

        // Encuentra las casillas vecinas disponibles alrededor del enemigo
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        var availableNodes = gridManager.GetNeighbours(farthestEnemy.currentNode)
            .Where(n => n.isWalkable)
            .ToList();

        // Devuelve una casilla aleatoria de las disponibles
        if (availableNodes.Count > 0)
        {
            return availableNodes[Random.Range(0, availableNodes.Count)];
        }

        return null; // No se encontró ninguna casilla
    }
    
    // El UnitController llamará a esto antes de cada ataque
    public float GetDamageMultiplier()
    {
        // Incrementa el contador ANTES de la comprobación.
        attackCounter++;

        // Comprueba si este ataque es el cuarto.
        if (attackCounter == ATTACKS_FOR_CRIT + 1) // Es decir, si es el ataque número 4
        {
            Debug.Log($"{unitController.unitStats.unitName} asesta un GOLPE CRÍTICO!");
            attackCounter = 0; // Reinicia el contador para el siguiente ciclo
            return CRIT_MULTIPLIER;
        }
        
        // Si no es el cuarto, devuelve el daño normal.
        return 1.0f;
    }
}