using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UnitController : MonoBehaviour
{
    public UnitStats unitStats;
    public int teamID;
    public Node currentNode;
    public UnitIconController originatingIcon;
    public float CurrentHealth { get; private set; }

    // --- MODIFICACIÓN 1: Simplificamos los estados ---
    // Ya no necesitamos CHASING. IDLE se encargará de decidir si moverse o atacar.
    private enum State { IDLE, MOVING, ATTACKING }
    private State currentState = State.IDLE;

    private UnitController currentTarget;
    private List<Node> currentPath;
    private float attackCooldown = 0f;
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        CurrentHealth = unitStats.maxHealth;
    }

    public void EvaluateAction()
    {
        // Si estamos en medio de una acción (movimiento o ataque), no reevaluamos.
        // La reevaluación ocurrirá cuando estas acciones terminen y el estado vuelva a IDLE.
        if (currentState == State.MOVING || currentState == State.ATTACKING) return;
        
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }

        // --- LÓGICA DE PRIORIDADES ---

        // PRIORIDAD 1: ¿HAY ALGUIEN A QUIEN ATACAR EN MI RANGO AHORA MISMO?
        UnitController immediateTarget = FindEnemyInAttackRange();
        if (immediateTarget != null)
        {
            currentTarget = immediateTarget; // Fijamos este como nuestro objetivo
            if (attackCooldown <= 0)
            {
                PerformAttack();
            }
            return; // Acción del frame decidida, salimos.
        }

        // PRIORIDAD 2: SI NO HAY NADIE CERCA, ¿TENGO UN OBJETIVO A LARGO PLAZO?
        // Si no tenemos un objetivo o el que teníamos murió, buscamos uno nuevo.
        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            FindClosestEnemy();
            if (currentTarget == null)
            {
                // No quedan enemigos en el mapa.
                currentState = State.IDLE;
                return;
            }
        }

        // PRIORIDAD 3: MOVERSE HACIA EL OBJETIVO A LARGO PLAZO
        // Si llegamos aquí, significa que tenemos un objetivo, pero no está en rango.
        MoveTowardsTarget();
    }

    private UnitController FindEnemyInAttackRange()
    {
        // Busca en todas las unidades la más cercana que esté DENTRO de nuestro rango de ataque.
        return GameManager.Instance.GetAllUnits()
            .Where(unit => unit != null && unit.teamID != this.teamID && unit.CurrentHealth > 0 && IsUnitWithinAttackRange(unit))
            .OrderBy(unit => Vector3.Distance(transform.position, unit.transform.position))
            .FirstOrDefault();
    }
    
    // --- MÉTODO AYUDANTE: Comprueba si una unidad específica está en rango ---
    private bool IsUnitWithinAttackRange(UnitController unit)
    {
        if (unit == null || unit.currentNode == null || this.currentNode == null) return false;
        int dist_x = Mathf.Abs(currentNode.gridX - unit.currentNode.gridX);
        int dist_z = Mathf.Abs(currentNode.gridZ - unit.currentNode.gridZ);
        int distance = Mathf.Max(dist_x, dist_z); // Distancia de Chebyshev para cuadrículas

        return distance <= unitStats.attackRange;
    }

    private void FindClosestEnemy()
    {
        currentTarget = GameManager.Instance.GetAllUnits()
            .Where(unit => unit != null && unit != this && unit.teamID != this.teamID && unit.CurrentHealth > 0)
            .OrderBy(unit => Vector3.Distance(transform.position, unit.transform.position))
            .FirstOrDefault();
    }

    private bool IsTargetInAttackRange()
    {
        // Reutilizamos nuestro nuevo método ayudante.
        return IsUnitWithinAttackRange(currentTarget);
    }

    private void PerformAttack()
    {
        currentState = State.ATTACKING;
        transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));

        if (unitStats.unitType == UnitStats.UnitType.Ranged && unitStats.projectilePrefab != null)
        {
            GameObject projGO = ObjectPooler.Instance.SpawnFromPool("Proyectil", transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Projectile projectile = projGO.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(this, currentTarget, unitStats.attackDamage);
        }
        else
        {
            Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ataca a {GetTeamTag(currentTarget.teamID)} {currentTarget.unitStats.unitName}");
            currentTarget.TakeDamage(unitStats.attackDamage, this);
        }

        attackCooldown = 1f / unitStats.attackSpeed;
        StartCoroutine(ResetStateAfterAction(0.1f));
    }

    private void MoveTowardsTarget()
    {
        if (gridManager == null || currentTarget == null || currentState == State.MOVING) return;
        currentPath = gridManager.FindPath(currentNode, currentTarget.currentNode);

        if (currentPath != null && currentPath.Count > 0)
        {
            StartCoroutine(MoveToNode(currentPath[0]));
        }
        else
        {
            currentState = State.IDLE;
        }
    }
    
    private IEnumerator ResetStateAfterAction(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentState = State.IDLE;
    }


    private IEnumerator MoveToNode(Node targetNode)
{
    currentState = State.MOVING;

    Vector3 startPosition = transform.position;
    Vector3 endPosition = targetNode.worldPosition;
    
    // --- LÓGICA DE NODOS MODIFICADA ---
    Node originNode = currentNode; // Guardamos el nodo de origen

    // Marcamos el NODO DE DESTINO como no transitable para que otros no intenten ir allí.
    targetNode.isWalkable = false; 
    
    float time = 0f;
    if(endPosition - startPosition != Vector3.zero)
    {
        transform.rotation = Quaternion.LookRotation(endPosition - startPosition);
    }

    while (time < 1f / unitStats.moveSpeed)
    {
        transform.position = Vector3.Lerp(startPosition, endPosition, time * unitStats.moveSpeed);
        time += Time.deltaTime;
        yield return null;
    }

    transform.position = endPosition;
    
    // --- AHORA, AL LLEGAR AL DESTINO, LIBERAMOS EL DE ORIGEN ---
    if (originNode != null) originNode.isWalkable = true;
    
    // Actualizamos nuestro nodo actual
    currentNode = targetNode;
    
    currentState = State.IDLE; 
}

    public void TakeDamage(float damage, UnitController attacker)
    {
        Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ha recibido {damage} de daño de {GetTeamTag(attacker.teamID)} {attacker.unitStats.unitName}.");
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die(attacker);
        }
    }

    public void Die(UnitController killer)
    {
        Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ha sido eliminada.");
        if (killer != null)
        {
            Debug.Log($"{GetTeamTag(killer.teamID)} {killer.unitStats.unitName} ha eliminado a {GetTeamTag(this.teamID)} {this.unitStats.unitName}");
        }
        
        StopAllCoroutines();
        if (currentNode != null) currentNode.isWalkable = true;
        GameManager.Instance.UnregisterUnit(this);
        GameManager.Instance.CheckForCombatEnd();
        Destroy(gameObject);
    }
     public static string GetTeamTag(int teamID)
    {
        if (teamID == 0) return "<color=#42A5F5>[Aliada]</color>";   // BLUE = ALLIES
        if (teamID == 1) return "<color=#EF5350>[Enemiga]</color>";   // RED = ENEMIES
        return "[Equipo ?]";
    }
}