using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum StatusEffect { Dazed, Immobilized, Sealed, Fear, Incurable, Environment_Disabled, Vanish }

public class UnitController : MonoBehaviour
{
    private readonly Dictionary<StatusEffect, Coroutine> activeStatusEffects = new Dictionary<StatusEffect, Coroutine>();
    public enum DamageType { Material, Inmaterial, Absoluto }
    public UnitStats unitStats;
    public int teamID;
    public Node currentNode;
    public UnitIconController originatingIcon;
    public Artifact EquippedArtifact { get; private set; }
    public int CurrentLevel { get; private set; }

    private Harmony_Defiant defiantLogic;
    private Harmony_Human humanLogic;

    private Renderer[] allRenderers;
    private Collider unitCollider;


    public int MaxHealth
    {
        get
        {
            int baseHealth = (unitStats.maxHealthByLevel.Count >= CurrentLevel && CurrentLevel > 0) ? unitStats.maxHealthByLevel[CurrentLevel - 1] : 0;
            if (EquippedArtifact != null)
            {
                baseHealth += EquippedArtifact.healthBonus;
            }
            return baseHealth;
        }
    }

    public float CurrentHealth { get; private set; }

    public int CurrentAttackDamage
    {
        get
        {
            int baseDamage = (unitStats.attackDamageByLevel.Count >= CurrentLevel && CurrentLevel > 0) ? unitStats.attackDamageByLevel[CurrentLevel - 1] : 0;
            if (EquippedArtifact != null)
            {
                baseDamage += EquippedArtifact.attackDamageBonus;
            }
            return baseDamage;
        }
    }

    public float CurrentAttackSpeed
    {
        get
        {
            float baseAttackSpeed = unitStats.attackSpeed;
            if (EquippedArtifact != null)
            {
                baseAttackSpeed += EquippedArtifact.attackSpeedBonus;
            }

            if (humanLogic != null)
            {
                baseAttackSpeed += humanLogic.GetAttackSpeedBonus();
            }

            return baseAttackSpeed;
        }
    }

    public float CurrentMoveSpeed
    {
        get
        {
            float baseMoveSpeed = unitStats.moveSpeed;
            if (EquippedArtifact != null)
            {
                baseMoveSpeed += EquippedArtifact.moveSpeedBonus;
            }
            return baseMoveSpeed;
        }
    }

    private enum State { IDLE, MOVING, ATTACKING }
    private State currentState = State.IDLE;

    private Coroutine movementCoroutine;
    private UnitController currentTarget;
    private List<Node> currentPath;
    private float attackCooldown = 0f;
    private GridManager gridManager;

    private void Awake() // <- Cambia Start por Awake si es necesario para asegurar el orden
    {
        gridManager = FindFirstObjectByType<GridManager>();
        defiantLogic = GetComponent<Harmony_Defiant>();
        humanLogic = GetComponent<Harmony_Human>();
        allRenderers = GetComponentsInChildren<Renderer>();
        unitCollider = GetComponent<Collider>();
    }

    public void Initialize(int level)
    {
        this.CurrentLevel = level;
        this.CurrentHealth = this.MaxHealth;
    }

    public void EvaluateAction()
    {
        if (defiantLogic != null && GameManager.Instance.IsHarmonyActiveForTeam(GameManager.Instance.FindHarmonyByName("Defiant"), teamID))
        {
            defiantLogic.EvaluateBlink();
        }

        if (HasStatus(StatusEffect.Dazed) || HasStatus(StatusEffect.Environment_Disabled) || HasStatus(StatusEffect.Vanish))
        {
            return;
        }

        if (currentState == State.MOVING || currentState == State.ATTACKING) return;
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }

        if (currentTarget == null || currentTarget.CurrentHealth <= 0)
        {
            FindClosestEnemy();
            if (currentTarget == null)
            {
                currentState = State.IDLE;
                return;
            }
        }

