using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(CanvasGroup))]
public class ResetArtifactsButtonUI : MonoBehaviour
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
        GameManager.OnGameStateChanged += HandleGameStateChange;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChange;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            HandleGameStateChange(GameManager.Instance.CurrentState);
        }
    }

    private void HandleGameStateChange(GameManager.GameState newState)
    {
        bool canBeUsed = (newState != GameManager.GameState.Combat && newState != GameManager.GameState.Overtime);

        targetButton.interactable = canBeUsed;
        canvasGroup.alpha = canBeUsed ? activeAlpha : inactiveAlpha;
    }
}