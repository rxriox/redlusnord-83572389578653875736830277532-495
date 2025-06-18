using UnityEngine;

public class Node
{
    public int gridX, gridZ;
    public bool isWalkable;
    public bool isReserved; // NUEVO: Para que otras unidades no elijan esta casilla como destino.
    public Vector3 worldPosition;

    public int gCost, hCost;
    public Node parent;

    public Node(bool _isWalkable, Vector3 _worldPos, int _gridX, int _gridZ)
    {
        isWalkable = _isWalkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridZ = _gridZ;
        isReserved = false; // Por defecto, ninguna casilla está reservada.
    }

    public int fCost { get { return gCost + hCost; } }
}