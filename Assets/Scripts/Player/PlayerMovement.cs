using UnityEngine;

/// <summary>
/// Controla el movimiento, salto y animaciones del jugador.
/// Implementa movimiento correcto sobre plataformas móviles sin usar SetParent.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.5f;

    [Header("Gravity Settings")]
    [SerializeField] private float gravityMultiplier = 2.5f;
    [SerializeField] private float fallMultiplier = 3f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Dash")]
    [SerializeField] private float dashForce = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private string dashTrailKey = "dashTrail";

    [Header("Double Jump Jetpack")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float jetpackForce = 7f;
    [SerializeField] private GameObject handTrailPrefab;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    // Estado de salto y trails
    private int jumpCount = 0;
    private TrailRenderer leftTrail;
    private TrailRenderer rightTrail;

    // Componentes
    private Rigidbody rb;
    private Animator anim;
    private Camera mainCam;

    // Estado de movimiento
    private bool isGrounded;
    private bool wasGrounded;

    // Dash
    private bool canDash = true;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private GameObject dashTrailInstance;

    // Entrada de usuario
    private float inputX;
    private float inputZ;

    // Constantes
    private static readonly Vector3 Down = Vector3.down;

    // Parámetro suavizado para animaciones
    private float smoothVerticalVelocity;

    // PLATAFORMAS MÓVILES - seguimiento sin SetParent
    private Transform currentPlatform;
    private Vector3 platformLastPosition;
    private bool isOnPlatform = false;
    private Vector3 platformVelocity = Vector3.zero;

    #region Unity Lifecycle

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        anim = GetComponent<Animator>();
        mainCam = Camera.main;

        if (handTrailPrefab != null && leftHand != null && rightHand != null)
        {
            leftTrail = Instantiate(handTrailPrefab, leftHand.position, leftHand.rotation, leftHand)
                        .GetComponent<TrailRenderer>();
            rightTrail = Instantiate(handTrailPrefab, rightHand.position, rightHand.rotation, rightHand)
                         .GetComponent<TrailRenderer>();

            leftTrail.emitting = false;
            rightTrail.emitting = false;
        }
    }

    void Update()
    {
        inputX = Input.GetAxis("Horizontal");
        inputZ = Input.GetAxis("Vertical");

        anim.SetBool("IsRunning", inputX != 0 || inputZ != 0);

        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            PerformJump();
        }

        if (Input.GetKeyDown(KeyCode.C) && !isGrounded && canDash && !isDashing)
        {
            StartDash();
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();

        // Actualizar velocidad de la plataforma antes de aplicar movimiento
        UpdatePlatformVelocity();

        if (isDashing)
        {
            dashTimer += Time.fixedDeltaTime;
            if (dashTimer >= dashDuration)
            {
                EndDash();
            }
            return;
        }

        HandleMovement();
        ApplyBetterJumpPhysics();

        smoothVerticalVelocity = Mathf.Lerp(smoothVerticalVelocity, rb.velocity.y, 0.2f);
        anim.SetFloat("VerticalVelocity", smoothVerticalVelocity);

        if (!wasGrounded && isGrounded)
        {
            anim.SetBool("IsJumping", false);
            canDash = true;
        }

        if (rb.velocity.y < 0f)
        {
            if (leftTrail != null) leftTrail.emitting = false;
            if (rightTrail != null) rightTrail.emitting = false;
        }

        wasGrounded = isGrounded;
    }

    #endregion

    #region Movement & Physics

    /// <summary>
    /// Calcula la velocidad de la plataforma a partir de su delta de posición desde el último FixedUpdate.
    /// </summary>
    private void UpdatePlatformVelocity()
    {
        if (isOnPlatform && currentPlatform != null)
        {
            Vector3 delta = currentPlatform.position - platformLastPosition;
            platformVelocity = delta / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            platformLastPosition = currentPlatform.position;
        }
        else
        {
            platformVelocity = Vector3.zero;
        }
    }

    private void ApplyBetterJumpPhysics()
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0)
        {
            if (jumpCount == 2 && !Input.GetButton("Jump"))
            {
                rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (jumpCount == 1)
            {
                rb.velocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.fixedDeltaTime;
            }
        }
    }

    private void HandleMovement()
    {
        // Dirección relativa a la cámara (solo horizontal)
        Vector3 camForward = mainCam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = mainCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDir = (camForward * inputZ + camRight * inputX).normalized;

        // Velocidad horizontal del jugador
        Vector3 horizontalVel = moveDir * moveSpeed;

        // Sumar velocidad horizontal de la plataforma para mantener la posición relativa
        if (isOnPlatform)
        {
            Vector3 platVel = platformVelocity;
            platVel.y = 0f;
            horizontalVel += platVel;
        }

        // Mantener componente vertical
        Vector3 newVel = horizontalVel;
        newVel.y = rb.velocity.y;

        // Aplicar al Rigidbody
        rb.velocity = newVel;

        // Rotación suave hacia el movimiento
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * 10f));
        }
    }

    private void PerformJump()
    {
        jumpCount++;
        anim.SetBool("IsJumping", true);

        if (jumpCount == 1)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else if (jumpCount == 2)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jetpackForce, ForceMode.Impulse);

            if (leftTrail != null)
            {
                leftTrail.Clear();
                leftTrail.emitting = true;
            }
            if (rightTrail != null)
            {
                rightTrail.Clear();
                rightTrail.emitting = true;
            }
        }
    }

    private void CheckGrounded()
    {
        Debug.DrawRay(groundCheck.position, Down * groundCheckDistance, Color.red);

        bool wasGroundedBefore = isGrounded;
        isGrounded = Physics.Raycast(groundCheck.position, Down, groundCheckDistance, groundLayer);

        // Resetear saltos al aterrizar
        if (isGrounded && !wasGroundedBefore && jumpCount > 0)
        {
            jumpCount = 0;

            if (leftTrail != null)
            {
                leftTrail.Clear();
                leftTrail.emitting = false;
            }
            if (rightTrail != null)
            {
                rightTrail.Clear();
                rightTrail.emitting = false;
            }
        }
    }

    #endregion

    #region Dash

    private void StartDash()
    {
        isDashing = true;
        canDash = false;
        dashTimer = 0f;

        anim.SetBool("IsDashing", true);

        Vector3 dashDir = transform.forward;
        dashDir.y = 0f;
        dashDir.Normalize();

        rb.velocity = dashDir * dashForce;

        if (MultiParticlePool.Instance != null && !string.IsNullOrEmpty(dashTrailKey))
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.6f;
            dashTrailInstance = MultiParticlePool.Instance.PlayParticle(dashTrailKey, spawnPos, Quaternion.identity);
            if (dashTrailInstance != null)
                dashTrailInstance.transform.SetParent(transform);
        }
    }

    private void EndDash()
    {
        isDashing = false;
        anim.SetBool("IsDashing", false);

        if (dashTrailInstance != null)
        {
            dashTrailInstance.transform.SetParent(null);
            if (MultiParticlePool.Instance != null && !string.IsNullOrEmpty(dashTrailKey))
                MultiParticlePool.Instance.ReturnToPool(dashTrailKey, dashTrailInstance);

            dashTrailInstance = null;
        }
    }

    #endregion

    #region Platform Collision

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("MovingPlatform")) return;

        // Determinar si el contacto tiene una normal que apunta hacia arriba (estamos encima)
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isOnPlatform = true;
                currentPlatform = collision.transform;
                platformLastPosition = currentPlatform.position;
                break;
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("MovingPlatform") || isOnPlatform) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isOnPlatform = true;
                currentPlatform = collision.transform;
                platformLastPosition = currentPlatform.position;
                break;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.CompareTag("MovingPlatform")) return;

        if (currentPlatform == collision.transform)
        {
            isOnPlatform = false;
            currentPlatform = null;
            platformVelocity = Vector3.zero;
        }
    }

    #endregion
}