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
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (target == null || target.CurrentHealth <= 0)
        {
            Destroy(gameObject);
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
        if (target != null && target.CurrentHealth > 0)
        {
            Debug.Log($"{UnitController.GetTeamTag(attacker.teamID)} {attacker.unitStats.unitName} ha infligido {damageAmount} de daño a {UnitController.GetTeamTag(target.teamID)} {target.unitStats.unitName}.");
            target.TakeDamage(damageAmount, attacker);
        }
        Destroy(gameObject);
    }
}