using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class UnitController : MonoBehaviour
{
    private enum UnitActionState { IDLE, MOVING, ATTACKING }
    private UnitActionState currentState = UnitActionState.IDLE;
    private UnitController currentTarget;
    private float attackCooldown = 0f;
    private GridManager gridManager;
    public UnitStats unitStats;
    private HealthBarUI healthBar;
    public Node currentNode;
    public int teamID;

    private float currentHealth;
    private float currentMana;

    public float CurrentHealth { get { return currentHealth; } }

    void Start()
    {
        if (unitStats == null) return;

        currentHealth = unitStats.maxHealth;
        currentMana = unitStats.maxMana;

        healthBar = GetComponentInChildren<HealthBarUI>();
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth, unitStats.maxHealth);
        }
        gridManager = FindFirstObjectByType<GridManager>();
    }

    public void TakeDamage(float damage)
    {
        if (unitStats == null) return;
        currentHealth -= damage;
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth, unitStats.maxHealth);
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (GameManager.Instance != null) GameManager.Instance.UnregisterUnit(this);
        if (currentNode != null) currentNode.isWalkable = true;
        Destroy(gameObject);
        StopAllCoroutines();
    }

    public void EvaluateAction()
    {
        if (unitStats == null || currentState == UnitActionState.MOVING) return;

        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            FindTarget();
            if (currentTarget == null)
            {
                currentState = UnitActionState.IDLE;
                return;
            }
        }

        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
            return;
        }

        if (Vector3.Distance(transform.position, currentTarget.transform.position) <= unitStats.attackRange)
        {
            StopAllCoroutines();
            currentState = UnitActionState.ATTACKING;
            AttackTarget();
        }
        else
        {
            currentState = UnitActionState.MOVING;
            MoveToTarget();
        }
    }
    
    private void FindTarget()
    {
        currentTarget = GameManager.Instance.GetAllUnits()
            .Where(u => u != null && u.teamID != this.teamID && u.CurrentHealth > 0)
            .OrderBy(u => Vector3.Distance(transform.position, u.transform.position))
            .FirstOrDefault();
    }
    
    private void AttackTarget()
    {
        if (currentTarget == null) return;
        transform.LookAt(currentTarget.transform.position);

        if (unitStats.unitType == UnitStats.UnitType.Ranged && unitStats.projectilePrefab != null)
        {
            GameObject projGO = Instantiate(unitStats.projectilePrefab, transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity);
            Projectile projectile = projGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(this, currentTarget, unitStats.attackDamage);
            }
        }
        else
        {
            currentTarget.TakeDamage(unitStats.attackDamage);
        }
        attackCooldown = 1f / unitStats.attackSpeed;
        currentState = UnitActionState.IDLE; // Listo para la siguiente evaluación
    }

    private void MoveToTarget()
    {
        if (currentTarget == null || gridManager == null) return;
        List<Node> path = gridManager.FindPath(transform.position, currentTarget.transform.position);
        if (path != null && path.Count > 1) // > 1 para no movernos a la casilla de al lado del enemigo
        {
            // Quitamos el último nodo para dejar espacio para atacar
            path.RemoveAt(path.Count - 1);
            StopAllCoroutines();
            StartCoroutine(MoveAlongPath(path));
        }
        else
        {
            currentState = UnitActionState.IDLE; // No hay camino, esperamos
        }
    }

    private IEnumerator MoveAlongPath(List<Node> path)
    {
        foreach (Node waypoint in path)
        {
            Vector3 targetPos = new Vector3(waypoint.worldPosition.x, transform.position.y, waypoint.worldPosition.z);
            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, unitStats.moveSpeed * Time.deltaTime);
                yield return null;
            }
            this.currentNode.isWalkable = true;
            this.currentNode = waypoint;
            this.currentNode.isWalkable = false;
        }
        currentState = UnitActionState.IDLE; // Terminamos de movernos
    }

    [Header("Referencias Internas")]
    [Tooltip("El icono de la UI que originó esta unidad. Se asigna automáticamente.")]
    public UnitIconController originatingIcon;
}