using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerControl playerControls;

    void Awake()
    {
        playerControls = new PlayerControl();
    }

    private void OnEnable()
    {
        playerControls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        // Deshabilitamos el mapa de acciones al salir.
        playerControls.Gameplay.Disable();
    }
}