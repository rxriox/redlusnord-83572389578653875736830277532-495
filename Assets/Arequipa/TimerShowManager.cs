using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class TimerShowManager : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("El Panel que contiene el texto del temporizador.")]
    public GameObject timerPanel;
    [Tooltip("El objeto de texto que muestra los segundos.")]
    public TextMeshProUGUI timerText;
    [Tooltip("El Panel que se muestra para indicar que ha comenzado el Overtime.")]
    public GameObject overtimePanel;
    [Tooltip("El objeto de texto que está dentro del panel de Overtime.")]
    public TextMeshProUGUI overtimeText;

    [Header("Configuración Visual")]
    [Tooltip("El color del panel cuando el tiempo está entre 10 y 4 segundos.")]
    public Color warningColor = Color.black;
    [Tooltip("El color del panel cuando el tiempo está entre 3 y 0 segundos.")]
    public Color dangerColor = Color.red;

    [Header("Sonidos del Temporizador")]
    [Tooltip("Sonido para los segundos 5, 4 y 3.")]
    public AudioClip normalTickSound;
    [Tooltip("Sonido para el segundo 2.")]
    public AudioClip finalTickSound2;
    [Tooltip("Sonido para el segundo 1.")]
    public AudioClip finalTickSound1;
    [Tooltip("Sonido para el segundo 0 (final del combate).")]
    public AudioClip finalTickSound0;

    private Image timerPanelImage;
    private UIPulseEffect timerPulseEffect;
    private UIPulseEffect overtimePulseEffect;
    private Coroutine overtimePulseCoroutine;

    private AudioSource audioSource;
    private int lastSecondDisplayed = -1;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
            timerPanelImage = timerPanel.GetComponent<Image>();
        }
        if (timerText != null)
        {
            timerPulseEffect = timerText.GetComponent<UIPulseEffect>();
        }
        if (overtimePanel != null)
        {
            overtimePanel.SetActive(false);
            if (overtimeText != null)
            {
                overtimePulseEffect = overtimeText.GetComponent<UIPulseEffect>();
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        GameManager.GameState currentState = GameManager.Instance.CurrentState;

        HandleCountdownTimer(currentState);
        
        HandleOvertimePanel(currentState);
    }

    private void HandleCountdownTimer(GameManager.GameState currentState)
    {
        bool shouldBeActive = currentState == GameManager.GameState.Combat;
        if (!shouldBeActive)
        {
            if (timerPanel != null && timerPanel.activeSelf)
            {
                timerPanel.SetActive(false);
                lastSecondDisplayed = -1;
            }
            return;
        }

        if (timerText == null || timerPanel == null) return;

        float timeLeft = GameManager.BATTLE_TIME_LIMIT - GameManager.Instance.battleTimer;
        if (timeLeft <= 10.99f)
        {
            timerPanel.SetActive(true);
            int currentSecond = Mathf.CeilToInt(timeLeft);
            currentSecond = Mathf.Clamp(currentSecond, 0, 10);

            if (currentSecond != lastSecondDisplayed)
            {
                lastSecondDisplayed = currentSecond;
                UpdateTimerDisplay(currentSecond);
            }
        }
        else
        {
            if (timerPanel.activeSelf)
            {
                timerPanel.SetActive(false);
                lastSecondDisplayed = -1;
            }
        }
    }

    private void HandleOvertimePanel(GameManager.GameState currentState)
    {
        bool shouldBeActive = currentState == GameManager.GameState.Overtime;
        if (overtimePanel != null)
        {
            overtimePanel.SetActive(shouldBeActive);

            if (shouldBeActive && overtimePulseCoroutine == null)
            {
                overtimePulseCoroutine = StartCoroutine(OvertimePulseLoop());
            }
            else if (!shouldBeActive && overtimePulseCoroutine != null)
            {
                StopCoroutine(overtimePulseCoroutine);
                overtimePulseCoroutine = null;
            }
        }
    }

    private IEnumerator OvertimePulseLoop()
    {
        while (true)
        {
            if (overtimePulseEffect != null)
            {
                overtimePulseEffect.PlayPulse();
            }
            yield return new WaitForSeconds(1.5f);
        }
    }

    private void UpdateTimerDisplay(int currentSecond)
    {
        timerText.text = currentSecond.ToString();

        if (timerPanelImage != null)
        {
            timerPanelImage.color = (currentSecond <= 3) ? dangerColor : warningColor;
        }

        if (timerPulseEffect != null)
        {
            timerPulseEffect.PlayPulse();
        }

        PlayTimerSound(currentSecond);
    }

    private void PlayTimerSound(int second)
    {
        AudioClip clipToPlay = null;
        switch (second)
        {
            case 5: case 4: case 3:
                clipToPlay = normalTickSound;
                break;
            case 2:
                clipToPlay = finalTickSound2;
                break;
            case 1:
                clipToPlay = finalTickSound1;
                break;
            case 0:
                clipToPlay = finalTickSound0;
                break;
        }

        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }
}