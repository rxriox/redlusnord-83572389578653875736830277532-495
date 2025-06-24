using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UnitController : MonoBehaviour
{
    // --- DATOS Y REFERENCIAS ---
    public UnitStats unitStats;
    public int teamID;
    public Node currentNode;
    public UnitIconController originatingIcon;
    public float CurrentHealth { get; private set; }

    // --- ESTADO DE LA IA ---
    private enum State { IDLE, MOVING, ATTACKING }
    private State currentState = State.IDLE;

    private UnitController currentTarget;
    private float attackCooldown = 0f;
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        CurrentHealth = unitStats.maxHealth;
    }

    // --- EL CEREBRO DE LA UNIDAD ---
    public void EvaluateAction()
    {
        if (currentState != State.IDLE || unitStats == null) return;

        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
            return;
        }

        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            FindClosestEnemy();
            if (currentTarget == null) return;
        }

        if (IsTargetInAttackRange())
        {
            PerformAttack();
        }
        else
        {
            MoveTowardsTarget();
        }
    }

    private void FindClosestEnemy()
    {
        // Usamos Linq para encontrar el objetivo más cercano que sea válido
        currentTarget = GameManager.Instance.GetAllUnits()
            // Filtramos para obtener solo unidades válidas (que no seamos nosotros, de otro equipo y vivas)
            .Where(unit => unit != null && unit != this && unit.teamID != this.teamID && unit.CurrentHealth > 0)
            // CORRECCIÓN: Ordenamos por la distancia a 'unit' (el enemigo potencial de la lista), no a 'currentTarget'.
            .OrderBy(unit => Vector3.Distance(transform.position, unit.transform.position))
            // Cogemos el primero de la lista ya ordenada (el más cercano).
            .FirstOrDefault();
    }

    private bool IsTargetInAttackRange()
    {
        if (currentTarget == null) return false;
        // Ahora usamos la distancia del grid, no la del mundo, para ser más precisos.
        int distance = Mathf.Abs(currentNode.gridX - currentTarget.currentNode.gridX) + Mathf.Abs(currentNode.gridZ - currentTarget.currentNode.gridZ);
        // Convertimos el rango de ataque a "casillas". Asumimos que 1 de rango = 1 casilla.
        return distance <= Mathf.CeilToInt(unitStats.attackRange);
    }

    private void PerformAttack()
    {
        currentState = State.ATTACKING;
        transform.LookAt(currentTarget.transform.position);

        Debug.Log($"{unitStats.unitName} ataca a {currentTarget.unitStats.unitName}");

        if (unitStats.unitType == UnitStats.UnitType.Ranged && unitStats.projectilePrefab != null)
        {
            GameObject projGO = Instantiate(unitStats.projectilePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Projectile projectile = projGO.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(this, currentTarget, unitStats.attackDamage);
        }
        else
        {
            currentTarget.TakeDamage(unitStats.attackDamage);
        }

        attackCooldown = 1f / unitStats.attackSpeed;
        currentState = State.IDLE;
    }

    // --- LA NUEVA LÓGICA DE MOVIMIENTO ---
    private void MoveTowardsTarget()
    {
        Node bestNextNode = FindBestNextNode();
        if (bestNextNode != null)
        {
            StartCoroutine(MoveToNode(bestNextNode));
        }
    }

    private Node FindBestNextNode()
    {
        if (currentTarget == null || gridManager == null) return null;

        List<Node> neighbours = gridManager.GetNeighbours(currentNode); // Necesitamos añadir GetNeighbours de vuelta
        Node bestNode = null;
        float minDistance = float.MaxValue;

        foreach (Node neighbour in neighbours)
        {
            if (neighbour.isWalkable)
            {
                float distanceToTarget = Vector3.Distance(neighbour.worldPosition, currentTarget.transform.position);
                if (distanceToTarget < minDistance)
                {
                    minDistance = distanceToTarget;
                    bestNode = neighbour;
                }
            }
        }
        return bestNode;
    }
    
    private IEnumerator MoveToNode(Node targetNode)
    {
        currentState = State.MOVING;

        if (currentNode != null) currentNode.isWalkable = true;
        currentNode = targetNode;
        currentNode.isWalkable = false;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = targetNode.worldPosition;
        float time = 0f;

        while (time < 1f / unitStats.moveSpeed)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, time * unitStats.moveSpeed);
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
        currentState = State.IDLE;
    }
    
    // Funciones de vida y muerte
    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0) Die();
    }

    public void Die()
    {
        StopAllCoroutines();
        if (currentNode != null) currentNode.isWalkable = true;
        GameManager.Instance.UnregisterUnit(this);
        Destroy(gameObject);
    }
}