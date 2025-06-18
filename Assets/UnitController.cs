using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UnitController : MonoBehaviour
{
    [SerializeField] private UnitStats baseStats;
    public int teamID;
    public int CurrentHealth { get; private set; }

    private GridManager gridManager;

    private UnitController currentTarget;
    private Node currentNode;
    private Node destinationNode;
    private float attackCooldown;
    private bool isExecutingAction = false;

    void Start()
    {
        CurrentHealth = baseStats.maxHealth;
        gridManager = FindAnyObjectByType<GridManager>();
        
        GameManager.Instance.RegisterUnit(this);

        currentNode = gridManager.NodeFromWorldPoint(transform.position);
        if (currentNode != null)
        {
            gridManager.SetUnitOnNode(this, currentNode);
        }
    }

    /// <summary>
    /// El GameManager llama a este método cuando es el turno de la unidad para PENSAR.
    /// </summary>
    public void EvaluateAction()
    {
        // Si ya estamos ejecutando una acción (moviéndonos o atacando), no evaluamos una nueva.
        if (isExecutingAction) return;
        
        // Gestionamos el cooldown del ataque
        if (attackCooldown > 0) attackCooldown -= Time.deltaTime;

        // 1. ADQUISICIÓN DE OBJETIVO
        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            FindBestTarget();
        }

        if (currentTarget != null)
        {
            // 2. DECISIÓN DE ACCIÓN
            if (IsInAttackRange())
            {
                if(attackCooldown <= 0) Attack();
            }
            else
            {
                // Si ya tenemos un destino, no buscamos uno nuevo. Nos comprometemos.
                if (destinationNode == null)
                {
                    destinationNode = FindBestAttackNode(currentTarget);
                }

                if (destinationNode != null)
                {
                    StartCoroutine(MoveToDestination());
                }
                else
                {
                    // No hay un camino, olvidamos el objetivo para reevaluarlo en el siguiente ciclo del GameManager
                    currentTarget = null;
                }
            }
        }
    }

    IEnumerator MoveToDestination()
    {
        isExecutingAction = true;
        gridManager.ReserveNode(destinationNode);

        List<Node> path = gridManager.FindPath(transform.position, destinationNode.worldPosition);

        if (path != null && path.Count > 0)
        {
            gridManager.ClearNode(currentNode);

            // Moverse a lo largo de toda la ruta de forma fluida
            foreach (Node step in path)
            {
                Vector3 targetPosition = step.worldPosition;
                transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z));
                
                float timeToMove = Vector3.Distance(transform.position, targetPosition) / baseStats.moveSpeed;
                float elapsedTime = 0;
                
                while(elapsedTime < timeToMove)
                {
                    transform.position = Vector3.Lerp(transform.position, targetPosition, elapsedTime / timeToMove);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                transform.position = targetPosition;
            }

            currentNode = destinationNode;
            gridManager.SetUnitOnNode(this, currentNode);
        }
        
        UnreserveCurrentDestination();
        isExecutingAction = false;
    }

    void Attack()
    {
        isExecutingAction = true;
        transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));
        
        // Aquí iría la animación de ataque
        StartCoroutine(AttackAnimation());
    }

    IEnumerator AttackAnimation()
    {
        // Simula el tiempo que dura la animación de ataque
        float animationTime = 1f / baseStats.attackSpeed;
        attackCooldown = animationTime;

        // A la mitad de la animación, aplicamos el daño
        yield return new WaitForSeconds(animationTime / 2);
        
        if (currentTarget != null && currentTarget.CurrentHealth > 0)
        {
            currentTarget.TakeDamage(baseStats.attackDamage);
            Debug.Log($"{name} ataca a {currentTarget.name}");
        }

        // Esperar el resto de la animación
        yield return new WaitForSeconds(animationTime / 2);
        isExecutingAction = false;
    }

    // --- El resto de métodos de ayuda se mantienen sin cambios ---
    void FindBestTarget()
    {
        currentTarget = FindObjectsByType<UnitController>(FindObjectsSortMode.None)
            .Where(u => u.teamID != this.teamID && u.CurrentHealth > 0)
            .OrderBy(u => Vector3.Distance(transform.position, u.transform.position))
            .FirstOrDefault();
    }
    
    Node FindBestAttackNode(UnitController target)
    {
        List<Node> reachableNodes = new List<Node>();
        Node targetNode = gridManager.NodeFromWorldPoint(target.transform.position);
        if (targetNode == null) return null;

        foreach (var neighbour in gridManager.GetNeighbours(targetNode))
        {
            if (neighbour.IsAvailable())
            {
                if (gridManager.FindPath(transform.position, neighbour.worldPosition) != null)
                {
                    reachableNodes.Add(neighbour);
                }
            }
        }

        if (reachableNodes.Count == 0) return null;
        return reachableNodes.OrderBy(n => Vector3.Distance(transform.position, n.worldPosition)).FirstOrDefault();
    }
    
    bool IsInAttackRange() => currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) <= baseStats.attackRange;
    
    public void TakeDamage(int damage)
    {
        if (CurrentHealth <= 0) return;
        CurrentHealth -= damage;
        if (CurrentHealth <= 0) { CurrentHealth = 0; Die(); }
    }

    void Die()
    {
        isExecutingAction = true; // Evita más acciones
        GameManager.Instance.UnregisterUnit(this);
        StopAllCoroutines();
        if (currentNode != null) gridManager.ClearNode(currentNode);
        UnreserveCurrentDestination();
        Destroy(gameObject);
    }

    void UnreserveCurrentDestination()
    {
        if (destinationNode != null)
        {
            gridManager.UnreserveNode(destinationNode);
            destinationNode = null;
        }
    }
}