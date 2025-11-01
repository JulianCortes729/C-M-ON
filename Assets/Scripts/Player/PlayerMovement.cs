using UnityEngine;

/// <summary>
/// Controla el movimiento, salto y animaciones del jugador.
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
    [SerializeField] private float gravityMultiplier = 2.5f; // Multiplicador de gravedad al caer
    [SerializeField] private float fallMultiplier = 3f; // Gravedad extra cuando cae
    [SerializeField] private float lowJumpMultiplier = 2f; // Gravedad cuando sueltas el botón de salto

    [Header("Dash")]
    [SerializeField] private float dashForce = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private GameObject dashTrailPrefab; // Prefab con TrailRenderer

    [Header("Double Jump Jetpack")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float jetpackForce = 7f;
    [SerializeField] private GameObject handTrailPrefab;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

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

    /// <summary>
    /// Inicializa componentes y configura el Rigidbody.
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Evita que el Rigidbody se vuelque
        anim = GetComponent<Animator>();
        mainCam = Camera.main;

        // Crear trails de manos una sola vez
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

    /// <summary>
    /// Captura la entrada del usuario y actualiza los parámetros de animación.
    /// </summary>
    void Update()
    {
        inputX = Input.GetAxis("Horizontal"); // A-D
        inputZ = Input.GetAxis("Vertical");   // W-S

        // Actualiza animación de correr
        anim.SetBool("IsRunning", inputX != 0 || inputZ != 0);

        // Detecta salto - permite saltar solo si hay saltos disponibles
        if (Input.GetButtonDown("Jump"))
        {
            Debug.Log("TECLA SALTO PRESIONADA | jumpCount actual: " + jumpCount + " | maxJumps: " + maxJumps + " | Puede saltar: " + (jumpCount < maxJumps));

            if (jumpCount < maxJumps)
            {
                PerformJump();
            }
            else
            {
                Debug.Log("✗ NO PUEDE SALTAR - Ya usó todos los saltos");
            }
        }

        // Detecta Dash en el aire
        if (Input.GetKeyDown(KeyCode.C) && !isGrounded && canDash && !isDashing)
        {
            StartDash();
        }
    }

    /// <summary>
    /// Actualiza la física, movimiento y animaciones del jugador.
    /// </summary>
    void FixedUpdate()
    {
        CheckGrounded();

        // Si está dashing, ignora movimiento normal y controla duración
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

        // Suaviza el parámetro de velocidad vertical para el Blend Tree
        smoothVerticalVelocity = Mathf.Lerp(smoothVerticalVelocity, rb.velocity.y, 0.2f);
        anim.SetFloat("VerticalVelocity", smoothVerticalVelocity);

        // Desactiva animación de salto solo al aterrizar y permite dash en el próximo salto
        if (!wasGrounded && isGrounded)
        {
            anim.SetBool("IsJumping", false);
            canDash = true;
        }

        // Apagar trails cuando empieza a caer
        if (rb.velocity.y < 0f)
        {
            if (leftTrail != null) leftTrail.emitting = false;
            if (rightTrail != null) rightTrail.emitting = false;
        }

        wasGrounded = isGrounded;
    }

    /// <summary>
    /// Aplica física de salto más realista con gravedad variable.
    /// </summary>
    private void ApplyBetterJumpPhysics()
    {
        if (rb.velocity.y < 0)
        {
            // Caída más rápida cuando va hacia abajo
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0)
        {
            if (jumpCount == 2 && !Input.GetButton("Jump"))
            {
                // Salto más corto si sueltas el botón SOLO en el segundo salto (jetpack)
                rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (jumpCount == 1)
            {
                // Primer salto: aplica gravedad normal pero ligeramente aumentada para que no flote
                rb.velocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.fixedDeltaTime;
            }
        }
    }

    /// <summary>
    /// Mueve al jugador según la entrada y la orientación de la cámara.
    /// </summary>
    private void HandleMovement()
    {
        // Calcula dirección relativa a la cámara
        Vector3 camForward = mainCam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = mainCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDir = (camForward * inputZ + camRight * inputX).normalized;

        // Aplica velocidad al Rigidbody
        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = rb.velocity.y; // Mantiene la velocidad vertical actual
        rb.velocity = velocity;

        // Rota el jugador hacia la dirección de movimiento
        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * 10f));
        }
    }

    /// <summary>
    /// Ejecuta el salto según el número de saltos realizados.
    /// </summary>
    private void PerformJump()
    {
        jumpCount++;
        anim.SetBool("IsJumping", true);

        Debug.Log("SALTO EJECUTADO: " + jumpCount + " de " + maxJumps + " | isGrounded: " + isGrounded);

        if (jumpCount == 1)
        {
            // Primer salto normal
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            Debug.Log("→ Primer salto normal ejecutado");
        }
        else if (jumpCount == 2)
        {
            // Segundo salto tipo jetpack
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(Vector3.up * jetpackForce, ForceMode.Impulse);
            Debug.Log("→ Segundo salto JETPACK ejecutado");

            // Activar trails
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

    /// <summary>
    /// Verifica si el jugador está en el suelo usando un Raycast.
    /// </summary>
    private void CheckGrounded()
    {
        Debug.DrawRay(groundCheck.position, Down * groundCheckDistance, Color.red);
        bool wasGroundedBefore = isGrounded;
        isGrounded = Physics.Raycast(groundCheck.position, Down, groundCheckDistance, groundLayer);

        // Solo resetea saltos cuando ATERRIZA (transición de aire a suelo)
        if (isGrounded && !wasGroundedBefore && jumpCount > 0)
        {
            Debug.Log("✓ ATERRIZÓ - Saltos reseteados de " + jumpCount + " a 0");
            jumpCount = 0;

            // Limpiar restos de trails
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

    /// <summary>
    /// Inicia el Dash en el aire y crea el rastro visual.
    /// </summary>
    private void StartDash()
    {
        isDashing = true;
        canDash = false;
        dashTimer = 0f;

        // Activa animación de Dash
        anim.SetBool("IsDashing", true);

        // Calcula dirección de dash (hacia adelante según el Player)
        Vector3 dashDir = transform.forward;
        dashDir.y = 0;
        dashDir.Normalize();

        rb.velocity = dashDir * dashForce;

        // Instancia el rastro visual si hay prefab
        if (dashTrailPrefab != null)
        {
            dashTrailInstance = Instantiate(dashTrailPrefab, transform.position + Vector3.up * 0.6f, Quaternion.identity, transform);
        }
    }

    /// <summary>
    /// Finaliza el Dash y elimina el rastro visual.
    /// </summary>
    private void EndDash()
    {
        isDashing = false;

        // Desactiva animación de Dash
        anim.SetBool("IsDashing", false);

        // Destruye el rastro visual
        if (dashTrailInstance != null)
        {
            Destroy(dashTrailInstance);
        }
    }
}