using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// Controla el comportamiento individual de cada unidad en el autobattler,
/// incluyendo movimiento, ataque, selección de objetivo y gestión de estado.
/// </summary>
public class UnitController : MonoBehaviour
{
    private enum UnitActionState { IDLE, MOVING, ATTACKING, WAITING }
    private UnitActionState currentState = UnitActionState.IDLE;
    
    [Header("Referencias y Estadísticas")]
    [Tooltip("ScriptableObject que contiene las estadísticas base de esta unidad.")]
    [SerializeField] public UnitStats baseStats; 
    [Tooltip("ID del equipo al que pertenece esta unidad (0 para jugador, 1 para enemigo).")]
    public int teamID;
    [Tooltip("Prefab del UI de la barra de salud que se instanciará sobre la unidad.")]
    [SerializeField] private GameObject healthBarUIPrefab;
    [Tooltip("Offset vertical adicional para la barra de salud sobre la unidad.")]
    [SerializeField] private float healthBarVerticalOffset = 0.8f;

    // Salud actual de la unidad.
    public int CurrentHealth { get; private set; }
    
    // Referencia al GridManager para interactuar con la cuadrícula.
    private GridManager gridManager;

    // Referencia al controlador de la barra de salud de esta unidad.
    private HealthBarUI healthBarUI;

    // Objetivo actual de la unidad.
    private UnitController currentTarget;
    // Nodo de la cuadrícula en el que se encuentra actualmente la unidad.
    private Node currentNode;
    // Temporizador para el cooldown de ataque.
    private float attackCooldown;
    // Temporizador para el estado de espera (cuando la unidad no puede realizar una acción).
    private float waitingTimer;

    // Altura vertical de la unidad para posicionamiento.
    private float unitSpawnHeightOffset = 0f; 

    // Prioridad de selección de objetivo para la IA de la unidad.
    public enum TargetPriority { Closest, LowestHealth }
    [Header("Configuración de IA")]
    [Tooltip("Define cómo esta unidad seleccionará su objetivo (más cercano o con menos salud).")]
    public TargetPriority targetPriority = TargetPriority.Closest;

    /// <summary>
    /// Se llama una vez por frame. Usado para asegurarse de que la barra de salud siempre mire a la cámara.
    /// </summary>
    void Update()
    {
        // Se deja el LateUpdate en HealthBarUI para manejar el LookAt.
        // Este Update aquí en UnitController no es estrictamente necesario para la barra de salud,
        // pero se mantiene por si se añaden futuras lógicas que necesiten Update.
    }


    /// <summary>
    /// Inicializa la unidad cuando es creada o colocada en el tablero.
    /// </summary>
    /// <param name="manager">Instancia del GridManager.</param>
    /// <param name="startingNode">El nodo inicial donde se coloca la unidad.</param>
    public void Initialize(GridManager manager, Node startingNode)
    {
        gridManager = manager;
        currentNode = startingNode;
        // La unidad ocupa su nodo inicial en la cuadrícula.
        gridManager.SetUnitOnNode(this, currentNode);
        
        CurrentHealth = baseStats.maxHealth; // Establece la salud inicial.
        GameManager.Instance.RegisterUnit(this); // Registra la unidad en el GameManager.

        // Calcular la altura del prefab de la unidad para posicionarla correctamente.
        // Este cálculo se almacena para ser usado en el movimiento.
        Renderer unitRenderer = GetComponentInChildren<Renderer>();
        if (unitRenderer != null)
        {
            unitSpawnHeightOffset = unitRenderer.bounds.extents.y;
        }
        else
        {
            Collider unitCollider = GetComponentInChildren<Collider>();
            if (unitCollider != null)
            {
                unitSpawnHeightOffset = unitCollider.bounds.extents.y;
            }
            else
            {
                unitSpawnHeightOffset = 0.5f; 
            }
        }

        // Ajustar la posición Y de la unidad inmediatamente después de la inicialización
        // para asegurar que está correctamente sobre el tile si fue instanciada sin offset.
        // Esto es un doble chequeo ya que GridManager.PlaceUnitOnTile ya debería hacerlo.
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, currentNode.worldPosition.y + unitSpawnHeightOffset, currentPos.z);


