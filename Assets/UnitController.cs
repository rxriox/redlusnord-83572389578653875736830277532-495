using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitController : MonoBehaviour
{
    [SerializeField] private UnitStats baseStats;
    public int teamID;
    public int CurrentHealth { get; private set; }

    private GridManager gridManager;
    private UnitController targetEnemy;
    private Coroutine combatCoroutine;
    private Node reservedDestinationNode;
    private float attackCooldown = 0f;

    void Start()
    {
        CurrentHealth = baseStats.maxHealth;
        gridManager = FindAnyObjectByType<GridManager>();
    }

    void Update()
    {
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }
    }

    private void OnEnable() => GameManager.OnCombatStart += StartCombat;
    private void OnDisable() => GameManager.OnCombatStart -= StopCombat;

    void StartCombat()
    {
        if (combatCoroutine != null) StopCoroutine(combatCoroutine);
        combatCoroutine = StartCoroutine(CombatLoop());
    }
    
    void StopCombat()
    {
        if (combatCoroutine != null) StopCoroutine(combatCoroutine);
        if (reservedDestinationNode != null)
        {
            gridManager.UnreserveNode(reservedDestinationNode);
            reservedDestinationNode = null;
        }
    }

    IEnumerator CombatLoop()
    {
        while (CurrentHealth > 0)
        {
            // --- GESTIÓN DE OBJETIVOS ---
            if (targetEnemy == null || targetEnemy.CurrentHealth <= 0)
            {
                CleanUpAfterTarget();
                targetEnemy = FindClosestEnemy();
            }

            if (targetEnemy == null)
            {
                yield return new WaitForSeconds(0.5f); // No hay enemigos, esperar.
                continue;
            }

            // --- LÓGICA DE DECISIÓN: ATACAR O MOVERSE ---
            if (IsInAttackRange(targetEnemy))
            {
                // Si estoy en rango, libero cualquier reserva que tuviera (porque ya llegué) y ataco.
                CleanUpAfterTarget();
                yield return StartCoroutine(AttackTarget());
            }
            else
            {
                // Si estoy fuera de rango, me muevo. La corutina de movimiento se encargará de todo el trayecto.
                yield return StartCoroutine(MoveToTarget());
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Se encarga del proceso COMPLETO de moverse hasta el destino.
    /// </summary>
    IEnumerator MoveToTarget()
    {
        // Si no tengo una casilla reservada, busco y reservo una.
        if (reservedDestinationNode == null)
        {
            reservedDestinationNode = FindAndReserveBestAttackPosition(targetEnemy);
        }

        // Si después de buscar no encontré ninguna (todas ocupadas/reservadas), el turno de movimiento termina.
        if (reservedDestinationNode == null)
        {
            yield break;
        }

        List<Node> path = gridManager.FindPath(transform.position, reservedDestinationNode.worldPosition);

        if (path != null && path.Count > 0)
        {
            // Moverse a lo largo de toda la ruta hasta llegar al final
            foreach (Node node in path)
            {
                Vector3 targetPosition = node.worldPosition + new Vector3(0, 0.5f, 0);
                while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
                {
                    // En esta versión, NOS COMPROMETEMOS a llegar al destino.
                    // No comprobamos el rango aquí para evitar paradas prematuras.
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, 5f * Time.deltaTime);
                    transform.LookAt(new Vector3(targetEnemy.transform.position.x, transform.position.y, targetEnemy.transform.position.z));
                    yield return null; // Esperar al siguiente frame
                }
            }
        }
        
        // Cuando el movimiento termina (o si no había ruta), la corutina finaliza y el CombatLoop re-evaluará.
    }

    IEnumerator AttackTarget()
    {
        if (attackCooldown <= 0)
        {
            transform.LookAt(new Vector3(targetEnemy.transform.position.x, transform.position.y, targetEnemy.transform.position.z));
            targetEnemy.TakeDamage(baseStats.attackDamage);
            attackCooldown = 1f / baseStats.attackSpeed; // Reiniciar cooldown
        }
        yield return null; // Esperar un frame antes de re-evaluar en el bucle principal
    }

    // --- FUNCIONES DE AYUDA ---

    private void CleanUpAfterTarget() {
        if (reservedDestinationNode != null) {
            gridManager.UnreserveNode(reservedDestinationNode);
            reservedDestinationNode = null;
        }
    }

    private Node FindAndReserveBestAttackPosition(UnitController target) {
        Node bestNode = null;
        float minDistanceToSelf = float.MaxValue;
        Node targetNode = gridManager.NodeFromWorldPoint(target.transform.position);

        foreach (var neighbour in gridManager.GetNeighbours(targetNode)) {
            if (neighbour.isWalkable && !neighbour.isReserved) {
                float distance = Vector3.Distance(transform.position, neighbour.worldPosition);
                if (distance < minDistanceToSelf) {
                    minDistanceToSelf = distance;
                    bestNode = neighbour;
                }
            }
        }
        
        if (bestNode != null) {
            gridManager.ReserveNode(bestNode);
        }
        
        return bestNode;
    }
    
    private bool IsInAttackRange(UnitController target) { return target != null && Vector3.Distance(transform.position, target.transform.position) <= baseStats.attackRange; }
    
    private UnitController FindClosestEnemy() {
        UnitController[] allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        UnitController closest = null;
        float minDistance = float.MaxValue;
        foreach (var unit in allUnits) { if (unit != this && unit.teamID != this.teamID && unit.CurrentHealth > 0) { float dist = Vector3.Distance(transform.position, unit.transform.position); if (dist < minDistance) { minDistance = dist; closest = unit; } } }
        return closest;
    }

    public void TakeDamage(int damage) { if (CurrentHealth <= 0) return; CurrentHealth -= damage; if (CurrentHealth <= 0) { CurrentHealth = 0; Die(); } }
    
    void Die() {
        CleanUpAfterTarget(); // Liberar reserva si muero
        Destroy(gameObject);
    }
}