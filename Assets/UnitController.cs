using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UnitController : MonoBehaviour
{
    private enum UnitActionState { IDLE, MOVING, ATTACKING, WAITING }
    private UnitActionState currentState = UnitActionState.IDLE;
    
    [SerializeField] private UnitStats baseStats;
    public int teamID;
    public int CurrentHealth { get; private set; }
    private GridManager gridManager;

    private UnitController currentTarget;
    private Node currentNode;
    private float attackCooldown;
    private float waitingTimer;

    public enum TargetPriority { Closest, LowestHealth }
    [Header("Configuración de IA")]
    public TargetPriority targetPriority = TargetPriority.Closest;

    // Este método es llamado por el GridManager al instanciar la unidad
    public void Initialize(GridManager manager, Node startingNode)
    {
        gridManager = manager;
        currentNode = startingNode;
        gridManager.SetUnitOnNode(this, currentNode);
        
        CurrentHealth = baseStats.maxHealth;
        GameManager.Instance.RegisterUnit(this);
    }
    
    public void EvaluateAction()
    {
        if (currentState == UnitActionState.MOVING || currentState == UnitActionState.ATTACKING) return;
        
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }

        if (currentState == UnitActionState.WAITING)
        {
            waitingTimer -= Time.deltaTime;
            if (waitingTimer <= 0)
            {
                currentState = UnitActionState.IDLE;
            }
            return;
        }
        
        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            FindBestTarget();
        }

        if (currentTarget != null)
        {
            if (IsInAttackRange())
            {
                if (attackCooldown <= 0) Attack();
            }
            else
            {
                MoveTowardsTarget();
            }
        }
    }

    void MoveTowardsTarget()
    {
        // MODIFICACIÓN CLAVE: Solo busca el mejor nodo de ataque y no intenta un nodo de apoyo si no hay uno.
        Node destinationNode = FindBestAttackNode(currentTarget);
        
        if (destinationNode != null && destinationNode != currentNode)
        {
            List<Node> path = gridManager.FindPath(transform.position, destinationNode.worldPosition);
            if (path != null && path.Count > 0)
            {
                StartCoroutine(MoveAnimation(path[0]));
            }
            else
            {
                // El camino está bloqueado o no es válido, entrar en modo de espera
                currentState = UnitActionState.WAITING;
                waitingTimer = 0.5f;
            }
        }
        else
        {
            // No hay destino válido para atacar directamente, entrar en modo de espera
            currentState = UnitActionState.WAITING;
            waitingTimer = 0.5f;
        }
    }

    IEnumerator MoveAnimation(Node targetNode)
    {
        currentState = UnitActionState.MOVING;

        // Reservar el nodo de destino para que otros no lo planeen
        gridManager.ReserveNode(targetNode);

        Vector3 startPos = transform.position;
        Vector3 endPos = targetNode.worldPosition;
        transform.LookAt(new Vector3(endPos.x, transform.position.y, endPos.z));
        
        float timeToMove = 1f / baseStats.moveSpeed;
        float elapsedTime = 0f;
        
        while(elapsedTime < timeToMove)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / timeToMove);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.position = endPos;
        gridManager.ClearNode(currentNode);
        currentNode = targetNode;
        gridManager.SetUnitOnNode(this, currentNode);
        
        gridManager.UnreserveNode(targetNode);
        currentState = UnitActionState.IDLE;
    }

    void Attack()
    {
        currentState = UnitActionState.ATTACKING;
        attackCooldown = 1f / baseStats.attackSpeed;
        
        transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));
        currentTarget.TakeDamage(baseStats.attackDamage);
        
        StartCoroutine(AttackCooldown());
    }

    IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        currentState = UnitActionState.IDLE;
    }

    void FindBestTarget()
    {
        IQueryable<UnitController> activeEnemies = FindObjectsByType<UnitController>(FindObjectsSortMode.None)
            .Where(u => u.teamID != this.teamID && u.CurrentHealth > 0)
            .AsQueryable();

        if (targetPriority == TargetPriority.Closest)
        {
            currentTarget = activeEnemies.OrderBy(u => Vector3.Distance(transform.position, u.transform.position)).FirstOrDefault();
        }
        else if (targetPriority == TargetPriority.LowestHealth)
        {
            currentTarget = activeEnemies.OrderBy(u => u.CurrentHealth).FirstOrDefault();
        }
    }
    
    Node FindBestAttackNode(UnitController target)
    {
        List<Node> reachableNodes = new List<Node>();
        Node targetNode = gridManager.NodeFromWorldPoint(target.transform.position);
        if (targetNode == null) return null;

        // Itera sobre los 8 vecinos del nodo objetivo
        foreach (var neighbour in gridManager.GetNeighbours(targetNode))
        {
            // Verifica si el vecino es un nodo disponible (caminable, no ocupado, no reservado)
            // Y si hay un camino válido desde la posición actual de la unidad hasta ese vecino.
            if (neighbour.IsAvailable())
            {
                // La distancia de ataque ya se maneja con el rango de ataque de la unidad.
                // Aquí solo nos aseguramos de que el nodo esté en la cuadrícula y sea accesible.
                if (gridManager.FindPath(transform.position, neighbour.worldPosition) != null) 
                {
                    reachableNodes.Add(neighbour);
                }
            }
        }

        if (reachableNodes.Count == 0) return null;
        
        // Ordena los nodos alcanzables por distancia a la unidad (para que se muevan al más cercano)
        // y selecciona el primero.
        return reachableNodes.OrderBy(n => Vector3.Distance(transform.position, n.worldPosition)).FirstOrDefault();
    }

    // El método FindSupportNode ya no será llamado si solo quieres que se muevan a celdas de ataque directas.
    // Lo mantenemos aquí por si decides reutilizarlo para otra lógica o para referencia.
    Node FindSupportNode(UnitController targetEnemy)
    {
        var frontlineAllies = FindObjectsByType<UnitController>(FindObjectsSortMode.None)
            .Where(u => u.teamID == this.teamID && u != this && u.CurrentHealth > 0 && Vector3.Distance(u.transform.position, targetEnemy.transform.position) <= gridManager.TileSize * 1.5f)
            .ToList();

        if (frontlineAllies.Count == 0) return null;

        List<Node> potentialSupportNodes = new List<Node>();

        foreach (var ally in frontlineAllies)
        {
            Node allyNode = gridManager.NodeFromWorldPoint(ally.transform.position);
            if (allyNode == null) continue;

            foreach (var neighbour in gridManager.GetNeighbours(allyNode))
            {
                if (neighbour.IsAvailable() && gridManager.FindPath(transform.position, neighbour.worldPosition) != null)
                {
                    potentialSupportNodes.Add(neighbour);
                }
            }
        }

        if (potentialSupportNodes.Count == 0) return null;
        
        return potentialSupportNodes
            .OrderBy(n => Vector3.Distance(n.worldPosition, targetEnemy.transform.position))
            .FirstOrDefault();
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
        GameManager.Instance.UnregisterUnit(this);
        StopAllCoroutines();
        if (currentNode != null) gridManager.ClearNode(currentNode);
        Destroy(gameObject);
    }
}