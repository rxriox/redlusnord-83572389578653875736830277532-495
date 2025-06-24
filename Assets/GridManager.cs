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
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        int[,] directions = { {0,1}, {0,-1}, {1,0}, {-1,0} }; // Arriba, Abajo, Derecha, Izquierda

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int checkX = node.gridX + directions[i,0];
            int checkZ = node.gridZ + directions[i,1];

            if (checkX >= 0 && checkX < gridWidth && checkZ >= 0 && checkZ < gridHeight)
            {
                neighbours.Add(grid[checkX, checkZ]);
            }
        }
        return neighbours;
    }
}