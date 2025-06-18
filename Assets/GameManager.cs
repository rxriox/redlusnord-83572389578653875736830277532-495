using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public enum GameState { Placement, Combat, Result }
    public GameState CurrentState { get; private set; }
    
    public static UnityAction OnCombatStart;

    void Start()
    {
        CurrentState = GameState.Placement;
    }

    public void StartCombat()
    {
        if (CurrentState == GameState.Placement)
        {
            CurrentState = GameState.Combat;
            Debug.Log("¡El Combate ha comenzado!");
            OnCombatStart?.Invoke();
        }
    }
}