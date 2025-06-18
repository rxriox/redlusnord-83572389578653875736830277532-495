using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    // ... (Las variables del Header se mantienen igual) ...
    [Header("Configuración del Tablero")]
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private GameObject tilePrefab;
    private Node[,] grid;

    void Awake() { GenerateGrid(); }
    void Update() { UpdateObstacleMap(); }

    void GenerateGrid() {
        grid = new Node[gridWidth, gridHeight];
        Vector3 gridBottomLeft = transform.position - Vector3.right * gridWidth / 2f * tileSize - Vector3.forward * gridHeight / 2f * tileSize;
        for (int x = 0; x < gridWidth; x++) {
            for (int z = 0; z < gridHeight; z++) {
                Vector3 worldPoint = gridBottomLeft + Vector3.right * (x * tileSize + tileSize / 2) + Vector3.forward * (z * tileSize + tileSize / 2);
                grid[x, z] = new Node(true, worldPoint, x, z);
                if (tilePrefab != null) Instantiate(tilePrefab, worldPoint, Quaternion.identity, this.transform);
            }
        }
    }

    private void UpdateObstacleMap() {
        if (grid == null) return;
        foreach (Node node in grid) {
            node.isWalkable = true;
        }
        UnitController[] allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        foreach (UnitController unit in allUnits) {
            if (unit != null && unit.CurrentHealth > 0) {
                Node unitNode = NodeFromWorldPoint(unit.transform.position);
                if (unitNode != null) {
                    unitNode.isWalkable = false;
                }
            }
        }
    }

    // --- NUEVOS MÉTODOS DE RESERVA ---
    public void ReserveNode(Node node) {
        if (node != null) node.isReserved = true;
    }

    public void UnreserveNode(Node node) {
        if (node != null) node.isReserved = false;
    }

    // El resto del script (PlaceUnitOnTile, FindPath, helpers, OnDrawGizmos) se mantiene igual que en la versión anterior.
    // Lo incluyo todo para que no haya dudas.

    public bool PlaceUnitOnTile(GameObject unitPrefab, Transform tileTransform) {
        UpdateObstacleMap();
        Node node = NodeFromWorldPoint(tileTransform.position);
        if (node == null || !node.isWalkable) return false;
        Vector3 spawnPosition = node.worldPosition + new Vector3(0, 0.5f, 0);
        Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
        node.isWalkable = false;
        return true;
    }

    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos) {
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);
        if (startNode == null || targetNode == null) return null;
        // A* ya no necesita comprobar si el target es 'walkable', porque buscamos una casilla ADYACENTE.
        // La comprobación la hará la lógica que busca el destino.
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);
        while (openSet.Count > 0) {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++) { if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)) { currentNode = openSet[i]; } }
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            if (currentNode == targetNode) return RetracePath(startNode, targetNode);
            foreach (Node neighbour in GetNeighbours(currentNode)) {
                if (!neighbour.isWalkable || closedSet.Contains(neighbour)) continue;
                int newCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
                    neighbour.gCost = newCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                }
            }
        }
        return null;
    }
    
    public Node NodeFromWorldPoint(Vector3 worldPosition) {
        float percentX = (worldPosition.x - transform.position.x + gridWidth / 2f * tileSize) / (gridWidth * tileSize);
        float percentZ = (worldPosition.z - transform.position.z + gridHeight / 2f * tileSize) / (gridHeight * tileSize);
        percentX = Mathf.Clamp01(percentX);
        percentZ = Mathf.Clamp01(percentZ);
        int x = Mathf.Clamp(Mathf.FloorToInt(percentX * gridWidth), 0, gridWidth - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(percentZ * gridHeight), 0, gridHeight - 1);
        return grid[x, z];
    }
    public List<Node> GetNeighbours(Node node) {
        List<Node> neighbours = new List<Node>();
        for (int x = -1; x <= 1; x++) { for (int z = -1; z <= 1; z++) { if (x == 0 && z == 0) continue; int checkX = node.gridX + x; int checkZ = node.gridZ + z; if (checkX >= 0 && checkX < gridWidth && checkZ >= 0 && checkZ < gridHeight) { neighbours.Add(grid[checkX, checkZ]); } } }
        return neighbours;
    }
    private List<Node> RetracePath(Node startNode, Node endNode) {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode) { path.Add(currentNode); currentNode = currentNode.parent; }
        path.Reverse();
        return path;
    }
    private int GetDistance(Node nodeA, Node nodeB) {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);
        return (dstX > dstZ) ? 14 * dstZ + 10 * (dstX - dstZ) : 14 * dstX + 10 * (dstZ - dstX);
    }
    void OnDrawGizmos() {
        if (grid != null) { 
            foreach (Node n in grid) { 
                if (n.isReserved) Gizmos.color = new Color(1, 1, 0, 0.5f); // Amarillo para reservado
                else Gizmos.color = n.isWalkable ? Color.clear : new Color(1, 0, 0, 0.5f); // Rojo para ocupado
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (tileSize - 0.1f)); 
            } 
        } 
    }
}