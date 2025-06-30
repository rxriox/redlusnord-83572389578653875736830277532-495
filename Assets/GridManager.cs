using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float nodeSize = 1f;
    public Node[,] grid;

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

    public void SpawnUnit(UnitStats stats, int team, Node node, UnitIconController originatingIcon = null)
    {
        if (stats != null && node != null && node.isWalkable)
        {
            GameObject unitGO = Instantiate(stats.characterPrefab, node.worldPosition, Quaternion.identity, this.transform);
            UnitController unitController = unitGO.GetComponent<UnitController>();

            if (unitController != null)
            {
                unitController.unitStats = stats;
                unitController.teamID = team;
                unitController.currentNode = node;
                unitController.originatingIcon = originatingIcon;
                node.isWalkable = false;
                GameManager.Instance.RegisterUnit(unitController);
                if (originatingIcon != null)
                {
                    originatingIcon.SetAsPlaced();
                }
            }
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
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0)
                    continue;

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
    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);
        if (dstX > dstZ)
            return 14 * dstZ + 10 * (dstX - dstZ);

        return 14 * dstX + 10 * (dstZ - dstX);
    }
    public List<Node> FindPath(Node startNode, Node targetNode)
{
    UnitController targetUnit = GameManager.Instance.GetUnitAtNode(targetNode);

    List<Node> openSet = new List<Node>();
    HashSet<Node> closedSet = new HashSet<Node>();
    openSet.Add(startNode);

    while (openSet.Count > 0)
    {
        Node currentNode = openSet[0];
        for (int i = 1; i < openSet.Count; i++)
        {
            if (openSet[i].fCost < currentNode.fCost || 
                (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
            {
                currentNode = openSet[i];
            }
        }

        openSet.Remove(currentNode);
        closedSet.Add(currentNode);

        if (currentNode == targetNode)
        {
            List<Node> path = RetracePath(startNode, targetNode);
            if (path.Count > 0)
            {
                path.RemoveAt(path.Count - 1);
            }
            return path;
        }

        foreach (Node neighbour in GetNeighbours(currentNode))
        {
            UnitController occupant = GameManager.Instance.GetUnitAtNode(neighbour);

            if (closedSet.Contains(neighbour) || !neighbour.isWalkable || occupant != null)
            {
                if (neighbour == targetNode)
                {
                    // Es el destino, podemos pathfind hacia él.
                }
                else
                {
                    continue;
                }
            }

            int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
            if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
            {
                neighbour.gCost = newMovementCostToNeighbour;
                neighbour.hCost = GetDistance(neighbour, targetNode);
                neighbour.parent = currentNode;

                if (!openSet.Contains(neighbour))
                    openSet.Add(neighbour);
                else
                    openSet.Sort((a, b) => a.fCost.CompareTo(b.fCost));
            }
        }
    }

    return null;
}
    
    public Node FindClosestValidNode(Vector3 worldPosition, int teamID)
{
    Node closestNode = null;
    float minDistance = float.MaxValue;

    foreach (Node node in grid)
    {
        if (IsNodeValidForPlacement(node, teamID))
        {
            float distance = Vector3.Distance(node.worldPosition, worldPosition);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestNode = node;
            }
        }
    }

    return closestNode;
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
}