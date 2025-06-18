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
    private Coroutine combatCoroutine;

    // --- Variables de Estado y Cooldown ---
    private UnitController currentTarget;
    private Node currentNode;
    private Node destinationNode;
    private float actionCooldown;
    private bool isMoving = false;

    void Start()
    {
        CurrentHealth = baseStats.maxHealth;
        gridManager = FindAnyObjectByType<GridManager>();

        currentNode = gridManager.NodeFromWorldPoint(transform.position);
        if (currentNode != null)
        {
            gridManager.SetUnitOnNode(this, currentNode);
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
        isMoving = false;
    }

    IEnumerator CombatLoop()
    {
        while (CurrentHealth > 0)
        {
            if (isMoving)
            {
                yield return null;
                continue;
            }

            if (actionCooldown > 0)
            {
                actionCooldown -= Time.deltaTime;
                yield return null;
                continue;
            }

            // --- LÓGICA DE DECISIÓN REFACTORIZADA ---

            // 1. ADQUISICIÓN DE OBJETIVO
            if (currentTarget == null || currentTarget.CurrentHealth <= 0)
            {
                UnreserveCurrentDestination(); // Limpiamos planes viejos
                FindBestTarget(); // Solo buscamos el mejor objetivo, sin pensar en la posición aún.
            }

            // Si tenemos un objetivo válido, decidimos qué hacer.
            if (currentTarget != null)
            {
                // 2. DECISIÓN DE ACCIÓN
                if (IsInAttackRange())
                {
                    // Si ya está en rango (ej. al inicio del combate o si un enemigo se acerca), atacamos.
                    Attack();
                }
                else // No está en rango, necesitamos movernos.
                {
                    // 3. PLANIFICACIÓN DE MOVIMIENTO
                    if (destinationNode == null)
                    {
                        // Buscamos una posición solo si es necesario.
                        destinationNode = FindBestAttackNode(currentTarget);
                        if (destinationNode != null)
                        {
                            gridManager.ReserveNode(destinationNode);
                        }
                    }

                    // 4. EJECUCIÓN DE MOVIMIENTO
                    if (destinationNode != null)
                    {
                        MoveOneStep();
                    }
                    else
                    {
                        // Tenemos un objetivo, pero está lejos Y no hay casillas alcanzables a su alrededor.
                        // En este caso, la unidad espera. No olvida a su objetivo, por si se abre un hueco.
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
    
    /// <summary>
    /// NUEVO MÉTODO: Se enfoca únicamente en encontrar el enemigo más prometedor.
    /// </summary>
    void FindBestTarget()
    {
        currentTarget = FindObjectsByType<UnitController>(FindObjectsSortMode.None)
            .Where(u => u.teamID != this.teamID && u.CurrentHealth > 0)
            .OrderBy(u => Vector3.Distance(transform.position, u.transform.position))
            .FirstOrDefault();
    }

    // --- El resto de métodos se mantienen igual ---

    void MoveOneStep()
    {
        List<Node> path = gridManager.FindPath(transform.position, destinationNode.worldPosition);

        if (path != null && path.Count > 0)
        {
            Node nextNode = path[0];
            StartCoroutine(MoveAnimation(nextNode));
            actionCooldown = 1f / baseStats.moveSpeed;
        }
        else
        {
            UnreserveCurrentDestination();
            currentTarget = null;
        }
    }

    IEnumerator MoveAnimation(Node nextNode)
    {
        isMoving = true;
        gridManager.ClearNode(currentNode);

        Vector3 startPos = transform.position;
        Vector3 targetPosition = nextNode.worldPosition;
        transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z));
        
        float journeyDuration = 1f / baseStats.moveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < journeyDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPosition, elapsedTime / journeyDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        currentNode = nextNode;
        gridManager.SetUnitOnNode(this, currentNode);

        if (currentNode == destinationNode)
        {
            UnreserveCurrentDestination();
        }
        isMoving = false;
    }
    
    void Attack()
    {
        transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));
        currentTarget.TakeDamage(baseStats.attackDamage);
        actionCooldown = 1f / baseStats.attackSpeed;
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
    
    bool IsInAttackRange()
    {
        return currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) <= baseStats.attackRange;
    }
    
    public void TakeDamage(int damage)
    {
        if (CurrentHealth <= 0) return;
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        StopAllCoroutines();
        if (currentNode != null)
        {
            gridManager.ClearNode(currentNode);
        }
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