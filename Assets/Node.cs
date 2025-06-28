using UnityEngine;

public class Node
{
    public bool isWalkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridZ;

    // A* Pathfinding properties
    public int gCost; // Cost from the starting node
    public int hCost; // Heuristic cost to the end node
    public Node parent; // The node preceding this one in the path

    public Node(bool _isWalkable, Vector3 _worldPosition, int _gridX, int _gridZ)
    {
        isWalkable = _isWalkable;
        worldPosition = _worldPosition;
        gridX = _gridX;
        gridZ = _gridZ;
    }

    // Calculated property for the total cost
    public int fCost
    {
        get { return gCost + hCost; }
    }
}