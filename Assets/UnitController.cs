using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI; // Necesario para interactuar con componentes UI como Slider o Image

/// <summary>
/// Controla el comportamiento individual de cada unidad en el autobattler,
/// incluyendo movimiento, ataque, selección de objetivo y gestión de estado.
/// </summary>
public class UnitController : MonoBehaviour
{
    // Estados de acción que una unidad puede tener.
    private enum UnitActionState { IDLE, MOVING, ATTACKING, WAITING }
    private UnitActionState currentState = UnitActionState.IDLE;
    
    [Header("Referencias y Estadísticas")]
    [Tooltip("ScriptableObject que contiene las estadísticas base de esta unidad.")]
    [SerializeField] private UnitStats baseStats;
    [Tooltip("ID del equipo al que pertenece esta unidad (0 para jugador, 1 para enemigo).")]
    public int teamID;
    [Tooltip("Prefab del UI de la barra de salud que se instanciará sobre la unidad.")]
    [SerializeField] private GameObject healthBarUIPrefab; // Nuevo campo para el prefab de la barra de salud
    [Tooltip("Offset vertical adicional para la barra de salud sobre la unidad.")]
    [SerializeField] private float healthBarVerticalOffset = 0.5f; // Nuevo: Offset ajustable en el Inspector
    
    // Salud actual de la unidad.
    public int CurrentHealth { get; private set; }
    
    // Referencia al GridManager para interactuar con la cuadrícula.
    private GridManager gridManager;

    // Referencia al controlador de la barra de salud de esta unidad.
    private HealthBarUI healthBarUI; // Referencia al nuevo script HealthBarUI

    // Objetivo actual de la unidad.
    private UnitController currentTarget;
    // Nodo de la cuadrícula en el que se encuentra actualmente la unidad.
    private Node currentNode;
    // Temporizador para el cooldown de ataque.
    private float attackCooldown;
    // Temporizador para el estado de espera (cuando la unidad no puede realizar una acción).
    private float waitingTimer;

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

        // Instanciar y configurar la barra de salud
        if (healthBarUIPrefab != null)
        {
            // Instanciar el prefab de la barra de salud como hijo de esta unidad.
            // Esto asegura que la barra de salud se mueva con la unidad.
            GameObject hbGO = Instantiate(healthBarUIPrefab, transform); 
            
            // --- CÁLCULO MEJORADO DE LA POSICIÓN VERTICAL DE LA BARRA DE SALUD ---
            // Intenta obtener los límites de TODOS los renderers hijos para determinar la altura total del modelo.
            Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);
            bool hasRenderer = false;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
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

            float highestPointY = hasRenderer ? combinedBounds.max.y : transform.position.y + 1.8f; // Fallback a 1.8f si no hay renderer.

            // Convertir la posición global más alta a local para la barra de salud.
            hbGO.transform.position = new Vector3(transform.position.x, highestPointY + healthBarVerticalOffset, transform.position.z);
            hbGO.transform.SetParent(transform); // Asegura que la barra de salud siga siendo hija.
            // --- FIN CÁLCULO MEJORADO ---

            healthBarUI = hbGO.GetComponent<HealthBarUI>();
            if (healthBarUI != null)
            {
                // Asignar color según el equipo (verde para aliados, rojo para enemigos).
                healthBarUI.SetColor(teamID == 0 ? Color.green : Color.red);
                // Establecer la salud inicial de la barra de salud.
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
        // Si la unidad ya está moviéndose o atacando, no hacer nada más por ahora.
        if (currentState == UnitActionState.MOVING || currentState == UnitActionState.ATTACKING) return;
        
        // Decrementa el temporizador de cooldown si está activo.
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }

        // Si la unidad está en estado de espera, decrementar el temporizador de espera.
        // Una vez que el temporizador llega a cero, vuelve a IDLE para reevaluar acciones.
        if (currentState == UnitActionState.WAITING)
        {
            waitingTimer -= Time.deltaTime;
            if (waitingTimer <= 0)
            {
                currentState = UnitActionState.IDLE;
            }
            return; // No hace más acciones mientras espera.
        }
        
