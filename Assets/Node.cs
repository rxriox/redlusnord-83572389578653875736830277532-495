using UnityEngine;

/// <summary>
/// Representa una celda individual en la cuadrícula del juego.
/// Contiene información sobre transitabilidad, posición y estado de ocupación.
/// </summary>
public class Node
{
    // Indica si el nodo se puede caminar (no es un obstáculo).
    public bool isWalkable;
    // Posición del nodo en el espacio del mundo de Unity.
    public Vector3 worldPosition;
    // Coordenadas del nodo en la cuadrícula (X).
    public int gridX;
    // Coordenadas del nodo en la cuadrícula (Z).
    public int gridZ;

    // --- Variables para el algoritmo A* ---
    // Costo desde el nodo inicial hasta este nodo.
    public int gCost;
    // Costo heurístico (distancia estimada) desde este nodo hasta el nodo objetivo.
    public int hCost;
    // El nodo anterior en el camino más corto conocido.
    public Node parent;

    // --- Variables de estado de ocupación y reserva ---
    // True si una unidad tiene la intención de moverse a este nodo (útil para evitar que dos unidades apunten al mismo).
    public bool isReserved; 
    // Referencia a la unidad que está actualmente parada en este nodo.
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

    /// <summary>
    /// Comprueba si este nodo está disponible para ser ocupado o movido a.
    /// Un nodo está disponible si es caminable, no hay una unidad ocupándolo actualmente y no está reservado.
    /// </summary>
    /// <returns>True si el nodo está disponible, false en caso contrario.</returns>
    public bool IsAvailable()
    {
        return isWalkable && occupyingUnit == null && !isReserved;
    }
}
