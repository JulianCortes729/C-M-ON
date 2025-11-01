using UnityEngine;
using System.Collections;

/// <summary>
/// Araña con patrullaje configurable (2+ puntos), persecución al jugador y retorno automático al patrullaje.
/// Arreglos incluidos:
///  - Comparación de llegada al punto solo en XZ (evita quedar atascada por diferencias en Y).
///  - Avanza al siguiente punto si la dirección es prácticamente cero (casos borde).
///  - Activación del bool "IsChase" en Animator tanto para patrullar como para perseguir
///    (según tu comentario: la misma animación se usa para ambos).
/// Complejidad: O(1) por frame en Update/FixedUpdate (las búsquedas O(n) son esporádicas).
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class SpiderIA2 : MonoBehaviour
{
    #region Inspector Serialized Fields

    [Header("Referencias")]
    [Tooltip("Transform del jugador.")]
    [SerializeField] private Transform player;

    [Tooltip("Animator con bool 'IsChase' y trigger 'Death'.")]
    [SerializeField] private Animator animator;

    [Tooltip("LayerMask que representa el suelo (para detección de bordes).")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Patrullaje")]
    [Tooltip("Lista de puntos (Transform) que la araña recorrerá en patrullaje. Asignar 2 o más.")]
    [SerializeField] private Transform[] patrolPoints;

    [Tooltip("Si true hace ping-pong (ida y vuelta). Si false hace loop circular.")]
    [SerializeField] private bool pingPong = false;

    [Tooltip("Velocidad cuando patrulla.")]
    [SerializeField] private float patrolSpeed = 2.0f;

    [Tooltip("Umbral (metros) en XZ para considerar que llegó a un punto de patrulla.")]
    [SerializeField] private float patrolPointThreshold = 0.2f;

    [Header("Persecución / Movimiento")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float edgeCheckDistance = 0.6f;
    [SerializeField] private float edgeRayHeight = 0.5f;
    [SerializeField] private float edgeRayDepth = 1.2f;

    [Header("Pisotón (muerte de la araña)")]
    [SerializeField] private float stompHeightMargin = 0.3f;
    [SerializeField] private float minImpactSpeed = 0.3f;
    [SerializeField] private float stompBounce = 5f;
    [SerializeField] private float deathDuration = 1f;

    #endregion

    #region Private Fields

    private Rigidbody rb;
    private Collider col;

    // Patrullaje
    private int currentPatrolIndex = 0;
    private int patrolDirection = 1; // +1 hacia adelante, -1 hacia atrás (para ping-pong)
    private bool hasValidPatrol => patrolPoints != null && patrolPoints.Length >= 2;

    // Estados
    private bool isChasing = false;
    private bool isDead = false;

    // Cached player death handler (si existe)
    private PlayerDeathHandler playerDeathHandler;

    // Precomputed squared range for cheaper checks
    private float sqrDetectionRange;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
        ValidateReferences();

        sqrDetectionRange = detectionRange * detectionRange;

        if (hasValidPatrol)
        {
            // Elegir el punto de patrulla más cercano al iniciar (evita viajes innecesarios).
            currentPatrolIndex = FindNearestPatrolIndex();
            // Mirar hacia el objetivo inicial (orientación instantánea para evitar giros al inicio).
            FaceDirection(GetDirectionToTargetXZ(patrolPoints[currentPatrolIndex].position));
        }
        else
        {
            Debug.LogWarning("[SpiderAI2_PatrolFix] No hay patrolPoints válidos (necesitas 2 o más).", this);
        }
    }

    private void Update()
    {
        if (isDead || player == null)
            return;

        if (!IsPlayerAlive())
        {
            StopChasing();
            return;
        }

        UpdateChaseState();
        // Actualizamos la animación IsChase para que sea true tanto si persigue como si está patrullando.
        // Aquí consideramos "movimiento activo" = persecución o patrulla válida.
        bool shouldPlayChaseAnim = isChasing || (!isChasing && hasValidPatrol);
        animator?.SetBool("IsChase", shouldPlayChaseAnim);
    }

    private void FixedUpdate()
    {
        if (isDead)
            return;

        // Movimiento: prioridad persecución, si no entonces patrulla (si hay puntos)
        if (isChasing)
        {
            Fixed_ChasePlayer();
        }
        else if (hasValidPatrol)
        {
            Fixed_Patrol();
        }
    }

    #endregion

    #region Initialization & Validation

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    private void ValidateReferences()
    {
        if (player == null)
            Debug.LogWarning("[SpiderAI2_PatrolFix] player no asignado.", this);
        else
        {
            playerDeathHandler = player.GetComponent<PlayerDeathHandler>();
            if (playerDeathHandler == null)
                Debug.LogWarning("[SpiderAI2_PatrolFix] player no tiene PlayerDeathHandler.", this);
        }

        if (animator == null)
            Debug.LogWarning("[SpiderAI2_PatrolFix] animator no asignado.", this);
    }

    #endregion

    #region Chase / Patrol State Management

    /// <summary>
    /// Actualiza si debemos perseguir según distancia squared (evita sqrt).
    /// </summary>
    private void UpdateChaseState()
    {
        float sqrDistance = (transform.position - player.position).sqrMagnitude;
        bool shouldChase = sqrDistance <= sqrDetectionRange;

        if (shouldChase != isChasing)
        {
            isChasing = shouldChase;
            // Nota: la animación IsChase se maneja en Update() para cubrir patrulla también.
            // animator?.SetBool("IsChase", isChasing); // ya lo hacemos globalmente en Update
            if (!isChasing && hasValidPatrol)
            {
                // Al dejar de perseguir, volver a apuntar al patrol point más cercano
                currentPatrolIndex = FindNearestPatrolIndex();
            }
        }
    }

    private void StopChasing()
    {
        if (isChasing) isChasing = false;
    }

    #endregion

    #region Fixed Movement: Chase & Patrol

    /// <summary>
    /// Movimiento cuando persigue (FixedUpdate).
    /// </summary>
    private void Fixed_ChasePlayer()
    {
        Vector3 dir = GetDirectionToTargetXZ(player.position);
        if (dir == Vector3.zero)
            return;

        // Evitar caídas
        if (!IsGroundAhead(dir))
        {
            rb.velocity = Vector3.zero;
            return;
        }

        rb.MovePosition(transform.position + dir * chaseSpeed * Time.fixedDeltaTime);
        RotateTowards(dir);
    }

    /// <summary>
    /// Movimiento para patrullaje entre puntos (FixedUpdate).
    /// Corregimos la comprobación de llegada usando solo XZ y manejamos el caso 'dir == zero'.
    /// </summary>
    private void Fixed_Patrol()
    {
        Transform target = patrolPoints[currentPatrolIndex];
        Vector3 dir = GetDirectionToTargetXZ(target.position);

        // COMPROBACIÓN DE LLEGADA (solo XZ): evita que diferencias en Y bloqueen la lógica.
        Vector2 posXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 targetXZ = new Vector2(target.position.x, target.position.z);
        float sqrThresh = patrolPointThreshold * patrolPointThreshold;
        if ((posXZ - targetXZ).sqrMagnitude <= sqrThresh)
        {
            AdvancePatrolIndex();
            return;
        }

        // Si la dirección horizontal es prácticamente cero (por ejemplo XZ iguales), 
        // avanzamos para no quedar estancados.
        if (dir == Vector3.zero)
        {
            AdvancePatrolIndex();
            return;
        }

        // Evitar caídas: si no hay suelo hacia la dirección al objetivo, detenerse y rotar.
        if (!IsGroundAhead(dir))
        {
            rb.velocity = Vector3.zero;
            RotateTowards(dir);
            return;
        }

        rb.MovePosition(transform.position + dir * patrolSpeed * Time.fixedDeltaTime);
        RotateTowards(dir);
    }

    #endregion

    #region Patrol Helpers

    /// <summary>
    /// Encuentra el índice del patrol point más cercano (O(n), llamado en Start o al terminar chase).
    /// </summary>
    private int FindNearestPatrolIndex()
    {
        int bestIndex = 0;
        float bestSqr = float.MaxValue;
        Vector3 pos = transform.position;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Vector3 p = patrolPoints[i].position;
            // comparamos en XZ para consistencia con movimiento
            float dx = p.x - pos.x;
            float dz = p.z - pos.z;
            float s = dx * dx + dz * dz;
            if (s < bestSqr)
            {
                bestSqr = s;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Avanza al siguiente index respetando ping-pong o loop.
    /// </summary>
    private void AdvancePatrolIndex()
    {
        if (pingPong)
        {
            if (currentPatrolIndex == patrolPoints.Length - 1)
                patrolDirection = -1;
            else if (currentPatrolIndex == 0)
                patrolDirection = 1;

            currentPatrolIndex += patrolDirection;
        }
        else
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    #endregion

    #region Direction / Rotation / Ground Detection

    /// <summary>
    /// Dirección normalizada hacia target proyectada en XZ (evita Y).
    /// </summary>
    private Vector3 GetDirectionToTargetXZ(Vector3 targetPosition)
    {
        Vector3 d = targetPosition - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return Vector3.zero;
        return d.normalized;
    }

    /// <summary>
    /// Rotación suave hacia la dirección dada.
    /// </summary>
    private void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Orientación instantánea (uso en Start).
    /// </summary>
    private void FaceDirection(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    /// <summary>
    /// Comprueba si hay suelo delante con un raycast; si direction == zero se usa forward.
    /// </summary>
    private bool IsGroundAhead(Vector3 direction)
    {
        if (direction == Vector3.zero) direction = transform.forward;
        Vector3 origin = transform.position + direction * edgeCheckDistance + Vector3.up * edgeRayHeight;
        bool hasGround = Physics.Raycast(origin, Vector3.down, edgeRayDepth, groundLayer);

#if UNITY_EDITOR
        Debug.DrawRay(origin, Vector3.down * edgeRayDepth, hasGround ? Color.green : Color.red);
#endif

        return hasGround;
    }

    #endregion

    #region Collision & Stomp Logic

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead)
            return;

        if (!collision.collider.CompareTag("Player"))
            return;

        if (!IsPlayerAlive())
            return;

        Rigidbody playerRb = collision.collider.GetComponent<Rigidbody>();
        if (playerRb == null)
        {
            Debug.LogWarning("[SpiderAI2_PatrolFix] Player sin Rigidbody.", this);
            return;
        }

        if (IsStompAttack(collision, playerRb))
        {
            StartCoroutine(DieAndBounce(playerRb));
        }
        else
        {
            playerDeathHandler?.Die();
            if (hasValidPatrol)
                currentPatrolIndex = FindNearestPatrolIndex();
        }
    }

    private bool IsStompAttack(Collision collision, Rigidbody playerRb)
    {
        bool centerAbove = player.position.y > transform.position.y + stompHeightMargin;
        bool contactAbove = HasContactAbove(collision);
        bool falling = IsFalling(playerRb, collision);

        return centerAbove && contactAbove && falling;
    }

    private bool HasContactAbove(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
#if UNITY_EDITOR
            Debug.DrawRay(contact.point, Vector3.up * 0.2f, Color.yellow, 1f);
#endif
            if (contact.point.y - transform.position.y > stompHeightMargin)
                return true;
        }
        return false;
    }

    private bool IsFalling(Rigidbody playerRb, Collision collision)
    {
        float playerVerticalVelocity = playerRb.velocity.y;
        float relativeVerticalVelocity = collision.relativeVelocity.y;

        return (playerVerticalVelocity < -minImpactSpeed) ||
               (relativeVerticalVelocity < -minImpactSpeed);
    }

    #endregion

    #region Death Coroutine

    private IEnumerator DieAndBounce(Rigidbody playerRb)
    {
        isDead = true;
        animator?.SetTrigger("Death");

        if (col != null) col.enabled = false;

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        if (playerRb != null)
            playerRb.AddForce(Vector3.up * stompBounce, ForceMode.VelocityChange);

        yield return new WaitForSeconds(deathDuration);

        Destroy(gameObject);
    }

    #endregion

    #region Utility

    private bool IsPlayerAlive()
    {
        return playerDeathHandler == null || !playerDeathHandler.isDying;
    }

    #endregion
}
