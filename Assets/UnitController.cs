using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UnitController : MonoBehaviour
{
    [SerializeField] private UnitStats baseStats;
    public int teamID;
    public int CurrentHealth { get; private set; }

    private GridManager gridManager;
    private Coroutine combatCoroutine;

    // --- NUEVAS VARIABLES DE ESTADO ---
    private UnitController currentTarget;
    private Node currentNode;
    private Node destinationNode;
    private float attackCooldown;

    void Start()
    {
        CurrentHealth = baseStats.maxHealth;
        gridManager = FindAnyObjectByType<GridManager>();
        
        // Al empezar, la unidad ocupa el nodo en el que está.
        currentNode = gridManager.NodeFromWorldPoint(transform.position);
        gridManager.SetUnitOnNode(this, currentNode);
    }

    private void OnEnable() => GameManager.OnCombatStart += StartCombat;
    private void OnDisable() => GameManager.OnCombatStart -= StopCombat;

    void StartCombat()
    {
        if (combatCoroutine != null) StopCoroutine(combatCoroutine);
        combatCoroutine = StartCoroutine(CombatLoop());
    }

    void StopCombat()
    {
        if (combatCoroutine != null) StopCoroutine(combatCoroutine);
    }

    IEnumerator CombatLoop()
    {
        while (CurrentHealth > 0)
        {
            // --- FASE DE DECISIÓN ---
            if (currentTarget == null || currentTarget.CurrentHealth <= 0)
            {
                // Si no tengo objetivo o mi objetivo ha muerto, busco uno nuevo.
                FindNewTargetAndPosition();
            }

            // --- FASE DE ACCIÓN ---
            if (currentTarget != null) // Si encontré un objetivo válido
            {
                if (IsInAttackRange())
                {
                    // Si estoy en rango, ATACAR
                    yield return StartCoroutine(Attack());
                }
                else if (destinationNode != null)
                {
                    // Si no estoy en rango pero tengo un destino, MOVERSE
                    yield return StartCoroutine(Move());
                }
            }

            // Si no hay objetivo ni destino, la unidad espera.
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// Lógica principal para encontrar un nuevo objetivo y una posición desde la cual atacarlo.
    /// Si no encuentra un objetivo con posiciones disponibles, no hace nada.
    /// </summary>
    void FindNewTargetAndPosition()
    {
        // Limpiamos el objetivo y destino anteriores
        currentTarget = null;
        UnreserveCurrentDestination();

        // Buscamos TODOS los enemigos y los ordenamos por distancia.
        var allEnemies = FindObjectsByType<UnitController>(FindObjectsSortMode.None)
            .Where(u => u.teamID != this.teamID && u.CurrentHealth > 0)
            .OrderBy(u => Vector3.Distance(transform.position, u.transform.position))
            .ToList();

        // Iteramos sobre cada enemigo, del más cercano al más lejano.
        foreach (var enemy in allEnemies)
        {
            // Intentamos encontrar una casilla de ataque disponible alrededor de este enemigo.
            Node attackNode = FindBestAttackNode(enemy);

            if (attackNode != null)
            {
                // ¡Éxito! Encontramos un enemigo y una posición para atacarlo.
                currentTarget = enemy;
                destinationNode = attackNode;
                gridManager.ReserveNode(destinationNode);
                return; // Salimos del método, ya tenemos nuestro plan.
            }
        }
        // Si el bucle termina, significa que ningún enemigo tiene casillas de ataque disponibles.
        // La unidad se quedará esperando.
    }

    /// <summary>
    /// Busca la mejor casilla adyacente disponible para atacar a un objetivo.
    /// "Mejor" significa la más cercana a la posición actual de esta unidad.
    /// </summary>
    Node FindBestAttackNode(UnitController target)
    {
        List<Node> availableNodes = new List<Node>();
        Node targetNode = gridManager.NodeFromWorldPoint(target.transform.position);

        if (targetNode == null) return null;

        // Recopilamos todas las casillas vecinas que estén disponibles
        foreach (var neighbour in gridManager.GetNeighbours(targetNode))
        {
            if (neighbour.IsAvailable())
            {
                availableNodes.Add(neighbour);
            }
        }

        if (availableNodes.Count == 0) return null; // No hay ninguna casilla disponible

        // De todas las disponibles, devolvemos la que esté más cerca de nosotros
        return availableNodes.OrderBy(n => Vector3.Distance(transform.position, n.worldPosition)).FirstOrDefault();
    }

    IEnumerator Move()
    {
        // Dejamos libre el nodo actual
        gridManager.ClearNode(currentNode);

        List<Node> path = gridManager.FindPath(transform.position, destinationNode.worldPosition);
        if (path != null && path.Count > 0)
        {
            // Moverse por cada nodo del camino hasta llegar al final
            foreach (Node node in path)
            {
                Vector3 targetPosition = node.worldPosition; // El pívot debe estar en la base del personaje
                transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z));

                while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, baseStats.moveSpeed * Time.deltaTime);
                    yield return null; // Esperar al siguiente frame
                }
                // Asegurarse de estar exactamente en el centro del nodo
                transform.position = targetPosition;
            }
        }
        
        // Hemos llegado al destino
        currentNode = destinationNode;
        gridManager.SetUnitOnNode(this, currentNode); // Ocupamos el nuevo nodo
        UnreserveCurrentDestination(); // Lo liberamos de la reserva
    }

    IEnumerator Attack()
    {
        attackCooldown -= Time.deltaTime;
        if (attackCooldown <= 0)
        {
            transform.LookAt(new Vector3(currentTarget.transform.position.x, transform.position.y, currentTarget.transform.position.z));
            
            // Lógica de ataque (animación, proyectil, etc.)
            Debug.Log($"{name} ataca a {currentTarget.name}");
            currentTarget.TakeDamage(baseStats.attackDamage);

            attackCooldown = 1f / baseStats.attackSpeed; // Reiniciar cooldown
        }
        yield return null;
    }

    bool IsInAttackRange()
    {
        return currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) <= baseStats.attackRange;
    }
    
    public void TakeDamage(int damage)
    {
        if (CurrentHealth <= 0) return;
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        StopAllCoroutines();
        // Liberar el nodo que ocupaba al morir
        if (currentNode != null)
        {
            gridManager.ClearNode(currentNode);
        }
        UnreserveCurrentDestination();
        Destroy(gameObject);
    }

    void UnreserveCurrentDestination()
    {
        if (destinationNode != null)
        {
            gridManager.UnreserveNode(destinationNode);
            destinationNode = null;
        }
    }
}