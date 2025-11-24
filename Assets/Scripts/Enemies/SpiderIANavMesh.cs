using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Controlador de la Inteligencia Artificial (IA) de una araña utilizando NavMeshAgent.
/// Implementa lógica de Patrullaje, Persecución con persistencia de rango dual (Detection Range y Lost Range),
/// y mecánica de Pisotón (Stomp) para matar al jugador.
/// </summary>
/// <remarks>
/// Requiere un componente <see cref="NavMeshAgent"/> adjunto.
/// </remarks>
[RequireComponent(typeof(NavMeshAgent))]
public class SpiderIANavMesh : MonoBehaviour
{
    #region Declaraciones de Campos de Inspector

    [Header("Referencias")]
    [Tooltip("Referencia al Transform del jugador.")]
    [SerializeField] private Transform player;
    [Tooltip("Referencia al componente Animator de la araña.")]
    [SerializeField] private Animator animator;

    // --- Patrullaje ---

    [Header("Patrullaje")]
    [Tooltip("Puntos a recorrer. Se requiere un mínimo de 2 puntos para un patrullaje válido.")]
    [SerializeField] private Transform[] patrolPoints;

    [Tooltip("Define el modo de movimiento entre puntos. True: Ping-Pong (ida y vuelta). False: Loop Circular.")]
    [SerializeField] private bool pingPong = false;

    [Tooltip("Velocidad de movimiento del agente durante el estado de Patrullaje.")]
    [SerializeField] private float patrolSpeed = 2f;

    [Tooltip("Distancia mínima restante para considerar que el agente ha llegado al punto de patrulla.")]
    [SerializeField] private float waypointReachedDistance = 0.5f;

    [Tooltip("Tiempo de pausa en segundos al llegar a un punto de patrulla (0 = sin pausa).")]
    [SerializeField] private float waitTimeAtWaypoint = 0f;

    // --- Persecución ---

    [Header("Persecución")]
    [Tooltip("Distancia a la que el agente detecta al jugador e inicia la Persecución.")]
    [SerializeField] private float detectionRange = 8f;

    [Tooltip("Distancia máxima a la que el agente sigue persiguiendo. Si el jugador se aleja más allá de este rango, el temporizador de rendición comienza.")]
    [SerializeField] private float lostPlayerRange = 12f;

    [Tooltip("Velocidad de movimiento del agente durante el estado de Persecución.")]
    [SerializeField] private float chaseSpeed = 4f;

    [Tooltip("Distancia mínima a la que el agente se detiene cerca del jugador para el ataque.")]
    [SerializeField] private float stopChaseDistance = 1.5f;

    [Tooltip("Tiempo de gracia antes de que el agente regrese a Patrullaje después de perder al jugador (salir del lostPlayerRange).")]
    [SerializeField] private float returnToPatrolDelay = 2f;

    // --- Pisotón (Stomp Attack) ---

    [Header("Pisotón (Ataque de Muerte)")]
    [Tooltip("Margen de altura mínimo para considerar que el centro del jugador está por encima de la araña para un pisotón.")]
    [SerializeField] private float stompHeightMargin = 0.3f;
    [Tooltip("Velocidad de impacto vertical mínima del jugador para que el pisotón se considere válido (caída).")]
    [SerializeField] private float minImpactSpeed = 0.3f;
    [Tooltip("Fuerza vertical aplicada al jugador tras un pisotón exitoso (muerte de la araña).")]
    [SerializeField] private float stompBounce = 5f;
    [Tooltip("Duración de la animación de muerte de la araña.")]
    [SerializeField] private float deathDuration = 1f;

    #endregion

    #region Campos Privados (Estado Interno)

    private NavMeshAgent agent;
    private Collider col;

    /// <summary>
    /// Enumera los posibles estados de comportamiento de la IA de la araña.
    /// </summary>
    private enum State { Patrol, Chase, Waiting, Dead, Frozen }
    private State currentState = State.Patrol;

    // --- Patrullaje ---
    private int currentPatrolIndex = 0;
    private int patrolDirection = 1;
    private float waitTimer = 0f;
    private bool hasValidPatrol => patrolPoints != null && patrolPoints.Length >= 2;

    // --- Persecución ---
    private float lostPlayerTimer = 0f;

