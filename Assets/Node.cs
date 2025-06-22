using UnityEngine;

public class Node
{
    public bool isWalkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridZ;
    public int gCost;
    public int hCost;
    public Node parent;
    public bool isReserved;
    public UnitController occupyingUnit;

    // Costo total (gCost + hCost) para el algoritmo A*.
    public int fCost { get { return gCost + hCost; } }
    /// <summary>
    /// Constructor para crear una nueva instancia de Node.
    /// </summary>
    /// <param name="_isWalkable">Si el nodo es caminable.</param>
    /// <param name="_worldPos">La posición del nodo en el mundo.</param>
    /// <param name="_gridX">Coordenada X en la cuadrícula.</param>
    /// <param name="_gridZ">Coordenada Z en la cuadrícula.</param>
    public Node(bool _isWalkable, Vector3 _worldPos, int _gridX, int _gridZ)
    {
        isWalkable = _isWalkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridZ = _gridZ;
        isReserved = false;
        occupyingUnit = null;
    }
    public bool IsAvailable()
    {
        return isWalkable && occupyingUnit == null && !isReserved;
    }
}