        // Instanciar y configurar la barra de salud
        if (healthBarUIPrefab != null)
        {
            GameObject hbGO = Instantiate(healthBarUIPrefab, transform); 
            
            Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);
            bool hasRenderer = false;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (!r.enabled) continue;
                if (!hasRenderer)
                {
                    combinedBounds = r.bounds;
                    hasRenderer = true;
                }
                else
                {
                    combinedBounds.Encapsulate(r.bounds);
                }
            }

            float highestPointY = hasRenderer ? combinedBounds.max.y : transform.position.y + 1.8f; 

            hbGO.transform.position = new Vector3(transform.position.x, highestPointY + healthBarVerticalOffset, transform.position.z);
            hbGO.transform.SetParent(transform); 

            healthBarUI = hbGO.GetComponent<HealthBarUI>();
            if (healthBarUI != null)
            {
                healthBarUI.SetColor(teamID == 0 ? Color.green : Color.red);
                healthBarUI.SetHealthPercentage(CurrentHealth, baseStats.maxHealth);
            }
        }
    }
    
    /// <summary>
    /// Evalúa la acción que la unidad debe tomar en el ciclo de combate.
    /// Se llama repetidamente desde el CombatLoop del GameManager.
    /// </summary>
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

    /// <summary>
    /// Decide a qué nodo debe moverse la unidad en función de su tipo (cuerpo a cuerpo o a distancia).
    /// </summary>
    void MoveTowardsTarget()
    {
        Node destinationNode = null;

        if (baseStats.unitType == UnitStats.UnitType.Melee)
        {
            destinationNode = FindBestMeleeAttackNode(currentTarget);
        }
        else if (baseStats.unitType == UnitStats.UnitType.Ranged)
        {
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
                currentState = UnitActionState.WAITING;
                waitingTimer = 0.5f; 
            }
        }
        else
        {
            currentState = UnitActionState.WAITING;
            waitingTimer = 0.5f; 
        }
    }

    /// <summary>
    /// Corutina para animar el movimiento de la unidad a un nodo específico.
    /// </summary>
    /// <param name="targetNode">El nodo de la cuadrícula al que se moverá la unidad.</param>
    IEnumerator MoveAnimation(Node targetNode)
    {
        currentState = UnitActionState.MOVING;

        gridManager.ReserveNode(targetNode);

        Vector3 startPos = transform.position;
        // La posición final incluye el offset vertical.
        Vector3 endPos = new Vector3(targetNode.worldPosition.x, targetNode.worldPosition.y + unitSpawnHeightOffset, targetNode.worldPosition.z); 
        
        // Ajusta la rotación para mirar al objetivo, manteniendo la altura correcta.
        // Vector3 lookAtTarget = currentTarget != null ? currentTarget.transform.position : endPos;
        // transform.LookAt(new Vector3(lookAtTarget.x, transform.position.y, lookAtTarget.z));
        transform.LookAt(new Vector3(endPos.x, transform.position.y, endPos.z)); // Mirar al punto de destino en el plano XZ

        float timeToMove = 1f / baseStats.moveSpeed;
        float elapsedTime = 0f;
        
        while(elapsedTime < timeToMove)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / timeToMove);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.position = endPos; // Asegura la posición final.
        gridManager.ClearNode(currentNode);
        currentNode = targetNode;
        gridManager.SetUnitOnNode(this, currentNode);
        
        gridManager.UnreserveNode(targetNode);
        currentState = UnitActionState.IDLE;
    }

    /// <summary>
    /// Ejecuta el ataque de la unidad, aplicando daño directo o lanzando un proyectil.
    /// </summary>
    void Attack()
    {
        currentState = UnitActionState.ATTACKING;
        attackCooldown = 1f / baseStats.attackSpeed;
        
        // Asegura que la unidad mire al objetivo en el plano horizontal (XZ), manteniendo su altura.
        if (currentTarget != null)
        {
            transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));
        }

        if (baseStats.unitType == UnitStats.UnitType.Melee)
        {
            currentTarget.TakeDamage(baseStats.attackDamage);
        }
        else if (baseStats.unitType == UnitStats.UnitType.Ranged)
        {
            if (baseStats.projectilePrefab != null)
            {
                // La posición de inicio del proyectil ahora también considera la altura de la unidad.
                Vector3 startPos = new Vector3(transform.position.x, transform.position.y + unitSpawnHeightOffset * 0.5f, transform.position.z); 
                GameObject projectileGO = Instantiate(baseStats.projectilePrefab, startPos, Quaternion.identity);
                Projectile projectile = projectileGO.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(this, currentTarget, baseStats.attackDamage);
                }
            }
            else
            {
                Debug.LogWarning($"¡{baseStats.unitName} (Unidad a distancia) le falta un Projectile Prefab en sus UnitStats!");
                currentTarget.TakeDamage(baseStats.attackDamage);
            }
        }
        
        StartCoroutine(AttackCooldown());
    }

    /// <summary>
    /// Corutina para gestionar el cooldown de ataque de la unidad.
    /// </summary>
    IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        currentState = UnitActionState.IDLE;
    }

    /// <summary>
    /// Busca el mejor objetivo disponible en la escena según la prioridad establecida.
    /// </summary>
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
    /// <returns>El nodo más cercano y disponible al que moverse para atacar cuerpo a cuerpo, o null si no hay uno.</returns>
    Node FindBestMeleeAttackNode(UnitController target)
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

    /// <summary>
    /// Encuentra el mejor nodo para que una unidad a distancia se mueva dentro de su rango de ataque.
    /// Prioriza estar en rango y ser el nodo accesible más cercano sin estar demasiado cerca.
    /// </summary>
    /// <param name="target">La unidad objetivo.</param>
    /// <returns>El nodo más cercano al que moverse para estar en rango de ataque, o null si no hay uno.</returns>
    Node FindBestRangedMovementNode(UnitController target)
    {
        List<Node> validMovementNodes = new List<Node>();
        Node targetNode = gridManager.NodeFromWorldPoint(target.transform.position);
        if (targetNode == null) return null;

        int targetGridX = targetNode.gridX;
        int targetGridZ = targetNode.gridZ;

        float maxSearchDistance = baseStats.attackRange; 
        int maxGridDistance = Mathf.CeilToInt(maxSearchDistance / gridManager.TileSize) + 2; 

        for (int x = targetGridX - maxGridDistance; x <= targetGridX + maxGridDistance; x++)
        {
            for (int z = targetGridZ - maxGridDistance; z <= targetGridZ + maxGridDistance; z++)
            {
                if (x >= 0 && x < gridManager.gridWidth && z >= 0 && z < gridManager.gridHeight)
                {
                    Node potentialNode = gridManager.grid[x, z]; 
                    
                    if (potentialNode.IsAvailable())
                    {
                        float distanceToTarget = Vector3.Distance(potentialNode.worldPosition, target.transform.position);
                        
                        if (distanceToTarget <= baseStats.attackRange && distanceToTarget > gridManager.TileSize * 0.1f) 
                        {
                            if (gridManager.FindPath(transform.position, potentialNode.worldPosition) != null)
                            {
                                validMovementNodes.Add(potentialNode);
                            }
                        }
                    }
                }
            }
        }

        if (validMovementNodes.Count == 0) return null;

        return validMovementNodes.OrderBy(n => Vector3.Distance(transform.position, n.worldPosition)).FirstOrDefault();
    }


    /// <summary>
    /// Comprueba si el objetivo actual está dentro del rango de ataque de la unidad.
    /// </summary>
    /// <returns>True si el objetivo está en rango, false en caso contrario.</returns>
    bool IsInAttackRange() => currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) <= baseStats.attackRange;
    
    /// <summary>
    /// Reduce la salud de la unidad.
    /// </summary>
    /// <param name="damage">Cantidad de daño a recibir.</param>
    public void TakeDamage(int damage)
    {
        if (CurrentHealth <= 0) return;
        CurrentHealth -= damage;
        if (healthBarUI != null)
        {
            healthBarUI.SetHealthPercentage(CurrentHealth, baseStats.maxHealth);
        }
        if (CurrentHealth <= 0) { CurrentHealth = 0; Die(); }
    }

    /// <summary>
    /// Maneja la lógica cuando la unidad muere.
    /// </summary>
    public void Die() // Changed to public
    {
        GameManager.Instance.UnregisterUnit(this);
        StopAllCoroutines();
        if (currentNode != null) gridManager.ClearNode(currentNode);
        
        if (healthBarUI != null)
        {
            Destroy(healthBarUI.gameObject);
        }

        Destroy(gameObject);
    }
}