    // --- Player ---
    private PlayerDeathHandler playerDeathHandler;
    private Rigidbody playerRb;

    // --- Optimización (Distancias al Cuadrado) ---
    private float sqrDetectionRange;
    private float sqrStopChaseDistance;
    private float sqrLostPlayerRange;

    // --- Control de Actualización (Optimización de Update) ---
    private const float UPDATE_INTERVAL = 0.1f; // Intervalo de 100ms para actualizaciones costosas (SetDestination, etc.)
    private float updateTimer = 0f;

    // --- Control de Reset ---
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private int initialPatrolIndex;

    #endregion

    #region Ciclo de Vida de Unity

    /// <summary>
    /// Se llama al inicio. Inicializa componentes, valida configuración y calcula valores cuadrados para optimización.
    /// </summary>
    private void Start()
    {
        InitializeComponents();
        ValidateSetup();

        // Calcular distancias al cuadrado para evitar el costoso uso de Vector3.Distance() y Math.Sqrt().
        sqrDetectionRange = detectionRange * detectionRange;
        sqrStopChaseDistance = stopChaseDistance * stopChaseDistance;
        sqrLostPlayerRange = lostPlayerRange * lostPlayerRange;

        // Guardar estado inicial para el reset
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
            Debug.LogWarning("[SpiderIANavMesh] No hay suficientes patrol points (mínimo 2). La araña se detiene.", this);
            agent.isStopped = true;
        }
    }

    /// <summary>
    /// Se llama en cada frame. Contiene la lógica principal de la IA.
    /// </summary>
    private void Update()
    {
        // Lógica de salida rápida si el agente está Inactivo o Muerto.
        if (currentState == State.Dead || currentState == State.Frozen)
            return;

        // Verificar si el jugador existe y está vivo.
        if (player == null || !IsPlayerAlive())
        {
            if (currentState == State.Chase)
                TransitionToPatrol();
            return;
        }

        updateTimer += Time.deltaTime;

        // Lógica de detección y transición de estado (Persecución/Patrullaje).
        UpdateDetectionState();

        // Ejecutar la lógica específica del estado actual.
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

        // Sincronizar velocidad con el Animator.
        UpdateAnimation();
    }

    #endregion

    #region Inicialización y Validación

    /// <summary>
    /// Inicializa y configura el NavMeshAgent y el Collider.
    /// </summary>
    private void InitializeComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        col = GetComponent<Collider>();

        // Configuración de NavMeshAgent para un movimiento más responsivo y orgánico.
        agent.speed = patrolSpeed;
        agent.angularSpeed = 500f;
        agent.acceleration = 8f;
        agent.stoppingDistance = waypointReachedDistance; // Se actualiza en TransitionToChase
        agent.autoBraking = true;
        agent.updateRotation = true;
        agent.updateUpAxis = true;
    }

    /// <summary>
    /// Valida que las referencias esenciales (Player, Animator) y la configuración del NavMesh sean correctas.
    /// </summary>
    private void ValidateSetup()
    {
        if (player != null)
        {
            playerDeathHandler = player.GetComponent<PlayerDeathHandler>();
            playerRb = player.GetComponent<Rigidbody>();

            if (playerDeathHandler == null)
                Debug.LogWarning("[SpiderIANavMesh] El Transform del jugador no tiene PlayerDeathHandler.", this);
            if (playerRb == null)
                Debug.LogWarning("[SpiderIANavMesh] El Transform del jugador no tiene Rigidbody.", this);
        }
        else
        {
            Debug.LogWarning("[SpiderIANavMesh] Referencia a Player no asignada en el Inspector.", this);
        }

        if (animator == null)
            Debug.LogWarning("[SpiderIANavMesh] Referencia a Animator no asignada.", this);

        // Advertencia si no hay NavMesh cerca (chequeo crucial).
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
        {
            Debug.LogError("[SpiderIANavMesh] No hay NavMesh cerca de la araña. Asegúrate de 'Bake' el NavMesh.", this);
        }
    }

    #endregion

    #region Lógica de Detección (Control de Persistencia)

    /// <summary>
    /// Gestiona las transiciones entre Patrullaje y Persecución basándose en la distancia
    /// al jugador y los rangos de detección/pérdida.
    /// </summary>
    private void UpdateDetectionState()
    {
        float sqrDist = (player.position - transform.position).sqrMagnitude;

        // Detección: El jugador está en el rango inicial para iniciar la persecución.
        bool isDetectionRange = sqrDist <= sqrDetectionRange;

        // Pérdida: El jugador se ha alejado más allá del rango de persistencia.
        bool isLostRange = sqrDist > sqrLostPlayerRange;

        // 1. Transición a Persecución
        if (isDetectionRange && currentState == State.Patrol)
        {
            TransitionToChase();
        }
        // 2. Transición a Patrullaje (Lógica de Rendición)
        else if (isLostRange && currentState == State.Chase)
        {
            lostPlayerTimer += Time.deltaTime;
            // Si el temporizador de rendición expira, volvemos a patrullar.
            if (lostPlayerTimer >= returnToPatrolDelay)
            {
                TransitionToPatrol();
            }
        }
        // 3. Persistencia (Dentro de Lost Range, Fuera de Detection Range)
        else if (currentState == State.Chase)
        {
            // Si el agente está en modo Persecución y el jugador NO está fuera del Lost Range,
            // el agente persiste y el temporizador de pérdida se resetea.
            if (!isLostRange)
            {
                lostPlayerTimer = 0f;
            }
        }
    }

    #endregion

    #region Lógica de Estados (Updates)

    /// <summary>
    /// Lógica ejecutada durante el estado de Patrullaje.
    /// </summary>
    private void UpdatePatrol()
    {
        if (!hasValidPatrol)
            return;

        // Comprobación eficiente de llegada al waypoint.
        bool arrivedAtWaypoint = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;

        // El agente puede detenerse internamente (sin camino o velocidad muy baja)
        bool agentIsStopped = !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;


        if (arrivedAtWaypoint && agentIsStopped)
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

    /// <summary>
    /// Lógica ejecutada durante el estado de Persecución. Se ejecuta periódicamente (UPDATE_INTERVAL)
    /// para optimizar las costosas llamadas a NavMesh.
    /// </summary>
    private void UpdateChase()
    {
        // Optimizando la actualización del destino.
        if (updateTimer >= UPDATE_INTERVAL)
        {
            updateTimer = 0f;

            float sqrDist = (player.position - transform.position).sqrMagnitude;

            // Micro-Stop: Si está muy cerca del jugador (distancia de ataque), se detiene.
            if (sqrDist <= sqrStopChaseDistance)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            else
            {
                // El agente persigue activamente.
                // La rendición por distancia/tiempo se maneja exclusivamente en UpdateDetectionState().
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }
    }

    /// <summary>
    /// Lógica ejecutada durante el estado de Espera (pausa en waypoint).
    /// </summary>
    private void UpdateWaiting()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer >= waitTimeAtWaypoint)
        {
            waitTimer = 0f;
            AdvanceToNextPatrolPoint();
            // Transición implícita a Patrol por el cambio de destino y la reanudación del movimiento.
            currentState = State.Patrol;
        }
    }

    #endregion

    #region Transiciones de Estado

    /// <summary>
    /// Transiciona el agente al estado de Persecución.
    /// </summary>
    private void TransitionToChase()
    {
        currentState = State.Chase;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = stopChaseDistance;
        agent.isStopped = false;
        lostPlayerTimer = 0f;

        // Establecer el primer destino de persecución inmediatamente.
        agent.SetDestination(player.position);
    }

    /// <summary>
    /// Transiciona el agente de vuelta al estado de Patrullaje.
    /// </summary>
    private void TransitionToPatrol()
    {
        currentState = State.Patrol;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = waypointReachedDistance;
        agent.isStopped = false;
        lostPlayerTimer = 0f;

        // Reanudar patrullaje desde el punto más cercano.
        if (hasValidPatrol)
        {
            currentPatrolIndex = FindNearestPatrolIndex();
            SetDestinationToCurrentPatrolPoint();
        }
    }

    /// <summary>
    /// Transiciona el agente al estado de Espera (pausa en waypoint).
    /// </summary>
    private void TransitionToWaiting()
    {
        currentState = State.Waiting;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        waitTimer = 0f;
    }

    #endregion

    #region Lógica de Patrullaje

    /// <summary>
    /// Encuentra el índice del punto de patrullaje más cercano a la posición actual de la araña.
    /// </summary>
    /// <returns>El índice del punto de patrullaje más cercano.</returns>
    private int FindNearestPatrolIndex()
    {
        int bestIndex = 0;
        float bestSqrDist = float.MaxValue;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            // Usar sqrMagnitude para comparar distancias sin la raíz cuadrada.
            float sqrDist = (patrolPoints[i].position - transform.position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Avanza al siguiente punto de patrullaje, gestionando los modos Ping-Pong y Circular.
    /// </summary>
    private void AdvanceToNextPatrolPoint()
    {
        if (pingPong)
        {
            // Lógica Ping-Pong: invierte dirección al llegar a los extremos.
            if (currentPatrolIndex >= patrolPoints.Length - 1)
                patrolDirection = -1;
            else if (currentPatrolIndex <= 0)
                patrolDirection = 1;

            currentPatrolIndex += patrolDirection;
        }
        else
        {
            // Lógica Circular: reinicia al llegar al final.
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        SetDestinationToCurrentPatrolPoint();
    }

    /// <summary>
    /// Establece el destino del NavMeshAgent al punto de patrullaje actual,
    /// validando que el punto sea alcanzable en el NavMesh.
    /// </summary>
    private void SetDestinationToCurrentPatrolPoint()
    {
        if (!hasValidPatrol)
            return;

        Transform target = patrolPoints[currentPatrolIndex];

        // Verificar que el punto está en el NavMesh antes de establecer el destino.
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target.position, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            Debug.LogWarning($"[SpiderIANavMesh] Patrol point {currentPatrolIndex} no está en el NavMesh. Intentando el siguiente...", this);
            // Si el punto es inválido, avanzamos al siguiente punto.
            AdvanceToNextPatrolPoint();
        }
    }

    #endregion

    #region Animación

    /// <summary>
    /// Sincroniza el estado de movimiento del agente con el Animator.
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        // IsChase refleja el movimiento activo (Patrol o Chase) y la velocidad del agente.
        bool isMoving = currentState == State.Patrol || currentState == State.Chase;
        bool hasVelocity = agent.velocity.sqrMagnitude > 0.01f;

        animator.SetBool("IsChase", isMoving && hasVelocity);
    }

    #endregion

    #region Colisión y Pisotón

    /// <summary>
    /// Maneja la colisión con el jugador para determinar si ocurre un pisotón (muerte de la araña)
    /// o si la araña mata al jugador.
    /// </summary>
    /// <param name="collision">Datos de la colisión.</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == State.Dead || currentState == State.Frozen)
            return;

        if (!collision.collider.CompareTag("Player"))
            return;

        if (!IsPlayerAlive())
            return;

        // Asegurar la referencia al Rigidbody del jugador
        if (playerRb == null)
        {
            playerRb = collision.collider.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                Debug.LogWarning("[SpiderIANavMesh] Player sin Rigidbody, no se puede ejecutar lógica de colisión de muerte.", this);
                return;
            }
        }

        if (IsStompAttack(collision))
        {
            // Si el jugador pisa a la araña correctamente, la araña muere.
            StartCoroutine(Die(playerRb));
        }
        else
        {
            // Si la araña colisiona con el jugador, la araña mata al jugador.
            StartCoroutine(PlayerKilledSequence());
        }
    }

    /// <summary>
    /// Determina si la colisión con el jugador cuenta como un Pisotón (Stomp Attack) válido.
    /// </summary>
    /// <param name="collision">Datos de la colisión.</param>
    /// <returns>True si el jugador pisa a la araña.</returns>
    private bool IsStompAttack(Collision collision)
    {
        // 1. Centro del jugador está por encima de la araña (margen).
        bool centerAbove = player.position.y > transform.position.y + stompHeightMargin;

        // 2. Al menos un punto de contacto está en la parte superior del collider de la araña.
        bool contactAbove = false;
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.point.y > transform.position.y + stompHeightMargin)
            {
                contactAbove = true;
                break;
            }
        }

        // 3. El jugador está cayendo (velocidad vertical negativa).
        bool falling = playerRb.velocity.y < -minImpactSpeed ||
                      collision.relativeVelocity.y < -minImpactSpeed;

        return centerAbove && contactAbove && falling;
    }

    #endregion

    #region Muerte y Reseteo

    /// <summary>
    /// Corrutina que maneja la muerte de la araña (por pisotón).
    /// </summary>
    /// <param name="playerRb">Rigidbody del jugador para el rebote.</param>
    private IEnumerator Die(Rigidbody playerRb)
    {
        currentState = State.Dead;

        animator?.SetTrigger("Death");

        // Desactivar componentes
        if (col != null)
            col.enabled = false;

        agent.isStopped = true;
        agent.enabled = false;

        // Rebotar al jugador para feedback visual
        if (playerRb != null)
            playerRb.AddForce(Vector3.up * stompBounce, ForceMode.VelocityChange);

        yield return new WaitForSeconds(deathDuration);

        Destroy(gameObject);
    }

    /// <summary>
    /// Secuencia que se ejecuta cuando la araña mata al jugador (colisión simple).
    /// Congela la araña, mata al jugador y espera el tiempo de respawn/fade para resetearse.
    /// </summary>
    private IEnumerator PlayerKilledSequence()
    {
        // Congelar araña inmediatamente para evitar movimientos extraños durante la muerte del jugador.
        currentState = State.Frozen;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Ejecutar la muerte del jugador.
        playerDeathHandler?.Die();

        // Toca un sonido de ataque (Asumiendo que MultiAudioPool es un sistema de audio global).
        MultiAudioPool.Instance?.Play("spiderAttack", transform.position);

        animator.SetBool("IsChase", false); // Detener la animación de correr.

        // Esperar el tiempo necesario (debe coincidir con la secuencia de muerte/respawn del jugador).
        yield return new WaitForSeconds(2f);

        // Resetear la araña a su punto inicial.
        ResetToInitialState();
    }

    /// <summary>
    /// Método público para resetear la araña a su posición, rotación y estado inicial de patrullaje.
    /// </summary>
    public void ResetToInitialState()
    {
        // Desactivar NavMeshAgent para un movimiento manual seguro.
        agent.enabled = false;

        // Restaurar posición y rotación guardadas en Start().
        transform.SetPositionAndRotation(initialPosition, initialRotation);

        // Reactivar y resetear parámetros del agente.
        agent.enabled = true;
        currentState = State.Patrol;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = waypointReachedDistance;
        agent.isStopped = false;
        lostPlayerTimer = 0f;
        waitTimer = 0f;

        // Resetear patrulla al punto de inicio.
        if (hasValidPatrol)
        {
            currentPatrolIndex = initialPatrolIndex;
            patrolDirection = 1;
            SetDestinationToCurrentPatrolPoint();
        }

        // Resetear animación a un estado neutral.
        if (animator != null)
        {
            animator.ResetTrigger("Death");
            animator.SetBool("IsChase", false);
            // Asegurarse de que el animator esté en un estado base (Idle)
            animator.Play("Idle", 0, 0f);
        }

        Debug.Log("[SpiderIANavMesh] Araña reseteada a posición inicial");
    }

    #endregion

    #region Utilidad

    /// <summary>
    /// Verifica si el jugador está vivo o en proceso de morir.
    /// </summary>
    /// <returns>True si el jugador está disponible y no está muriendo.</returns>
    private bool IsPlayerAlive()
    {
        return playerDeathHandler == null || !playerDeathHandler.isDying;
    }

    #endregion

    #region Gizmos de Debug

    /// <summary>
    /// Dibuja Gizmos en el Editor para visualizar rangos y camino de patrulla, mejorando la depuración.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Rango de Detección (Amarillo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Rango de Pérdida/Persistencia (Naranja semitransparente)
        if (lostPlayerRange > detectionRange)
        {
            Gizmos.color = new Color(1f, 0.64f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, lostPlayerRange);
        }

        // Distancia de Parada (Rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopChaseDistance);

        // Puntos de Patrullaje (Cian)
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);

                    // Líneas de conexión
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (!pingPong && i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                    {
                        // Conexión del último al primero en modo circular (Loop)
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }

        // Camino actual del NavMeshAgent (Verde) - Solo en modo Play
        if (Application.isPlaying && agent != null && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                // Dibuja la ruta calculada por el NavMeshAgent
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }

        // Posición Inicial de Reseteo (Magenta) - Solo en modo Play
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(initialPosition, 0.5f);
            Gizmos.DrawLine(transform.position, initialPosition);
        }
    }

    #endregion
}