        // Si no hay objetivo válido o el objetivo actual está muerto, busca uno nuevo.
        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            FindBestTarget();
        }

        // Si se ha encontrado un objetivo válido.
        if (currentTarget != null)
        {
            // Si la unidad está dentro de su rango de ataque.
            if (IsInAttackRange())
            {
                // Y si su cooldown de ataque ha terminado, ataca.
                if (attackCooldown <= 0) Attack();
            }
            else
            {
                // Si no está en rango, se mueve hacia el objetivo.
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

        // Determina la lógica de movimiento según el tipo de unidad.
        if (baseStats.unitType == UnitStats.UnitType.Melee)
        {
            destinationNode = FindBestMeleeAttackNode(currentTarget);
        }
        else if (baseStats.unitType == UnitStats.UnitType.Ranged)
        {
            destinationNode = FindBestRangedMovementNode(currentTarget);
        }
        
        // Si se encontró un nodo de destino válido y no es el nodo actual.
        if (destinationNode != null && destinationNode != currentNode)
        {
            // Intenta encontrar un camino al nodo de destino.
            List<Node> path = gridManager.FindPath(transform.position, destinationNode.worldPosition);
            if (path != null && path.Count > 0)
            {
                // Si hay un camino, inicia la animación de movimiento al primer paso del camino.
                StartCoroutine(MoveAnimation(path[0]));
            }
            else
            {
                // Si el camino está bloqueado o no existe, entra en estado de espera.
                currentState = UnitActionState.WAITING;
                waitingTimer = 0.5f; 
            }
        }
        else
        {
            // Si no hay un nodo de destino válido (no se puede acercar más o ya está en posición), entra en estado de espera.
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
        currentState = UnitActionState.MOVING; // Establece el estado a MOVING.

        // Reserva el nodo de destino para que otras unidades no lo intenten ocupar.
        gridManager.ReserveNode(targetNode);

        Vector3 startPos = transform.position;
        Vector3 endPos = targetNode.worldPosition;
        // Orienta la unidad para que mire hacia su punto de destino.
        transform.LookAt(new Vector3(endPos.x, transform.position.y, endPos.z));
        
        // Calcula el tiempo que tomará moverse en base a la velocidad de movimiento de la unidad.
        float timeToMove = 1f / baseStats.moveSpeed;
        float elapsedTime = 0f;
        
        // Animación de movimiento suave (interpolación lineal).
        while(elapsedTime < timeToMove)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / timeToMove);
            elapsedTime += Time.deltaTime;
            yield return null; // Espera al siguiente frame.
        }
        
        // Asegura que la unidad termine exactamente en la posición del nodo.
        transform.position = endPos;
        gridManager.ClearNode(currentNode); // Libera el nodo anterior.
        currentNode = targetNode; // Actualiza el nodo actual de la unidad.
        gridManager.SetUnitOnNode(this, currentNode); // Ocupa el nuevo nodo.
        
        gridManager.UnreserveNode(targetNode); // Libera la reserva del nodo de destino.
        currentState = UnitActionState.IDLE; // Vuelve al estado IDLE.
    }

    /// <summary>
    /// Ejecuta el ataque de la unidad, aplicando daño directo o lanzando un proyectil.
    /// </summary>
    void Attack()
    {
        currentState = UnitActionState.ATTACKING; // Establece el estado a ATTACKING.
        attackCooldown = 1f / baseStats.attackSpeed; // Reinicia el cooldown de ataque.
        
        // Orienta la unidad para que mire a su objetivo.
        transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));

        if (baseStats.unitType == UnitStats.UnitType.Melee)
        {
            // Para unidades cuerpo a cuerpo, aplica daño directamente al objetivo.
            currentTarget.TakeDamage(baseStats.attackDamage);
        }
        else if (baseStats.unitType == UnitStats.UnitType.Ranged)
        {
            // Para unidades a distancia, instancia un proyectil si tiene uno asignado.
            if (baseStats.projectilePrefab != null)
            {
                // Instancia el proyectil ligeramente por encima de la unidad para evitar colisiones iniciales.
                Vector3 startPos = transform.position + Vector3.up * 0.5f; 
                GameObject projectileGO = Instantiate(baseStats.projectilePrefab, startPos, Quaternion.identity);
                Projectile projectile = projectileGO.GetComponent<Projectile>();
                if (projectile != null)
                {
                    // Inicializa el proyectil con la información necesaria.
                    projectile.Initialize(this, currentTarget, baseStats.attackDamage);
                }
            }
            else
            {
                Debug.LogWarning($"¡{baseStats.unitName} (Unidad a distancia) le falta un Projectile Prefab en sus UnitStats!");
                // Como fallback, si no hay proyectil, aplica el daño directamente.
                currentTarget.TakeDamage(baseStats.attackDamage);
            }
        }
        
        StartCoroutine(AttackCooldown()); // Inicia la corutina del cooldown de ataque.
    }

    /// <summary>
    /// Corutina para gestionar el cooldown de ataque de la unidad.
    /// </summary>
    IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown); // Espera la duración del cooldown.
        currentState = UnitActionState.IDLE; // Vuelve al estado IDLE.
    }

    /// <summary>
    /// Busca el mejor objetivo disponible en la escena según la prioridad establecida.
    /// </summary>
    void FindBestTarget()
    {
        // Obtiene todas las unidades activas que pertenecen al equipo enemigo.
        IQueryable<UnitController> activeEnemies = FindObjectsByType<UnitController>(FindObjectsSortMode.None)
            .Where(u => u.teamID != this.teamID && u.CurrentHealth > 0)
            .AsQueryable();

        // Ordena los enemigos según la prioridad de objetivo (más cercano o con menos salud).
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
    /// Una unidad cuerpo a cuerpo necesita estar en un nodo adyacente a su objetivo.
    /// </summary>
    /// <param name="target">La unidad objetivo.</param>
    /// <returns>El nodo más cercano y disponible al que moverse para atacar cuerpo a cuerpo, o null si no hay uno.</returns>
    Node FindBestMeleeAttackNode(UnitController target)
    {
        List<Node> reachableNodes = new List<Node>();
        Node targetNode = gridManager.NodeFromWorldPoint(target.transform.position);
        if (targetNode == null) return null;

        // Itera sobre los 8 vecinos del nodo objetivo.
        foreach (var neighbour in gridManager.GetNeighbours(targetNode))
        {
            // Verifica si el vecino es un nodo disponible (caminable, no ocupado, no reservado).
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
        
        // Ordena los nodos alcanzables por la distancia de la unidad a ellos (para priorizar el movimiento mínimo).
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

        // Calcular los límites de la cuadrícula para la búsqueda alrededor del objetivo.
        int targetGridX = targetNode.gridX;
        int targetGridZ = targetNode.gridZ;

        // Determinar un radio de búsqueda en tiles que sea al menos el rango de ataque.
        // Se añade un margen de +2 para asegurar la exploración de nodos que bordean el rango.
        float maxSearchDistance = baseStats.attackRange; 
        int maxGridDistance = Mathf.CeilToInt(maxSearchDistance / gridManager.TileSize) + 2; 

        // Iterar sobre una sección de la cuadrícula alrededor del objetivo.
        for (int x = targetGridX - maxGridDistance; x <= targetGridX + maxGridDistance; x++)
        {
            for (int z = targetGridZ - maxGridDistance; z <= targetGridZ + maxGridDistance; z++)
            {
                // Asegurarse de que el nodo está dentro de los límites de la cuadrícula.
                if (x >= 0 && x < gridManager.gridWidth && z >= 0 && z < gridManager.gridHeight)
                {
                    Node potentialNode = gridManager.grid[x, z]; // Acceso directo al array de la cuadrícula.
                    
                    // Si el nodo está disponible (caminable, no ocupado, no reservado).
                    if (potentialNode.IsAvailable())
                    {
                        float distanceToTarget = Vector3.Distance(potentialNode.worldPosition, target.transform.position);
                        
                        // Comprobar si este nodo coloca al objetivo dentro del rango de ataque
                        // Y si no es demasiado cercano (para evitar que las unidades a distancia se pongan como melee).
                        // Se usa un pequeño margen de 0.1f para que no se peguen exactamente a 0.
                        if (distanceToTarget <= baseStats.attackRange && distanceToTarget > gridManager.TileSize * 0.1f) 
                        {
                            // Asegurarse de que haya un camino a este nodo desde la posición actual de la unidad.
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

        // Ordena los nodos válidos por distancia a la unidad actual (el más cercano primero)
        // y selecciona el primero. Esto minimiza el movimiento innecesario.
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
        if (CurrentHealth <= 0) return; // Si ya está muerto, no hace nada.
        CurrentHealth -= damage;
        // Actualiza la barra de salud cada vez que la unidad recibe daño.
        if (healthBarUI != null)
        {
            healthBarUI.SetHealthPercentage(CurrentHealth, baseStats.maxHealth);
        }
        if (CurrentHealth <= 0) { CurrentHealth = 0; Die(); } // Si la salud cae a 0 o menos, la unidad muere.
    }

    /// <summary>
    /// Maneja la lógica cuando la unidad muere.
    /// </summary>
    void Die()
    {
        GameManager.Instance.UnregisterUnit(this); // Desregistra la unidad del GameManager.
        StopAllCoroutines(); // Detiene cualquier corutina de movimiento o ataque.
        if (currentNode != null) gridManager.ClearNode(currentNode); // Libera el nodo de la cuadrícula.
        
        // Destruye la barra de salud asociada antes de destruir la unidad.
        if (healthBarUI != null)
        {
            Destroy(healthBarUI.gameObject);
        }

        Destroy(gameObject); // Destruye el GameObject de la unidad en la escena.
    }
}
