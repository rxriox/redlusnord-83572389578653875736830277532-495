using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gestiona la creación de la cuadrícula, el seguimiento de nodos y la lógica de pathfinding (A*).
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Configuración del Tablero")]
    [Tooltip("Ancho de la cuadrícula en número de celdas.")]
    [SerializeField] public int gridWidth = 8; // Ancho de la cuadrícula, accesible públicamente.
    [Tooltip("Altura de la cuadrícula en número de celdas.")]
    [SerializeField] public int gridHeight = 8; // Altura de la cuadrícula, accesible públicamente.
    [Tooltip("Tamaño de cada celda de la cuadrícula en unidades de Unity.")]
    [SerializeField] private float tileSize = 1.0f;
    [Tooltip("Prefab del objeto de celda (Tile) para visualizar la cuadrícula.")]
    [SerializeField] private GameObject tilePrefab;

    // Array 2D que almacena todos los nodos de la cuadrícula.
    public Node[,] grid; // La cuadrícula completa, accesible públicamente.
    
    // Propiedad para obtener el tamaño de la celda.
    public float TileSize => tileSize;

    void Awake()
    {
        GenerateGrid();
    }

    /// <summary>
    /// Genera la cuadrícula de nodos en el inicio del juego.
    /// </summary>
    void GenerateGrid()
    {
        grid = new Node[gridWidth, gridHeight];
        // Calcula la posición de la esquina inferior izquierda de la cuadrícula para centrarla.
        Vector3 gridBottomLeft = transform.position - Vector3.right * gridWidth / 2f * tileSize - Vector3.forward * gridHeight / 2f * tileSize;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // Calcula la posición del mundo para el centro de cada celda.
                Vector3 worldPoint = gridBottomLeft + Vector3.right * (x * tileSize + tileSize / 2) + Vector3.forward * (z * tileSize + tileSize / 2);
                grid[x, z] = new Node(true, worldPoint, x, z); // Crea un nuevo nodo.
                // Instancia un prefab de celda si está asignado para visualización.
                if (tilePrefab != null) Instantiate(tilePrefab, worldPoint, Quaternion.identity, this.transform);
            }
        }
    }
    
    /// <summary>
    /// Marca un nodo como reservado, útil para la planificación de movimiento de unidades.
    /// </summary>
    /// <param name="node">El nodo a reservar.</param>
    public void ReserveNode(Node node)
    {
        if (node != null) node.isReserved = true;
    }

    /// <summary>
    /// Libera la reserva de un nodo.
    /// </summary>
    /// <param name="node">El nodo a liberar.</param>
    public void UnreserveNode(Node node)
    {
        if (node != null) node.isReserved = false;
    }

    /// <summary>
    /// Intenta colocar una nueva unidad en un nodo de la cuadrícula.
    /// </summary>
    /// <param name="unitPrefab">El prefab de la unidad a colocar.</param>
    /// <param name="tileTransform">La transformación del tile donde se intentará colocar la unidad.</param>
    /// <returns>True si la unidad se colocó con éxito, false en caso contrario.</returns>
    public bool PlaceUnitOnTile(GameObject unitPrefab, Transform tileTransform)
    {
        Node node = NodeFromWorldPoint(tileTransform.position);
        if (node == null || !node.IsAvailable()) return false; // El nodo no es válido o no está disponible.

        Vector3 spawnPosition = node.worldPosition;
        GameObject newUnitGO = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);

        UnitController newUnitController = newUnitGO.GetComponent<UnitController>();
        if (newUnitController != null)
        {
            newUnitController.Initialize(this, node); // Inicializa la unidad con el GridManager y el nodo.
            return true;
        }
        else
        {
            Destroy(newUnitGO); // Si no tiene UnitController, destruye el objeto.
            return false;
        }
    }

    /// <summary>
    /// Asigna una unidad a un nodo específico, marcándolo como ocupado.
    /// </summary>
    /// <param name="unit">La unidad que ocupará el nodo.</param>
    /// <param name="node">El nodo a ocupar.</param>
    public void SetUnitOnNode(UnitController unit, Node node)
    {
        if (node != null) node.occupyingUnit = unit;
    }

    /// <summary>
    /// Libera un nodo de la unidad que lo ocupa.
    /// </summary>
    /// <param name="node">El nodo a liberar.</param>
    public void ClearNode(Node node)
    {
        if (node != null) node.occupyingUnit = null;
    }

    /// <summary>
    /// Implementación del algoritmo A* para encontrar un camino entre dos puntos en la cuadrícula.
    /// </summary>
    /// <param name="startPos">Posición de inicio en el mundo.</param>
    /// <param name="targetPos">Posición objetivo en el mundo.</param>
    /// <returns>Una lista de Nodos que forman el camino, o null si no se encuentra un camino.</returns>
    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);
        
        if (startNode == null || targetNode == null) return null; // No se puede encontrar el camino si los nodos no existen.

        // Reinicia los costes y padres de todos los nodos para una nueva búsqueda.
        foreach (var node in grid)
        {
            node.gCost = int.MaxValue;
            node.parent = null;
        }

        List<Node> openSet = new List<Node>(); // Nodos a evaluar.
        HashSet<Node> closedSet = new HashSet<Node>(); // Nodos ya evaluados.

        startNode.gCost = 0; // Costo G del nodo inicial es 0.
        startNode.hCost = GetDistance(startNode, targetNode); // Calcula el costo H (heurístico).
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // Encuentra el nodo con el menor costo F en el openSet.
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++) 
            { 
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)) 
                { 
                    currentNode = openSet[i]; 
                } 
            }
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode) return RetracePath(startNode, targetNode); // Se encontró el camino.

            // Evalúa los vecinos del nodo actual.
            foreach (Node neighbour in GetNeighbours(currentNode))
            {
                bool isDestinationNode = (neighbour == targetNode);
                // Un vecino es un obstáculo si:
                // - No es caminable.
                // - Ya ha sido evaluado (está en closedSet).
                // - Está ocupado por otra unidad (y no es el objetivo final, ya que el objetivo puede estar ocupado).
                // - Está reservado por otra unidad que se está moviendo hacia él (y no es el objetivo final).
                if (!neighbour.isWalkable || closedSet.Contains(neighbour) || (neighbour.occupyingUnit != null && !isDestinationNode) || (neighbour.isReserved && !isDestinationNode))
                {
                    continue; // Ignora este vecino.
                }

                // Calcula el nuevo costo G para el vecino.
                int newCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newCostToNeighbour < neighbour.gCost)
                {
                    neighbour.gCost = newCostToNeighbour; // Actualiza el costo G.
                    neighbour.hCost = GetDistance(neighbour, targetNode); // Actualiza el costo H.
                    neighbour.parent = currentNode; // Establece el nodo actual como padre del vecino.
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour); // Añade el vecino al openSet si no está ya.
                }
            }
        }
        return null; // No se encontró un camino.
    }

    /// <summary>
    /// Convierte una posición en el mundo a su nodo de cuadrícula correspondiente.
    /// </summary>
    /// <param name="worldPosition">La posición en el mundo.</param>
    /// <returns>El nodo de la cuadrícula en esa posición, o null si la cuadrícula no existe.</returns>
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        if (grid == null || grid.Length == 0) return null;
        // Calcula las coordenadas relativas dentro de la cuadrícula (0 a 1).
        float percentX = (worldPosition.x - transform.position.x + gridWidth * tileSize / 2) / (gridWidth * tileSize);
        float percentZ = (worldPosition.z - transform.position.z + gridHeight * tileSize / 2) / (gridHeight * tileSize);
        
        // Clampa los valores para asegurarse de que estén dentro de los límites [0, 1].
        percentX = Mathf.Clamp01(percentX);
        percentZ = Mathf.Clamp01(percentZ);
        
        // Convierte las coordenadas relativas a índices de cuadrícula.
        int x = Mathf.Clamp(Mathf.FloorToInt(percentX * gridWidth), 0, gridWidth - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(percentZ * gridHeight), 0, gridHeight - 1);
        
        return grid[x, z];
    }

    /// <summary>
    /// Obtiene todos los nodos vecinos (incluyendo diagonales) de un nodo dado.
    /// </summary>
    /// <param name="node">El nodo central.</param>
    /// <returns>Una lista de nodos vecinos.</returns>
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        for (int x = -1; x <= 1; x++) 
        { 
            for (int z = -1; z <= 1; z++) 
            { 
                if (x == 0 && z == 0) continue; // Excluye el propio nodo central.
                
                int checkX = node.gridX + x; 
                int checkZ = node.gridZ + z; 
                
                // Asegura que el vecino esté dentro de los límites de la cuadrícula.
                if (checkX >= 0 && checkX < gridWidth && checkZ >= 0 && checkZ < gridHeight) 
                { 
                    neighbours.Add(grid[checkX, checkZ]); 
                } 
            } 
        }
        return neighbours;
    }

    /// <summary>
    /// Reconstruye el camino desde el nodo final hasta el nodo inicial usando los padres.
    /// </summary>
    /// <param name="startNode">El nodo inicial del camino.</param>
    /// <param name="endNode">El nodo final del camino.</param>
    /// <returns>Una lista de nodos que representan el camino desde el inicio al fin.</returns>
    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode) 
        { 
            path.Add(currentNode); 
            currentNode = currentNode.parent; 
        }
        path.Reverse(); // Invierte la lista para que el camino sea del inicio al fin.
        return path;
    }
    
    /// <summary>
    /// Calcula la distancia entre dos nodos usando una heurística simplificada (distancia de Manhattan para diagonales).
    /// </summary>
    /// <param name="nodeA">Primer nodo.</param>
    /// <param name="nodeB">Segundo nodo.</param>
    /// <returns>La distancia calculada entre los nodos.</returns>
    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);
        // Costo para movimientos diagonales (14) y movimientos ortogonales (10).
        return (dstX > dstZ) ? 14 * dstZ + 10 * (dstX - dstZ) : 14 * dstX + 10 * (dstZ - dstX);
    }
}
