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
        // --- INICIO DE LA CORRECCIÓN ---
        // Para compensar el hecho de que el pivote de nuestras casillas (Planes) está
        // en el centro, desplazamos el punto de referencia del cálculo sumando
        // la mitad del tamaño de un nodo. Esto alinea la cuadrícula lógica con la física.
        float adjustedX = worldPosition.x + nodeSize / 2f;
        float adjustedZ = worldPosition.z + nodeSize / 2f;

        int x = Mathf.FloorToInt(adjustedX / nodeSize);
        int z = Mathf.FloorToInt(adjustedZ / nodeSize);
        // --- FIN DE LA CORRECCIÓN ---

        // El resto de la lógica de comprobación de límites es la misma.
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
        // Pasamos tanto el equipo como las estadísticas del personaje a la función de comprobación.
        if (GameManager.Instance.CanPlaceUnit(team, stats) == false)
        {
            // El GameManager ya se encarga de mostrar un warning específico en la consola.
            return;
        }
        
        // El resto de la función es la misma, se comprueba si la zona es válida.
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
    
    /// <summary>
    /// Comprueba si una casilla es válida para que un equipo coloque una unidad.
    /// </summary>
    /// <param name="node">La casilla a comprobar.</param>
    /// <param name="teamID">El equipo que intenta colocar (0=aliado, 1=enemigo).</param>
    /// <returns>True si la colocación es válida.</returns>
    public bool IsNodeValidForPlacement(Node node, int teamID)
    {
        // Primero, la casilla debe existir y estar libre.
        if (node == null || !node.isWalkable)
        {
            return false;
        }

        // Ahora, aplicamos las reglas de zona:
        // El equipo 0 (jugador) solo puede colocar en la mitad inferior del tablero.
        if (teamID == 0)
        {
            // `gridZ` va de 0 a `gridHeight - 1`. La mitad es `gridHeight / 2`.
            return node.gridZ < (gridHeight / 2);
        }
        // El equipo 1 (enemigo) solo puede colocar en la mitad superior.
        else if (teamID == 1)
        {
            return node.gridZ >= (gridHeight / 2);
        }
        
        // Si por alguna razón el teamID no es 0 ni 1, no se permite la colocación.
        return false;
    }
}