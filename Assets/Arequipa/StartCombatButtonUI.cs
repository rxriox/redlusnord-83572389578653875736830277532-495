using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(CanvasGroup))]
public class StartCombatButtonUI : MonoBehaviour
{
    [Header("Apariencia")]
    [Tooltip("La opacidad del botón cuando está activo.")]
    [Range(0, 1)] public float activeAlpha = 1.0f;

    [Tooltip("La opacidad del botón cuando está inactivo.")]
    [Range(0, 1)] public float inactiveAlpha = 0.5f;

    private Button targetButton;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        targetButton = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        GameManager.OnUnitCountChanged += HandleUnitOrStateChange;
        GameManager.OnGameStateChanged += HandleUnitOrStateChange;
    }

    private void OnDisable()
    {
        GameManager.OnUnitCountChanged -= HandleUnitOrStateChange;
        GameManager.OnGameStateChanged -= HandleUnitOrStateChange;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            HandleUnitOrStateChange();
        }
    }

    private void HandleUnitOrStateChange()
    {
        if (GameManager.Instance == null) return;

        int team0Count = GameManager.Instance.GetUnitCountForTeam(0);
        int team1Count = GameManager.Instance.GetUnitCountForTeam(1);

        bool canStart = GameManager.Instance.CurrentState == GameManager.GameState.Placement &&
                        team0Count > 0 &&
                        team1Count > 0;

        targetButton.interactable = canStart;
        canvasGroup.alpha = canStart ? activeAlpha : inactiveAlpha;
    }

    private void HandleUnitOrStateChange(GameManager.GameState newState)
    {
        HandleUnitOrStateChange();
    }
}