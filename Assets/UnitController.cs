using UnityEngine;

public class UnitController : MonoBehaviour
{
    public UnitStats unitStats;
    private HealthBarUI healthBar;
    public Node currentNode;
    public int teamID;

    private float currentHealth;
    private float currentMana;

    public float CurrentHealth { get { return currentHealth; } }

    void Start()
    {
        if (unitStats == null) return;

        currentHealth = unitStats.maxHealth;
        currentMana = unitStats.maxMana;

        healthBar = GetComponentInChildren<HealthBarUI>();
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth, unitStats.maxHealth);
        }
    }

    public void TakeDamage(float damage)
    {
        if (unitStats == null) return;
        currentHealth -= damage;
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth, unitStats.maxHealth);
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (GameManager.Instance != null) GameManager.Instance.UnregisterUnit(this);
        if (currentNode != null) currentNode.isWalkable = true;
        Destroy(gameObject);
    }

    public void EvaluateAction() { /* Tu lógica de combate */ }
    
    [Header("Referencias Internas")]
    [Tooltip("El icono de la UI que originó esta unidad. Se asigna automáticamente.")]
    public UnitIconController originatingIcon;
}