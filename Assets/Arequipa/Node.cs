using UnityEngine;

public class Node
{
    public bool isWalkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridZ;

    // A*
    public int gCost; // Costo desde inicio
    public int hCost; // Costo para el final
    public Node parent; // Nodo precedente

    public Node(bool _isWalkable, Vector3 _worldPosition, int _gridX, int _gridZ)
    {
        isWalkable = _isWalkable;
        worldPosition = _worldPosition;
        gridX = _gridX;
        gridZ = _gridZ;
    }

    // Costo total de mov calculado
    public int fCost
    {
        get { return gCost + hCost; }
    }
}