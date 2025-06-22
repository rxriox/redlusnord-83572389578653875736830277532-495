using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Image healthBar;

    // CORRECCIÓN: Añadimos la función que faltaba para actualizar la UI.
    public void SetHealth(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            // Se asegura de que el valor esté entre 0 y 1.
            healthBar.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }
}