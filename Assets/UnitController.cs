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
    public int MaxHealth => unitStats.maxHealth;
    public float CurrentHealth { get; private set; }
    public int CurrentAttackDamage => unitStats.attackDamage;
    public float CurrentAttackSpeed => unitStats.attackSpeed;
    public float CurrentMoveSpeed => unitStats.moveSpeed;

    private enum State { IDLE, MOVING, ATTACKING }
    private State currentState = State.IDLE;

    private UnitController currentTarget;
    private List<Node> currentPath;
    private float attackCooldown = 0f;
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        CurrentHealth = unitStats.maxHealth;
    }

    public void EvaluateAction()
    {
        if (currentState == State.MOVING || currentState == State.ATTACKING) return;
        
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }

        UnitController immediateTarget = FindEnemyInAttackRange();
        if (immediateTarget != null)
        {
            currentTarget = immediateTarget;
            if (attackCooldown <= 0)
            {
                PerformAttack();
            }
            return;
        }

        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            FindClosestEnemy();
            if (currentTarget == null)
            {
                currentState = State.IDLE;
                return;
            }
        }
        MoveTowardsTarget();
    }

    private UnitController FindEnemyInAttackRange()
    {
        return GameManager.Instance.GetAllUnits()
            .Where(unit => unit != null && unit.teamID != this.teamID && unit.CurrentHealth > 0 && IsUnitWithinAttackRange(unit))
            .OrderBy(unit => Vector3.Distance(transform.position, unit.transform.position))
            .FirstOrDefault();
    }
    
    private bool IsUnitWithinAttackRange(UnitController unit)
    {
        if (unit == null || unit.currentNode == null || this.currentNode == null) return false;
        int dist_x = Mathf.Abs(currentNode.gridX - unit.currentNode.gridX);
        int dist_z = Mathf.Abs(currentNode.gridZ - unit.currentNode.gridZ);
        int distance = Mathf.Max(dist_x, dist_z);

        return distance <= unitStats.attackRange;
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
        return IsUnitWithinAttackRange(currentTarget);
    }

    private void PerformAttack()
    {
        currentState = State.ATTACKING;
        transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));

        if (unitStats.unitType == UnitStats.UnitType.Ranged && unitStats.projectilePrefab != null)
        {
            GameObject projGO = ObjectPooler.Instance.SpawnFromPool("Proyectil", transform.position + Vector3.up * 0.5f, Quaternion.identity);
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
        StartCoroutine(ResetStateAfterAction(0.1f));
    }

    private void MoveTowardsTarget()
{
    if (gridManager == null || currentTarget == null || currentState != State.IDLE) return;
    currentPath = gridManager.FindPath(currentNode, currentTarget.currentNode);
    if (currentPath != null && currentPath.Count > 0)
    {
        Node nextNodeInPath = currentPath[0];
        bool isNextNodeOccupied = (GameManager.Instance.GetUnitAtNode(nextNodeInPath) != null);
        if (nextNodeInPath.isWalkable && !isNextNodeOccupied)
        {
            nextNodeInPath.isWalkable = false;

            if (currentNode != null)
            {
                currentNode.isWalkable = true;
            }
            
            Node previousNode = currentNode;
            currentNode = nextNodeInPath;

            StartCoroutine(AnimateMove(previousNode, nextNodeInPath));
        }
        else
        {
            currentState = State.IDLE;
        }
    }
    else
    {
        currentState = State.IDLE;
    }
}
private IEnumerator AnimateMove(Node from, Node to)
{
    currentState = State.MOVING;

    Vector3 startPosition = from.worldPosition;
    Vector3 endPosition = to.worldPosition;
    
    // Rotacion
    if(endPosition - startPosition != Vector3.zero)
    {
        transform.rotation = Quaternion.LookRotation(endPosition - startPosition);
    }
    
    // Movimiento
    float time = 0f;
    float moveDuration = 1f / unitStats.moveSpeed;
    while (time < moveDuration)
    {
        transform.position = Vector3.Lerp(startPosition, endPosition, time / moveDuration);
        time += Time.deltaTime;
        yield return null;
    }

    transform.position = endPosition;
    currentState = State.IDLE; 
}
    
    private IEnumerator ResetStateAfterAction(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentState = State.IDLE;
    }


    private IEnumerator AnimateMoveToPosition(Vector3 targetPosition)
{
    currentState = State.MOVING;

    Vector3 startPosition = transform.position;
    
    if(targetPosition - startPosition != Vector3.zero)
    {
        transform.rotation = Quaternion.LookRotation(targetPosition - startPosition);
    }
    
    float time = 0f;
    float moveDuration = 1f / unitStats.moveSpeed;
    while (time < moveDuration)
    {
        transform.position = Vector3.Lerp(startPosition, targetPosition, time / moveDuration);
        time += Time.deltaTime;
        yield return null;
    }

    transform.position = targetPosition;
    currentState = State.IDLE; 
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
        GameManager.Instance.CheckForCombatEnd();
        Destroy(gameObject);
    }
     public static string GetTeamTag(int teamID)
    {
        if (teamID == 0) return "<color=#42A5F5>[Aliada]</color>";   // BLUE = ALLIES
        if (teamID == 1) return "<color=#EF5350>[Enemiga]</color>";   // RED = ENEMIES
        return "[Equipo ?]";
    }
}