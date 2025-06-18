using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GridManager : MonoBehaviour
{
    [Header("Configuración del Tablero")]
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private GameObject tilePrefab;

    private Node[,] grid;
    
    public float TileSize => tileSize;

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
    
    // --- MÉTODOS RESTAURADOS PARA LA IA AVANZADA ---
    public void ReserveNode(Node node)
    {
        if (node != null) node.isReserved = true;
    }

    public void UnreserveNode(Node node)
    {
        if (node != null) node.isReserved = false;
    }


    public bool PlaceUnitOnTile(GameObject unitPrefab, Transform tileTransform)
    {
        Node node = NodeFromWorldPoint(tileTransform.position);
        if (node == null || !node.IsAvailable()) return false;

        Vector3 spawnPosition = node.worldPosition;
        GameObject newUnitGO = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);

        UnitController newUnitController = newUnitGO.GetComponent<UnitController>();
        if (newUnitController != null)
        {
            newUnitController.Initialize(this, node);
            return true;
        }
        else
        {
            Destroy(newUnitGO);
            return false;
        }
    }

    public void SetUnitOnNode(UnitController unit, Node node)
    {
        if (node != null) node.occupyingUnit = unit;
    }

    public void ClearNode(Node node)
    {
        if (node != null) node.occupyingUnit = null;
    }

    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);
        if (startNode == null || targetNode == null) return null;

        // Limpiar los costes de los nodos antes de cada búsqueda para asegurar rutas correctas
        foreach (var node in grid)
        {
            node.gCost = int.MaxValue;
            node.parent = null;
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++) { if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)) { currentNode = openSet[i]; } }
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode) return RetracePath(startNode, targetNode);

            foreach (Node neighbour in GetNeighbours(currentNode))
            {
                bool isDestinationNode = (neighbour == targetNode);
                // Un nodo es un obstáculo si no es caminable, ya lo hemos visitado,
                // hay una unidad en él (y no es nuestro destino final), o está reservado (y no es nuestro destino)
                if (!neighbour.isWalkable || closedSet.Contains(neighbour) || (neighbour.occupyingUnit != null && !isDestinationNode) || (neighbour.isReserved && !isDestinationNode))
                {
                    continue;
                }

                int newCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newCostToNeighbour < neighbour.gCost)
                {
                    neighbour.gCost = newCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                }
            }
        }
        return null;
    }

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
        for (int x = -1; x <= 1; x++) { for (int z = -1; z <= 1; z++) { if (x == 0 && z == 0) continue; int checkX = node.gridX + x; int checkZ = node.gridZ + z; if (checkX >= 0 && checkX < gridWidth && checkZ >= 0 && checkZ < gridHeight) { neighbours.Add(grid[checkX, checkZ]); } } }
        return neighbours;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode) { path.Add(currentNode); currentNode = currentNode.parent; }
        path.Reverse();
        return path;
    }
    
    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);
        return (dstX > dstZ) ? 14 * dstZ + 10 * (dstX - dstZ) : 14 * dstX + 10 * (dstZ - dstX);
    }
}