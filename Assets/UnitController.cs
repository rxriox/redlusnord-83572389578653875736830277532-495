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
        currentTarget = GameManager.Instance.GetAllUnits()
            .Where(unit => unit != null && unit != this && unit.teamID != this.teamID && unit.CurrentHealth > 0)
            .OrderBy(unit => Vector3.Distance(transform.position, unit.transform.position))
            .FirstOrDefault();
    }

    private bool IsTargetInAttackRange()
    {
        if (currentTarget == null) return false;
        int distance = Mathf.Abs(currentNode.gridX - currentTarget.currentNode.gridX) + Mathf.Abs(currentNode.gridZ - currentTarget.currentNode.gridZ);
        return distance <= Mathf.CeilToInt(unitStats.attackRange);
    }

    private void PerformAttack()
    {
        currentState = State.ATTACKING;
        transform.LookAt(currentTarget.transform.position);

        if (unitStats.unitType == UnitStats.UnitType.Ranged && unitStats.projectilePrefab != null)
        {
            GameObject projGO = Instantiate(unitStats.projectilePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Projectile projectile = projGO.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(this, currentTarget, unitStats.attackDamage);
        }
        else
        {
            Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ataca a {GetTeamTag(currentTarget.teamID)} {currentTarget.unitStats.unitName}");
            currentTarget.TakeDamage(unitStats.attackDamage, this);
        }

        attackCooldown = 1f / unitStats.attackSpeed;
        currentState = State.IDLE;
    }

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

    public void TakeDamage(float damage, UnitController attacker)
    {
        Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ha recibido {damage} de daño de {GetTeamTag(attacker.teamID)} {attacker.unitStats.unitName}.");
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    public void Die()
    {
        Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ha sido eliminada.");
        StopAllCoroutines();
        if (currentNode != null) currentNode.isWalkable = true;
        GameManager.Instance.UnregisterUnit(this);
        Destroy(gameObject);
    }
     public static string GetTeamTag(int teamID)
    {
        if (teamID == 0) return "<color=#42A5F5>[Aliada]</color>";   // Azul para aliados
        if (teamID == 1) return "<color=#EF5350>[Enemiga]</color>";   // Rojo para enemigos
        return "[Equipo ?]";
    }
}