using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Configuración del Tablero")]
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private GameObject tilePrefab;

    private Node[,] grid;
    public int GridWidth { get { return gridWidth; } }
    public int GridHeight { get { return gridHeight; } }


    void Awake()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        grid = new Node[gridWidth, gridHeight];
        Vector3 gridBottomLeft = transform.position - Vector3.right * gridWidth / 2f * tileSize - Vector3.forward * gridHeight / 2f * tileSize;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPoint = gridBottomLeft + Vector3.right * (x * tileSize + tileSize / 2) + Vector3.forward * (z * tileSize + tileSize / 2);
                grid[x, z] = new Node(true, worldPoint, x, z);
                if (tilePrefab != null) Instantiate(tilePrefab, worldPoint, Quaternion.identity, this.transform);
            }
        }
    }

    // --- MÉTODO RESTAURADO Y ACTUALIZADO ---
    /// <summary>
    /// Coloca una unidad en el tablero durante la fase de preparación.
    /// </summary>
    /// <param name="unitPrefab">El prefab de la unidad a instanciar.</param>
    /// <param name="tileTransform">El transform del tile donde se quiere colocar.</param>
    /// <returns>True si la colocación fue exitosa, false si no.</returns>
    public bool PlaceUnitOnTile(GameObject unitPrefab, Transform tileTransform)
    {
        Node node = NodeFromWorldPoint(tileTransform.position);

        // La colocación falla si el nodo no existe o si ya hay una unidad en él.
        if (node == null || node.occupyingUnit != null)
        {
            Debug.Log("No se puede colocar: El nodo no es válido o ya está ocupado.");
            return false;
        }

        // Instanciamos la unidad en la posición del nodo.
        // Es importante que el pívot del prefab esté en la base (los pies).
        Vector3 spawnPosition = node.worldPosition; 
        GameObject newUnitGO = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);

        // Le decimos al nodo que ahora está ocupado por esta nueva unidad.
        UnitController newUnitController = newUnitGO.GetComponent<UnitController>();
        if (newUnitController != null)
        {
            SetUnitOnNode(newUnitController, node);
            Debug.Log($"Unidad {newUnitGO.name} colocada en el nodo ({node.gridX}, {node.gridZ})");
            return true;
        }
        else
        {
            // Si el prefab no tiene UnitController, algo está mal. Destruimos la instancia y fallamos.
            Debug.LogError("El prefab de la unidad no tiene un componente UnitController.");
            Destroy(newUnitGO);
            return false;
        }
    }


    // --- MÉTODOS PARA GESTIONAR EL ESTADO DE LOS NODOS ---

    public void SetUnitOnNode(UnitController unit, Node node)
    {
        if (node != null)
        {
            node.occupyingUnit = unit;
        }
    }

    public void ClearNode(Node node)
    {
        if (node != null)
        {
            node.occupyingUnit = null;
        }
    }

    public void ReserveNode(Node node)
    {
        if (node != null) node.isReserved = true;
    }

    public void UnreserveNode(Node node)
    {
        if (node != null) node.isReserved = false;
    }

    // --- LÓGICA DE PATHFINDING (A*) MEJORADA ---

    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);

        if (startNode == null || targetNode == null)
        {
             return null;
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
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

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbour in GetNeighbours(currentNode))
            {
                bool isDestinationNode = (neighbour == targetNode);
                
                // Un vecino no es válido si...
                if (!neighbour.isWalkable || closedSet.Contains(neighbour)) continue; // Es un obstáculo o ya lo evaluamos.
                if (neighbour.occupyingUnit != null) continue; // Hay una unidad PARADA en él.
                if (neighbour.isReserved && !isDestinationNode) continue; // Está reservado por OTRA unidad.

                int newCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }
        return null; // No se encontró camino
    }
    
    // --- Métodos de Ayuda (sin cambios) ---
    
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        if (grid == null || grid.Length == 0) return null;
        float percentX = (worldPosition.x - transform.position.x + gridWidth * tileSize / 2) / (gridWidth * tileSize);
        float percentZ = (worldPosition.z - transform.position.z + gridHeight * tileSize / 2) / (gridHeight * tileSize);
        percentX = Mathf.Clamp01(percentX);
        percentZ = Mathf.Clamp01(percentZ);
        int x = Mathf.Clamp(Mathf.FloorToInt(percentX * gridWidth), 0, gridWidth - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(percentZ * gridHeight), 0, gridHeight - 1);
        return grid[x, z];
    }

    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue;
                int checkX = node.gridX + x;
                int checkZ = node.gridZ + z;
                if (checkX >= 0 && checkX < gridWidth && checkZ >= 0 && checkZ < gridHeight)
                {
                    neighbours.Add(grid[checkX, checkZ]);
                }
            }
        }
        return neighbours;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);
        return (dstX > dstZ) ? 14 * dstZ + 10 * (dstX - dstZ) : 14 * dstX + 10 * (dstZ - dstX);
    }

    void OnDrawGizmos()
    {
        if (grid == null) return;
        foreach (Node n in grid)
        {
            if (n.occupyingUnit != null) Gizmos.color = new Color(1, 0, 0, 0.5f);
            else if (n.isReserved) Gizmos.color = new Color(1, 1, 0, 0.5f);
            else Gizmos.color = Color.clear;
            
            Gizmos.DrawCube(n.worldPosition, Vector3.one * (tileSize - 0.1f));
        }
    }
}