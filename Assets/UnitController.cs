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
    private float actionCooldown;
    private bool isMoving = false;

    void Start()
    {
        CurrentHealth = baseStats.maxHealth;
        // CORREGIDO: Usando la función moderna recomendada por Unity.
        gridManager = FindAnyObjectByType<GridManager>(); 
        
        if (GameManager.Instance != null) GameManager.Instance.RegisterUnit(this);

        currentNode = gridManager.NodeFromWorldPoint(transform.position);
        if (currentNode != null) gridManager.SetUnitOnNode(this, currentNode);
    }

    public void TakeAction()
    {
        if (actionCooldown > 0)
        {
            actionCooldown -= Time.deltaTime;
            return;
        }
        if (isMoving) return;

        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            UnreserveCurrentDestination();
            FindBestTarget();
        }

        if (currentTarget != null)
        {
            if (IsInAttackRange())
            {
                Attack();
            }
            else
            {
                if (destinationNode == null)
                {
                    destinationNode = FindBestAttackNode(currentTarget);
                    if (destinationNode != null)
                    {
                        gridManager.ReserveNode(destinationNode);
                    }
                }

                if (destinationNode != null)
                {
                    MoveOneStep();
                }
                else
                {
                    currentTarget = null;
                }
            }
        }
    }

    void FindBestTarget()
    {
        var allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        currentTarget = allUnits
            .Where(u => u.teamID != this.teamID && u.CurrentHealth > 0)
            .OrderBy(u => Vector3.Distance(transform.position, u.transform.position))
            .FirstOrDefault();
    }
    
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
        
        float journeyDuration = (1f / baseStats.moveSpeed) * 0.9f;
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
        if (GameManager.Instance != null) GameManager.Instance.UnregisterUnit(this);

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