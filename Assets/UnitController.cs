using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UnitController : MonoBehaviour
{
    public UnitStats unitStats;
    public int teamID;
    public Node currentNode;
    public UnitIconController originatingIcon;
    public float CurrentHealth { get; private set; }

    // POLISH: State machine is more robust.
    private enum State { IDLE, MOVING, ATTACKING, CHASING }
    private State currentState = State.IDLE;

    private UnitController currentTarget;
    private List<Node> currentPath; // POLISH: Stores the calculated path
    private float attackCooldown = 0f;
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        CurrentHealth = unitStats.maxHealth;
    }

    public void EvaluateAction()
    {
        if (unitStats == null || currentState == State.MOVING) return;

        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }

        // POLISH: Only find a new target if the current one is invalid. Much better for performance.
        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            currentState = State.IDLE;
            FindClosestEnemy();
            if (currentTarget == null)
            {
                // No enemies left, do nothing.
                return;
            }
        }

        if (IsTargetInAttackRange())
        {
            // Target is in range
            if (attackCooldown <= 0)
            {
                PerformAttack();
            }
            else
            {
                // POLISH: If in range but attack is on cooldown, wait (IDLE state).
                // Turn to face the target while waiting.
                transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));
                currentState = State.IDLE; 
            }
        }
        else
        {
            // Target is out of range, move towards it.
            currentState = State.CHASING;
            MoveTowardsTarget();
        }
    }

    private void FindClosestEnemy()
    {
        // This logic is good, no changes needed here.
        currentTarget = GameManager.Instance.GetAllUnits()
            .Where(unit => unit != null && unit != this && unit.teamID != this.teamID && unit.CurrentHealth > 0)
            .OrderBy(unit => Vector3.Distance(transform.position, unit.transform.position))
            .FirstOrDefault();
    }

    private bool IsTargetInAttackRange()
    {
        if (currentTarget == null) return false;

        // Using Chebyshev distance (for square ranges) which is more common for grid-based games.
        // It's the greater of the distances along the x or z axes.
        int dist_x = Mathf.Abs(currentNode.gridX - currentTarget.currentNode.gridX);
        int dist_z = Mathf.Abs(currentNode.gridZ - currentTarget.currentNode.gridZ);
        int distance = Mathf.Max(dist_x, dist_z);

        return distance <= unitStats.attackRange;
    }

    private void PerformAttack()
    {
        currentState = State.ATTACKING;
        transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));

        // Ranged attack logic is fine
        if (unitStats.unitType == UnitStats.UnitType.Ranged && unitStats.projectilePrefab != null)
        {
            // Consider adding a small delay here to sync with an attack animation
            GameObject projGO = ObjectPooler.Instance.SpawnFromPool("Proyectil", transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Projectile projectile = projGO.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(this, currentTarget, unitStats.attackDamage);
        }
        else // Melee attack
        {
            // Consider adding a small delay here to sync with an attack animation
            Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ataca a {GetTeamTag(currentTarget.teamID)} {currentTarget.unitStats.unitName}");
            currentTarget.TakeDamage(unitStats.attackDamage, this);
        }

        attackCooldown = 1f / unitStats.attackSpeed;
        
        // POLISH: After attacking, we don't go to IDLE immediately.
        // The state machine in EvaluateAction() will decide the next action.
    }

    private void MoveTowardsTarget()
    {
        if (gridManager == null || currentTarget == null) return;

        // Calculate a new path using A*
        currentPath = gridManager.FindPath(currentNode, currentTarget.currentNode);

        if (currentPath != null && currentPath.Count > 0)
        {
            // Move to the first node in the path
            StartCoroutine(MoveToNode(currentPath[0]));
        }
        else
        {
            // Can't find a path, maybe the target is blocked off. Go idle.
            currentState = State.IDLE;
        }
    }

//borar abajo
    private Node FindBestNextNode()
    {
        if (currentTarget == null || gridManager == null) return null;

        List<Node> neighbours = gridManager.GetNeighbours(currentNode);
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
//borrrar ariba
    private IEnumerator MoveToNode(Node targetNode)
    {
        currentState = State.MOVING;

        // Free up the current node and occupy the target node
        if (currentNode != null) currentNode.isWalkable = true;
        currentNode = targetNode;
        currentNode.isWalkable = false;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = targetNode.worldPosition;
        float time = 0f;

        // Turn to face the direction of movement
        if(endPosition - startPosition != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(endPosition - startPosition);
        }

        while (time < 1f / unitStats.moveSpeed)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, time * unitStats.moveSpeed);
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
        
        // POLISH: After moving, immediately re-evaluate. 
        // Don't just go to IDLE, as the unit might need to move again or attack.
        currentState = State.CHASING; // Set to chasing to allow EvaluateAction to decide next step
    }

    public void TakeDamage(float damage, UnitController attacker)
    {
        Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ha recibido {damage} de daño de {GetTeamTag(attacker.teamID)} {attacker.unitStats.unitName}.");
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die(attacker);
        }
    }

    public void Die(UnitController killer)
    {
        Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ha sido eliminada.");
        if (killer != null)
        {
            Debug.Log($"{GetTeamTag(killer.teamID)} {killer.unitStats.unitName} ha eliminado a {GetTeamTag(this.teamID)} {this.unitStats.unitName}");
        }
        
        StopAllCoroutines();
        if (currentNode != null) currentNode.isWalkable = true;
        GameManager.Instance.UnregisterUnit(this);
        Destroy(gameObject);
    }
     public static string GetTeamTag(int teamID)
    {
        if (teamID == 0) return "<color=#42A5F5>[Aliada]</color>";   // BLUE = ALLIES
        if (teamID == 1) return "<color=#EF5350>[Enemiga]</color>";   // RED = ENEMIES
        return "[Equipo ?]";
    }
}