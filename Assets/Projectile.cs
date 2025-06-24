using UnityEngine;

public class Projectile : MonoBehaviour
{
    private UnitController attacker;
    private UnitController target;
    private int damageAmount;
    
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;
    
    public void Initialize(UnitController shooter, UnitController targetUnit, int dmg)
    {
        this.attacker = shooter;
        this.target = targetUnit;
        this.damageAmount = dmg;
        Destroy(gameObject, lifeTime); // El proyectil se autodestruye si no impacta
    }

    void Update()
    {
        if (target == null || target.CurrentHealth <= 0)
        {
            Destroy(gameObject); // El objetivo murió o desapareció
            return;
        }

        Vector3 targetPosition = target.transform.position + new Vector3(0, 0.5f, 0);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        transform.LookAt(targetPosition);

        if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        if (target != null)
        {
            target.TakeDamage(damageAmount);
        }
        Destroy(gameObject);
    }
}