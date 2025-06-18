using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    // --- SINGLETON ---
    // Esto crea una variable estática y global "Instance" para que cualquier
    // script pueda acceder al GameManager de forma sencilla con GameManager.Instance
    public static GameManager Instance { get; private set; }

    // --- ESTADOS DEL JUEGO ---
    // El enum ahora es público para que otros scripts puedan referenciarlo (ej: GameManager.GameState.Combat)
    public enum GameState { Placement, Combat, Result }
    public GameState CurrentState { get; private set; }
    
    // --- EVENTOS ---
    public static UnityAction OnCombatStart;
    public static UnityAction OnCombatEnd; // Añadido para futura expansión

    private void Awake()
    {
        // Lógica del Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // Establecer el estado inicial
        CurrentState = GameState.Placement;
    }

    // El botón de la UI llamará a este método
    public void StartCombatButton()
    {
        if (CurrentState == GameState.Placement)
        {
            CurrentState = GameState.Combat;
            Debug.Log("¡El Combate ha comenzado!");
            
            // Invocamos el evento para que todas las unidades suscritas (en su OnEnable) empiecen a luchar
            OnCombatStart?.Invoke();
        }
    }
}