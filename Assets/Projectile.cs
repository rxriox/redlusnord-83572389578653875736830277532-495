using UnityEngine;

public class Projectile : MonoBehaviour
{
    private UnitController attacker;
    private UnitController target;
    private int damageAmount;
    
    [Header("Configuración del Proyectil")]
    [Tooltip("Velocidad a la que se mueve el proyectil.")]
    [SerializeField] private float speed = 10f; // Velocidad del proyectil.
    [Tooltip("Tiempo máximo de vida del proyectil antes de destruirse (útil para proyectiles que fallan).")]
    [SerializeField] private float lifeTime = 5f; // Tiempo de vida máximo del proyectil.
    
    // Opcional: Puedes añadir un efecto de impacto visual aquí si tienes prefabs de partículas/animaciones.
    // [SerializeField] private GameObject hitEffectPrefab;
    /// Inicializa el proyectil con el atacante, el objetivo y la cantidad de daño.

    public void Initialize(UnitController shooter, UnitController targetUnit, int dmg)
    {
        attacker = shooter;
        target = targetUnit;
        damageAmount = dmg;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (target == null || target.CurrentHealth <= 0)
        {
            Destroy(gameObject);
            return;
        }

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

    void HitTarget()
    {
        if (target != null && target.CurrentHealth > 0)
        {
            target.TakeDamage(damageAmount); // Aplica el daño al objetivo.

            // Opcional: Instanciar un efecto visual en la posición de impacto.
            // if (hitEffectPrefab != null)
            // {
            //     Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            // }
        }
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
