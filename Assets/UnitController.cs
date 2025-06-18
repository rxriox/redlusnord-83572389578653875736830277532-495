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
        Node destinationNode = null;

        // Decide el tipo de movimiento basado en el UnitType de la unidad
        if (baseStats.unitType == UnitStats.UnitType.Melee)
        {
            destinationNode = FindBestMeleeAttackNode(currentTarget);
        }
        else if (baseStats.unitType == UnitStats.UnitType.Ranged)
        {
            // Para unidades a distancia, encontramos un nodo para movernos a su rango de ataque.
            destinationNode = FindBestRangedMovementNode(currentTarget);
        }
        
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
            // No hay destino válido para atacar directamente (o moverse a rango), entrar en modo de espera
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
    
    /// <summary>
    /// Encuentra el mejor nodo de ataque adyacente para unidades cuerpo a cuerpo.
    /// </summary>
    /// <param name="target">La unidad objetivo.</param>
    /// <returns>El nodo más cercano al que moverse para atacar, o null si no hay uno.</returns>
    Node FindBestMeleeAttackNode(UnitController target)
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

    /// <summary>
    /// Encuentra el mejor nodo para que una unidad a distancia se mueva dentro de su rango de ataque.
    /// Prioriza estar en rango y ser el nodo accesible más cercano.
    /// </summary>
    /// <param name="target">La unidad objetivo.</param>
    /// <returns>El nodo más cercano al que moverse para estar en rango de ataque, o null si no hay uno.</returns>
    Node FindBestRangedMovementNode(UnitController target)
    {
        List<Node> validMovementNodes = new List<Node>();
        Node targetNode = gridManager.NodeFromWorldPoint(target.transform.position);
        if (targetNode == null) return null;

        // Define un radio de búsqueda basado en el rango de ataque.
        // Se añade un margen para asegurar que el centro del nodo esté dentro del rango.
        float searchRadiusTiles = baseStats.attackRange / gridManager.TileSize + 1.0f; 
        
        // Calcular los límites de la cuadrícula para la búsqueda
        int startX = Mathf.Max(0, targetNode.gridX - Mathf.CeilToInt(searchRadiusTiles));
        int endX = Mathf.Min(gridManager.gridWidth - 1, targetNode.gridX + Mathf.CeilToInt(searchRadiusTiles));
        int startZ = Mathf.Max(0, targetNode.gridZ - Mathf.CeilToInt(searchRadiusTiles));
        int endZ = Mathf.Min(gridManager.gridHeight - 1, targetNode.gridZ + Mathf.CeilToInt(searchRadiusTiles));

        // Iterar sobre una sección de la cuadrícula alrededor del objetivo
        for (int x = startX; x <= endX; x++)
        {
            for (int z = startZ; z <= endZ; z++)
            {
                Node potentialNode = gridManager.grid[x, z]; // Acceder al array de la cuadrícula directamente
                
                // Si el nodo está disponible y se puede caminar sobre él
                if (potentialNode.IsAvailable())
                {
                    float distanceToTarget = Vector3.Distance(potentialNode.worldPosition, target.transform.position);
                    
                    // Comprobar si este nodo coloca al objetivo dentro del rango de ataque
                    if (distanceToTarget <= baseStats.attackRange)
                    {
                        // Asegurarse de que haya un camino a este nodo
                        if (gridManager.FindPath(transform.position, potentialNode.worldPosition) != null)
                        {
                            validMovementNodes.Add(potentialNode);
                        }
                    }
                }
            }
        }

        if (validMovementNodes.Count == 0) return null;

        // Ordena los nodos válidos por distancia a la unidad actual (el más cercano primero)
        return validMovementNodes.OrderBy(n => Vector3.Distance(transform.position, n.worldPosition)).FirstOrDefault();
    }


    // El método FindSupportNode se mantiene como referencia pero no se utiliza en el flujo principal de movimiento si se prefiere solo ataque directo.
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
