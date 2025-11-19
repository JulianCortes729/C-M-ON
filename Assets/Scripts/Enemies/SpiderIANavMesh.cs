using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// IA de araña mejorada usando NavMesh.
/// Ventajas:
/// - Navegación automática evitando obstáculos
/// - No se cae de plataformas (respeta el NavMesh)
/// - Encuentra caminos inteligentes automáticamente
/// - Más estable y sin temblores
/// - Detecta si el destino es alcanzable
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SpiderIANavMesh : MonoBehaviour
{
    #region Inspector Fields

    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;

    [Header("Patrullaje")]
    [Tooltip("Puntos de patrullaje (mínimo 2)")]
    [SerializeField] private Transform[] patrolPoints;

    [Tooltip("Si true: ping-pong, si false: loop circular")]
    [SerializeField] private bool pingPong = false;

    [Tooltip("Velocidad durante patrullaje")]
    [SerializeField] private float patrolSpeed = 2f;

    [Tooltip("Distancia para considerar que llegó al punto")]
    [SerializeField] private float waypointReachedDistance = 0.5f;

    [Tooltip("Tiempo de espera al llegar a un punto (0 = sin pausa)")]
    [SerializeField] private float waitTimeAtWaypoint = 0f;

    [Header("Persecución")]
    [Tooltip("Distancia para empezar a perseguir")]
    [SerializeField] private float detectionRange = 8f;

    [Tooltip("Velocidad durante persecución")]
    [SerializeField] private float chaseSpeed = 4f;

    [Tooltip("Distancia mínima para detenerse cerca del jugador")]
    [SerializeField] private float stopChaseDistance = 1.5f;

    [Tooltip("Tiempo antes de volver a patrullar si pierde al jugador")]
    [SerializeField] private float returnToPatrolDelay = 2f;

    [Header("Pisotón (muerte)")]
    [SerializeField] private float stompHeightMargin = 0.3f;
    [SerializeField] private float minImpactSpeed = 0.3f;
    [SerializeField] private float stompBounce = 5f;
    [SerializeField] private float deathDuration = 1f;

    #endregion

    #region Private Fields

    private NavMeshAgent agent;
    private Collider col;

    // Estados
    private enum State { Patrol, Chase, Waiting, Dead, Frozen }
    private State currentState = State.Patrol;

    // Patrullaje
    private int currentPatrolIndex = 0;
    private int patrolDirection = 1;
    private float waitTimer = 0f;
    private bool hasValidPatrol => patrolPoints != null && patrolPoints.Length >= 2;

    // Persecución
    private float lostPlayerTimer = 0f;
    private bool playerInRange = false;

    // Player
    private PlayerDeathHandler playerDeathHandler;
    private Rigidbody playerRb;

    // Optimización
    private float sqrDetectionRange;
    private float sqrStopChaseDistance;

    // Control de actualización
    private float updateInterval = 0.1f;
    private float updateTimer = 0f;

    // NUEVO: Reset coordinado
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private int initialPatrolIndex;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
        ValidateSetup();

        sqrDetectionRange = detectionRange * detectionRange;
        sqrStopChaseDistance = stopChaseDistance * stopChaseDistance;

        // NUEVO: Guardar posición y estado inicial
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (hasValidPatrol)
        {
            currentPatrolIndex = FindNearestPatrolIndex();
            initialPatrolIndex = currentPatrolIndex;
            SetDestinationToCurrentPatrolPoint();
        }
        else
        {
            Debug.LogWarning("[SpiderAINavMesh] No hay suficientes patrol points (mínimo 2)", this);
            agent.isStopped = true;
        }
    }

    private void Update()
    {
        if (currentState == State.Dead || currentState == State.Frozen)
            return;

        if (player == null || !IsPlayerAlive())
        {
            if (currentState == State.Chase)
                TransitionToPatrol();
            return;
        }

        updateTimer += Time.deltaTime;

        // Actualizar estado según distancia al jugador
        UpdateDetectionState();

        // Ejecutar lógica del estado actual
        switch (currentState)
        {
            case State.Patrol:
                UpdatePatrol();
                break;

            case State.Chase:
                UpdateChase();
                break;

            case State.Waiting:
                UpdateWaiting();
                break;
        }

        // Actualizar animación
        UpdateAnimation();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        col = GetComponent<Collider>();

        // Configurar NavMeshAgent
        agent.speed = patrolSpeed;
        agent.angularSpeed = 500f;
        agent.acceleration = 8f;
        agent.stoppingDistance = waypointReachedDistance;
        agent.autoBraking = true;
        agent.updateRotation = true;
        agent.updateUpAxis = true;
    }

    private void ValidateSetup()
    {
        if (player != null)
        {
            playerDeathHandler = player.GetComponent<PlayerDeathHandler>();
            playerRb = player.GetComponent<Rigidbody>();

            if (playerDeathHandler == null)
                Debug.LogWarning("[SpiderAINavMesh] Player no tiene PlayerDeathHandler", this);
            if (playerRb == null)
                Debug.LogWarning("[SpiderAINavMesh] Player no tiene Rigidbody", this);
        }
        else
        {
            Debug.LogWarning("[SpiderAINavMesh] Player no asignado", this);
        }

        if (animator == null)
            Debug.LogWarning("[SpiderAINavMesh] Animator no asignado", this);

        // Verificar que hay NavMesh
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
        {
            Debug.LogError("[SpiderAINavMesh] No hay NavMesh cerca de la araña. Asegúrate de 'Bake' el NavMesh.", this);
        }
    }

    #endregion

    #region State Detection

    private void UpdateDetectionState()
    {
        float sqrDist = (player.position - transform.position).sqrMagnitude;
        bool wasInRange = playerInRange;
        playerInRange = sqrDist <= sqrDetectionRange;

        // Transición a persecución
        if (playerInRange && currentState == State.Patrol)
        {
            TransitionToChase();
        }
        // Perdió al jugador
        else if (!playerInRange && currentState == State.Chase)
        {
            lostPlayerTimer += Time.deltaTime;
            if (lostPlayerTimer >= returnToPatrolDelay)
            {
                TransitionToPatrol();
            }
        }
        // Resetear timer si vuelve a estar en rango
        else if (playerInRange && currentState == State.Chase)
        {
            lostPlayerTimer = 0f;
        }
    }

    #endregion

    #region State Updates

    private void UpdatePatrol()
    {
        if (!hasValidPatrol)
            return;

        // Si llegamos al waypoint
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
            {
                if (waitTimeAtWaypoint > 0)
                {
                    TransitionToWaiting();
                }
                else
                {
                    AdvanceToNextPatrolPoint();
                }
            }
        }
    }

    private void UpdateChase()
    {
        // Actualizar destino periódicamente (no cada frame para optimizar)
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;

            float sqrDist = (player.position - transform.position).sqrMagnitude;

            // Si está muy cerca, detenerse
            if (sqrDist <= sqrStopChaseDistance)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            else
            {
                // Verificar si el destino es alcanzable
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(player.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
                else
                {
                    // No puede alcanzar al jugador, volver a patrullar
                    lostPlayerTimer = returnToPatrolDelay;
                }
            }
        }
    }

    private void UpdateWaiting()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer >= waitTimeAtWaypoint)
        {
            waitTimer = 0f;
            AdvanceToNextPatrolPoint();
            currentState = State.Patrol;
        }
    }

    #endregion

    #region State Transitions

    private void TransitionToChase()
    {
        currentState = State.Chase;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = stopChaseDistance;
        agent.isStopped = false;
        lostPlayerTimer = 0f;

        agent.SetDestination(player.position);
    }

    private void TransitionToPatrol()
    {
        currentState = State.Patrol;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = waypointReachedDistance;
        agent.isStopped = false;
        lostPlayerTimer = 0f;

        if (hasValidPatrol)
        {
            currentPatrolIndex = FindNearestPatrolIndex();
            SetDestinationToCurrentPatrolPoint();
        }
    }

    private void TransitionToWaiting()
    {
        currentState = State.Waiting;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        waitTimer = 0f;
    }

    #endregion

    #region Patrol Logic

    private int FindNearestPatrolIndex()
    {
        int bestIndex = 0;
        float bestSqrDist = float.MaxValue;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float sqrDist = (patrolPoints[i].position - transform.position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void AdvanceToNextPatrolPoint()
    {
        if (pingPong)
        {
            if (currentPatrolIndex >= patrolPoints.Length - 1)
                patrolDirection = -1;
            else if (currentPatrolIndex <= 0)
                patrolDirection = 1;

            currentPatrolIndex += patrolDirection;
        }
        else
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        SetDestinationToCurrentPatrolPoint();
    }

    private void SetDestinationToCurrentPatrolPoint()
    {
        if (!hasValidPatrol)
            return;

        Transform target = patrolPoints[currentPatrolIndex];

        // Verificar que el punto está en el NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target.position, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            Debug.LogWarning($"[SpiderAINavMesh] Patrol point {currentPatrolIndex} no está en el NavMesh", this);
            // Intentar con el siguiente punto
            AdvanceToNextPatrolPoint();
        }
    }

    #endregion

    #region Animation

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        // IsChase = true cuando se está moviendo (patrol o chase)
        bool isMoving = currentState == State.Patrol || currentState == State.Chase;
        bool hasVelocity = agent.velocity.sqrMagnitude > 0.01f;

        animator.SetBool("IsChase", isMoving && hasVelocity);
    }

    #endregion

    #region Collision & Stomp

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == State.Dead || currentState == State.Frozen)
            return;

        if (!collision.collider.CompareTag("Player"))
            return;

        if (!IsPlayerAlive())
            return;

        if (playerRb == null)
        {
            playerRb = collision.collider.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                Debug.LogWarning("[SpiderAINavMesh] Player sin Rigidbody", this);
                return;
            }
        }

        if (IsStompAttack(collision))
        {
            StartCoroutine(Die(playerRb));
        }
        else
        {
            // MODIFICADO: En lugar de matar directamente, congelar y avisar
            StartCoroutine(PlayerKilledSequence());
        }
    }

    private bool IsStompAttack(Collision collision)
    {
        // 1. Centro del jugador está por encima
        bool centerAbove = player.position.y > transform.position.y + stompHeightMargin;

        // 2. Al menos un punto de contacto está arriba
        bool contactAbove = false;
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.point.y > transform.position.y + stompHeightMargin)
            {
                contactAbove = true;
                break;
            }
        }

        // 3. Jugador cayendo
        bool falling = playerRb.velocity.y < -minImpactSpeed ||
                      collision.relativeVelocity.y < -minImpactSpeed;

        return centerAbove && contactAbove && falling;
    }

    #endregion

    #region Death & Reset

    private IEnumerator Die(Rigidbody playerRb)
    {
        currentState = State.Dead;

        animator?.SetTrigger("Death");

        // Desactivar física y movimiento
        if (col != null)
            col.enabled = false;

        agent.isStopped = true;
        agent.enabled = false;

        // Rebotar al jugador
        if (playerRb != null)
            playerRb.AddForce(Vector3.up * stompBounce, ForceMode.VelocityChange);

        yield return new WaitForSeconds(deathDuration);

        Destroy(gameObject);
    }

    // NUEVO: Secuencia cuando la araña mata al jugador
    private IEnumerator PlayerKilledSequence()
    {
        // Congelar araña inmediatamente
        currentState = State.Frozen;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Matar al jugador
        playerDeathHandler?.Die();

        //sonido de ataque
        MultiAudioPool.Instance?.Play("spiderAttack", transform.position);


        animator.SetBool("IsChase", false);
        // Esperar a que termine la animación de muerte y el fade
        // (esto debería coincidir con el tiempo del fade + delay del respawn)
        yield return new WaitForSeconds(2f);

        // Resetear araña a posición inicial
        ResetToInitialState();
    }

    // NUEVO: Método público para resetear araña (puede ser llamado externamente también)
    public void ResetToInitialState()
    {
        // Desactivar NavMeshAgent temporalmente para mover manualmente
        agent.enabled = false;

        // Restaurar posición y rotación
        transform.SetPositionAndRotation(initialPosition, initialRotation);

        // Reactivar NavMeshAgent
        agent.enabled = true;

        // Resetear estado
        currentState = State.Patrol;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = waypointReachedDistance;
        agent.isStopped = false;
        lostPlayerTimer = 0f;
        playerInRange = false;
        waitTimer = 0f;

        // Resetear patrulla al punto inicial
        if (hasValidPatrol)
        {
            currentPatrolIndex = initialPatrolIndex;
            patrolDirection = 1;
            SetDestinationToCurrentPatrolPoint();
        }

        // Resetear animación
        if (animator != null)
        {
            animator.ResetTrigger("Death");
            animator.SetBool("IsChase", false);
            animator.Play("Idle", 0, 0f);
        }

        Debug.Log("[SpiderAINavMesh] Araña reseteada a posición inicial");
    }

    #endregion

    #region Utility

    private bool IsPlayerAlive()
    {
        return playerDeathHandler == null || !playerDeathHandler.isDying;
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        // Rango de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Distancia de parada al perseguir
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopChaseDistance);

        // Puntos de patrullaje
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);

                    // Líneas conectando los puntos
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (!pingPong && i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                    {
                        // Loop: conectar último con primero
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }

        // Camino actual del NavMeshAgent
        if (Application.isPlaying && agent != null && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }

        // NUEVO: Mostrar posición inicial
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(initialPosition, 0.5f);
            Gizmos.DrawLine(transform.position, initialPosition);
        }
    }

    #endregion
}