        if (IsTargetInAttackRange())
        {
            if (attackCooldown <= 0 && !HasStatus(StatusEffect.Fear))
            {
                PerformAttack();
            }
        }
        else
        {
            UnitController immediateTarget = FindEnemyInAttackRange();
            if (immediateTarget != null)
            {
                Debug.Log($"{unitStats.unitName} cambia de objetivo a {immediateTarget.unitStats.unitName} por estar más cerca.");
                currentTarget = immediateTarget;
                if (attackCooldown <= 0 && !HasStatus(StatusEffect.Fear))
                {
                    PerformAttack();
                }
            }
            else
            {
                if (!HasStatus(StatusEffect.Immobilized))
                {
                    MoveTowardsTarget();
                }
            }
        }
    }

    private UnitController FindEnemyInAttackRange()
    {
        return GameManager.Instance.GetAllUnits()
            .Where(unit => unit != null && unit.teamID != this.teamID && unit.CurrentHealth > 0 && !unit.HasStatus(StatusEffect.Vanish) && IsUnitWithinAttackRange(unit))
            .OrderBy(unit => Vector3.Distance(transform.position, unit.transform.position))
            .FirstOrDefault();
    }

    private bool IsUnitWithinAttackRange(UnitController unit)
    {
        if (unit == null || unit.currentNode == null || this.currentNode == null) return false;
        int dist_x = Mathf.Abs(currentNode.gridX - unit.currentNode.gridX);
        int dist_z = Mathf.Abs(currentNode.gridZ - unit.currentNode.gridZ);
        int distance = Mathf.Max(dist_x, dist_z);

        return distance <= unitStats.attackRange;
    }

    private void FindClosestEnemy()
    {
        currentTarget = GameManager.Instance.GetAllUnits()
            .Where(unit => unit != null && unit != this && unit.teamID != this.teamID && unit.CurrentHealth > 0 && !unit.HasStatus(StatusEffect.Vanish))
            .OrderBy(unit => Vector3.Distance(transform.position, unit.transform.position))
            .FirstOrDefault();
    }

    public bool IsTargetInAttackRange()
    {
        return IsUnitWithinAttackRange(currentTarget);
    }

    private void PerformAttack()
    {
        float damageMultiplier = 1.0f;
        if (defiantLogic != null && GameManager.Instance.IsHarmonyActiveForTeam(GameManager.Instance.FindHarmonyByName("Defiant"), teamID))
        {
            damageMultiplier = defiantLogic.GetDamageMultiplier();
        }
        int finalDamage = Mathf.RoundToInt(CurrentAttackDamage * damageMultiplier);

        if (GameManager.Instance.CurrentState != GameManager.GameState.Combat)
        {
            currentState = State.IDLE;
            return;
        }

        currentState = State.ATTACKING;
        transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));

        if (unitStats.unitType == UnitStats.UnitType.Ranged && unitStats.projectilePrefab != null)
        {
            GameObject projGO = ObjectPooler.Instance.SpawnFromPool("Proyectil", transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Projectile projectile = projGO.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(this, currentTarget, finalDamage, DamageType.Material);
        }
        else
        {
            Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ataca a {GetTeamTag(currentTarget.teamID)} {currentTarget.unitStats.unitName}");
            currentTarget.TakeDamage(finalDamage, this, DamageType.Material);
        }

        attackCooldown = 1f / CurrentAttackSpeed;
        StartCoroutine(ResetStateAfterAction(0.1f));
    }

    private void MoveTowardsTarget()
    {
        if (HasStatus(StatusEffect.Immobilized) || HasStatus(StatusEffect.Dazed)) return;

        if (gridManager == null || currentTarget == null || currentState != State.IDLE) return;
        currentPath = gridManager.FindPath(currentNode, currentTarget.currentNode);
        if (currentPath != null && currentPath.Count > 0)
        {
            Node nextNodeInPath = currentPath[0];
            bool isNextNodeOccupied = (GameManager.Instance.GetUnitAtNode(nextNodeInPath) != null);
            if (nextNodeInPath.isWalkable && !isNextNodeOccupied)
            {
                nextNodeInPath.isWalkable = false;

                if (currentNode != null)
                {
                    currentNode.isWalkable = true;
                }

                Node previousNode = currentNode;
                currentNode = nextNodeInPath;
                movementCoroutine = StartCoroutine(AnimateMove(previousNode, nextNodeInPath));

                StartCoroutine(AnimateMove(previousNode, nextNodeInPath));
            }
            else
            {
                currentState = State.IDLE;
            }
        }
        else
        {
            currentState = State.IDLE;
        }
    }

    public void ResetActionState()
    {
        // Detiene cualquier corrutina de movimiento que se esté ejecutando.
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        // ===== MODIFICACIÓN AQUÍ: Limpieza completa del estado de la IA =====
        
        // 1. Resetea el estado para que pueda tomar una nueva decisión.
        currentState = State.IDLE;

        // 2. ¡La clave! Olvida a quién estaba persiguiendo.
        currentTarget = null;

        // 3. Olvida el camino que estaba siguiendo.
        if (currentPath != null)
        {
            currentPath.Clear();
        }

        // 4. Un pequeño reseteo del cooldown para evitar acciones instantáneas raras.
        attackCooldown = 0.2f;
        
        // ===== FIN DE LA MODIFICACIÓN =====
    }

    private IEnumerator AnimateMove(Node from, Node to)
    {
        currentState = State.MOVING;

        Vector3 startPosition = from.worldPosition;
        Vector3 endPosition = to.worldPosition;

        if (endPosition - startPosition != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(endPosition - startPosition);
        }

        float time = 0f;
        float moveDuration = 1f / CurrentMoveSpeed;
        while (time < moveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, time / moveDuration);
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
        currentState = State.IDLE;

        movementCoroutine = null;
    }

    private IEnumerator ResetStateAfterAction(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentState = State.IDLE;
    }

    private IEnumerator AnimateMoveToPosition(Vector3 targetPosition)
    {
        currentState = State.MOVING;

        Vector3 startPosition = transform.position;

        if (targetPosition - startPosition != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(targetPosition - startPosition);
        }

        float time = 0f;
        float moveDuration = 1f / CurrentMoveSpeed;
        while (time < moveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, time / moveDuration);
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        currentState = State.IDLE;
    }

    public void TakeDamage(float damage, UnitController attacker, DamageType damageType)
    {
        Debug.Log($"Tipo de daño recibido: {damageType}");

        if (attacker != null)
        {
            Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ha recibido {damage:F1} de daño de {GetTeamTag(attacker.teamID)} {attacker.unitStats.unitName}.");
        }
        else
        {
            Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ha recibido {damage:F1} de daño ambiental (tiempo extra).");
        }

        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            PanelUnitDetails.Instance.ForceDetailsPanelUpdate(this);
            Die(attacker);
        }
        else
        {
            PanelUnitDetails.Instance.ForceDetailsPanelUpdate(this);
        }
    }

    public void Die(UnitController killer)
    {
        Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ha sido eliminada.");
        if (killer != null)
        {
            Debug.Log($"{GetTeamTag(killer.teamID)} {killer.unitStats.unitName} ha eliminado a {GetTeamTag(this.teamID)} {this.unitStats.unitName}");
        }
        else
        {
            Debug.Log($"{GetTeamTag(this.teamID)} {this.unitStats.unitName} ha sido eliminado por el entorno.");
        }

        StopAllCoroutines();
        if (currentNode != null) currentNode.isWalkable = true;
        GameManager.Instance.UnregisterUnit(this);
        GameManager.Instance.CheckForCombatEnd();
        Destroy(gameObject);
    }

    public static string GetTeamTag(int teamID)
    {
        if (teamID == 0) return "<color=#42A5F5>[Aliada]</color>";
        if (teamID == 1) return "<color=#EF5350>[Enemiga]</color>";
        return "[Equipo ?]";
    }

    public void EquipArtifact(Artifact artifact)
    {
        if (EquippedArtifact != null)
        {
            UnequipArtifact();
        }

        EquippedArtifact = artifact;

        if (EquippedArtifact.healthBonus > 0)
        {
            CurrentHealth += EquippedArtifact.healthBonus;
            if (PlacementUIManager.Instance != null)
            {
                PanelUnitDetails.Instance.ForceDetailsPanelUpdate(this);
            }
        }
        PanelUnitDetails.Instance.ForceDetailsPanelUpdate(this);

        Debug.Log($"{unitStats.unitName} ha equipado {artifact.artifactName}");
    }

    public void UnequipArtifact()
    {
        if (EquippedArtifact != null)
        {
            Debug.Log($"{unitStats.unitName} se ha desequipado {EquippedArtifact.artifactName}");

            if (EquippedArtifact.healthBonus > 0)
            {
                CurrentHealth -= EquippedArtifact.healthBonus;
                if (CurrentHealth <= 0) CurrentHealth = 1;

                if (PanelUnitDetails.Instance != null)
                {
                    PanelUnitDetails.Instance.ForceDetailsPanelUpdate(this);
                }
            }

            EquippedArtifact = null;
            PanelUnitDetails.Instance.ForceDetailsPanelUpdate(this);
        }
    }

    public bool IsUnitWithinDistance(UnitController otherUnit, int distance)
    {
        if (otherUnit == null || otherUnit.currentNode == null || this.currentNode == null) return false;
        int dist_x = Mathf.Abs(currentNode.gridX - otherUnit.currentNode.gridX);
        int dist_z = Mathf.Abs(currentNode.gridZ - otherUnit.currentNode.gridZ);
        return Mathf.Max(dist_x, dist_z) <= distance;
    }

    public bool HasStatus(StatusEffect effect)
    {
        return activeStatusEffects.ContainsKey(effect);
    }

    public void ApplyStatus(StatusEffect effect, float duration)
    {
        if (HasStatus(effect))
        {
            StopCoroutine(activeStatusEffects[effect]);
        }

        if (effect == StatusEffect.Vanish)
        {
            SetVisibility(false);
        }

        foreach (var enemy in GameManager.Instance.GetAllUnits())
            {
                // Si una unidad enemiga me tenía como objetivo...
                if (enemy != null && enemy.teamID != this.teamID && enemy.currentTarget == this)
                {
                    // ...le ordeno que resetee su estado y busque un nuevo objetivo.
                    enemy.ResetActionState();
                }
            }

        Coroutine statusCoroutine = StartCoroutine(StatusCoroutine(effect, duration));
        activeStatusEffects[effect] = statusCoroutine;
        Debug.Log($"{unitStats.unitName} ahora está afectado por {effect} durante {duration}s.");
    }

    private IEnumerator StatusCoroutine(StatusEffect effect, float duration)
    {
        yield return new WaitForSeconds(duration);
        RemoveStatus(effect);
    }

    public void RemoveStatus(StatusEffect effect)
    {
        if (HasStatus(effect))
        {
            activeStatusEffects.Remove(effect);
            Debug.Log($"{unitStats.unitName} ya no está afectado por {effect}.");

            // Si el efecto es Vanish, vuelve a hacer visible la unidad
            if (effect == StatusEffect.Vanish)
            {
                SetVisibility(true);
            }
        }
    }

    public void SetVisibility(bool isVisible)
    {
        if (unitCollider != null) unitCollider.enabled = isVisible;
        foreach (var rend in allRenderers)
        {
            rend.enabled = isVisible;
        }
    }

    public void ReceiveHealing(int amount)
    {
        if (HasStatus(StatusEffect.Incurable))
        {
            Debug.Log($"{unitStats.unitName} no puede ser curado porque tiene el estado Incurable.");
            return;
        }

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        Debug.Log($"{unitStats.unitName} ha sido curado por {amount}.");
        PanelUnitDetails.Instance.ForceDetailsPanelUpdate(this);
    }
}
