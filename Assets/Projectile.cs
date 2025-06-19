using UnityEngine;

/// <summary>
/// Controla el comportamiento de un proyectil disparado por una unidad a distancia.
/// Mueve el proyectil hacia un objetivo y aplica daño al impactar.
/// </summary>
public class Projectile : MonoBehaviour
{
    private UnitController attacker; // La unidad que disparó este proyectil.
    private UnitController target;   // La unidad que es el objetivo de este proyectil.
    private int damageAmount;        // La cantidad de daño que infligirá el proyectil.
    
    [Header("Configuración del Proyectil")]
    [Tooltip("Velocidad a la que se mueve el proyectil.")]
    [SerializeField] private float speed = 10f; // Velocidad del proyectil.
    [Tooltip("Tiempo máximo de vida del proyectil antes de destruirse (útil para proyectiles que fallan).")]
    [SerializeField] private float lifeTime = 5f; // Tiempo de vida máximo del proyectil.
    
    // Opcional: Puedes añadir un efecto de impacto visual aquí si tienes prefabs de partículas/animaciones.
    // [SerializeField] private GameObject hitEffectPrefab;

    /// <summary>
    /// Inicializa el proyectil con el atacante, el objetivo y la cantidad de daño.
    /// </summary>
    /// <param name="shooter">La unidad que disparó el proyectil.</param>
    /// <param name="targetUnit">La unidad objetivo del proyectil.</param>
    /// <param name="dmg">La cantidad de daño a infligir al objetivo.</param>
    public void Initialize(UnitController shooter, UnitController targetUnit, int dmg)
    {
        attacker = shooter;
        target = targetUnit;
        damageAmount = dmg;

        // Asegura que el proyectil se destruya después de un tiempo si no impacta al objetivo.
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Si el objetivo es nulo (ej. ha muerto o ha desaparecido), el proyectil se destruye.
        if (target == null || target.CurrentHealth <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // Mueve el proyectil hacia la posición del objetivo.
        // Se añade un pequeño offset vertical para que el proyectil apunte al centro del objetivo.
        Vector3 targetPosition = target.transform.position + Vector3.up * 0.5f; 
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Opcional: Rota el proyectil para que siempre mire al objetivo mientras se mueve.
        transform.LookAt(targetPosition);

        // Si el proyectil está lo suficientemente cerca del objetivo, se considera que impactó.
        if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
        {
            HitTarget();
        }
    }

    /// <summary>
    /// Se llama cuando el proyectil impacta a su objetivo.
    /// </summary>
    void HitTarget()
    {
        // Verifica si el objetivo sigue siendo válido y tiene salud.
        if (target != null && target.CurrentHealth > 0)
        {
            target.TakeDamage(damageAmount); // Aplica el daño al objetivo.
            // Opcional: Instanciar un efecto visual en la posición de impacto.
            // if (hitEffectPrefab != null)
            // {
            //     Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            // }
        }
        // Destruye el proyectil después de impactar (o de fallar el impacto inicial).
        Destroy(gameObject);
    }

    // Opcional: Si los proyectiles tienen colliders y quieres usar detección de colisiones físicas,
    // puedes descomentar el siguiente método y asegurarte de que el collider del proyectil
    // y el collider de la unidad objetivo sean triggers.
    /*
    void OnTriggerEnter(Collider other)
    {
        // Intenta obtener el componente UnitController del objeto con el que colisionó.
        UnitController hitUnit = other.GetComponent<UnitController>();
        // Si colisionó con una unidad y esa unidad es nuestro objetivo, entonces es un impacto.
        if (hitUnit != null && hitUnit == target) 
        {
            HitTarget();
        }
    }
    */
}
