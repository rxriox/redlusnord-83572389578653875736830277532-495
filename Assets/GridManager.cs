using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float nodeSize = 1f;
    private Node[,] grid;

    [System.Serializable]
    public class UnitSpawnInfo
    {
        public UnitStats unitStats;
        public int teamID;
        public Vector3 spawnPoint;
    }
    public List<UnitSpawnInfo> initialUnitSpawns;

    void Awake()
    {
        CreateGrid();
        SpawnInitialUnits();
    }

    public void CreateGrid()
    {
        grid = new Node[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPosition = new Vector3(x * nodeSize, 0, z * nodeSize);
                GameObject tile = Instantiate(tilePrefab, worldPosition, Quaternion.identity, transform);
                grid[x, z] = new Node(true, worldPosition, x, z);
            }
        }
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float adjustedX = worldPosition.x + nodeSize / 2f;
        float adjustedZ = worldPosition.z + nodeSize / 2f;

        int x = Mathf.FloorToInt(adjustedX / nodeSize);
        int z = Mathf.FloorToInt(adjustedZ / nodeSize);
        if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
        {
            return grid[x, z];
        }

        return null;
    }

    public void SpawnInitialUnits()
    {
        foreach (var unitSpawn in initialUnitSpawns)
        {
            Node spawnNode = NodeFromWorldPoint(unitSpawn.spawnPoint);
            SpawnUnit(unitSpawn.unitStats, unitSpawn.teamID, spawnNode);
        }
    }

    void SpawnUnit(UnitStats stats, int team, Node node)
    {
        if (GameManager.Instance.CanPlaceUnit(team, stats) == false)
        {
            return;
        }

        if (stats != null && IsNodeValidForPlacement(node, team))
        {
            GameObject unitGO = Instantiate(stats.characterPrefab, node.worldPosition, Quaternion.identity);
            UnitController unitController = unitGO.GetComponent<UnitController>();

            if (unitController != null)
            {
                unitController.unitStats = stats;
                unitController.teamID = team;
                unitController.currentNode = node;
                node.isWalkable = false;
                GameManager.Instance.RegisterUnit(unitController);
            }
        }
        else
        {
            Debug.LogWarning($"No se pudo colocar la unidad inicial '{stats.name}' para el equipo {team} en la casilla ({node.gridX}, {node.gridZ}). ¡La zona no es válida o está ocupada!");
        }
    }

    public bool IsNodeValidForPlacement(Node node, int teamID)
    {
        if (node == null || !node.isWalkable)
        {
            return false;
        }

        if (teamID == 0)
        {
            return node.gridZ < (gridHeight / 2);
        }

        else if (teamID == 1)
        {
            return node.gridZ >= (gridHeight / 2);
        }

        return false;
    }
    /// <summary>
    /// Encuentra un camino entre dos puntos del grid usando el algoritmo A*.
    /// </summary>
    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);

        if (startNode == null || targetNode == null) return null;

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
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
                if (!neighbour.isWalkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }
        return null; // No path found
    }

    /// <summary>
    /// Obtiene los nodos vecinos de un nodo dado.
    /// </summary>
    private List<Node> GetNeighbours(Node node)
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

    /// <summary>
    /// Reconstruye el camino final desde el nodo de destino hasta el de inicio.
    /// </summary>
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

    /// <summary>
    /// Calcula la distancia heurística entre dos nodos (Manhattan).
    /// </summary>
    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);
        return 10 * (dstX + dstZ); // Costo de 10 por cada casilla ortogonal
    }
}