using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Este script ahora solo inicializa los controles del jugador.
/// La lógica de colocar unidades ha sido movida a DraggableIcon.cs
/// </summary>
public class PlayerController : MonoBehaviour
{
    // Ya no necesitamos una referencia al GridManager ni a la LayerMask aquí.

    private PlayerControl playerControls;

    void Awake()
    {
        // Simplemente inicializamos el sistema de input.
        playerControls = new PlayerControl();
    }

    private void OnEnable()
    {
        // Habilitamos el mapa de acciones "Gameplay".
        playerControls.Gameplay.Enable();
        
        // --- LÍNEAS ELIMINADAS ---
        // Ya NO nos suscribimos al evento de clic aquí, porque esa acción
        // ahora la gestiona el script DraggableIcon.
    }

    private void OnDisable()
    {
        // Deshabilitamos el mapa de acciones al salir.
        playerControls.Gameplay.Disable();
        
        // --- LÍNEAS ELIMINADAS ---
    }

    // El método HandlePlacement() o HandleTileSelection() ha sido completamente eliminado
    // porque ya no es la responsabilidad de este script.
}