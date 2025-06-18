using UnityEngine;

public class Node
{
    public bool isWalkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridZ;

    // --- Variables para A* ---
    public int gCost;
    public int hCost;
    public Node parent;

    // --- NUEVAS VARIABLES ---
    public bool isReserved; // True si una unidad ESTÁ YENDO hacia este nodo
    public UnitController occupyingUnit; // La unidad que está PARADA en este nodo

    public int fCost { get { return gCost + hCost; } }

    public Node(bool _isWalkable, Vector3 _worldPos, int _gridX, int _gridZ)
    {
        isWalkable = _isWalkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridZ = _gridZ;
        isReserved = false;
        occupyingUnit = null;
    }

    // Un nodo está "disponible" si se puede caminar sobre él Y no hay una unidad parada Y no está reservado.
    public bool IsAvailable()
    {
        return isWalkable && occupyingUnit == null && !isReserved;
    }
}