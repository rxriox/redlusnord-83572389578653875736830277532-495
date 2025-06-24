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
        if (unitStats == null) return;

        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
            return;
        }

        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            FindClosestEnemy();
            if (currentTarget == null)
            {
                currentState = UnitActionState.IDLE;
                return;
            }
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distanceToTarget <= unitStats.attackRange)
        {
            // Si está en rango, ataca.
            currentState = UnitActionState.ATTACKING;
            AttackTarget();
        }
        else
        {
            // Si NO está en rango, se mueve.
            currentState = UnitActionState.MOVING;
            MoveTowardsTarget(); 
        }
    }
    
    private void FindTarget()
    {
        currentTarget = GameManager.Instance.GetAllUnits()
            .Where(u => u != null && u.teamID != this.teamID && u.CurrentHealth > 0)
            .OrderBy(u => Vector3.Distance(transform.position, u.transform.position))
            .FirstOrDefault();
    }
    private void FindClosestEnemy()
    {
        currentTarget = GameManager.Instance.GetAllUnits()
            .Where(unit => unit != null && unit.teamID != this.teamID && unit.CurrentHealth > 0)
            .OrderBy(unit => Vector3.Distance(transform.position, unit.transform.position))
            .FirstOrDefault();
    }
    private void AttackTarget()
    {
        if (currentTarget == null) return;

        // Nos giramos para mirar al enemigo.
        transform.LookAt(currentTarget.transform.position);

        Debug.Log($"{unitStats.unitName} ataca a {currentTarget.unitStats.unitName}");

        // Lógica de ataque (Melee vs Rango)
        if (unitStats.unitType == UnitStats.UnitType.Ranged && unitStats.projectilePrefab != null)
        {
            // Creamos y lanzamos un proyectil.
            GameObject projGO = Instantiate(unitStats.projectilePrefab, transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity);
            Projectile projectile = projGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(this, currentTarget, unitStats.attackDamage);
            }
        }
        else // Si es Melee o no tiene proyectil
        {
            // Aplicamos el daño directamente.
            currentTarget.TakeDamage(unitStats.attackDamage);
        }

        // Reiniciamos el cooldown del ataque.
        attackCooldown = 1f / unitStats.attackSpeed;
    }

    private void MoveTowardsTarget()
    {
        if (currentTarget == null)
        {
            currentState = UnitActionState.IDLE;
            return;
        }
        
        // Detenemos cualquier corutina de movimiento anterior y empezamos la nueva.
        StopAllCoroutines();
        StartCoroutine(MoveDirectlyToTarget());
    }

    private IEnumerator MoveDirectlyToTarget()
    {
        // Mientras nuestro objetivo exista y estemos fuera de su rango de ataque...
        while (currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) > unitStats.attackRange)
        {
            // ...nos movemos hacia él en línea recta.
            transform.position = Vector3.MoveTowards(transform.position, currentTarget.transform.position, unitStats.moveSpeed * Time.deltaTime);
            // Hacemos que la unidad siempre mire a su objetivo mientras se mueve.
            transform.LookAt(currentTarget.transform.position);
            
            yield return null; // Esperamos al siguiente frame para continuar el movimiento.
        }

        // Una vez que el bucle termina (porque llegamos al rango o el objetivo murió),
        // volvemos al estado IDLE para que en el siguiente frame, EvaluateAction decida atacar.
        currentState = UnitActionState.IDLE;
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