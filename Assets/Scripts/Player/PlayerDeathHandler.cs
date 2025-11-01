using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerDeathHandler : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement movement;
    private Rigidbody rb;
    private Collider playerCollider; // NUEVO

    public bool isDying { get; private set; } = false;
    private bool cancelledByRespawn;
    private Coroutine deathCoroutine;

    private Vector3 deathPosition;
    private bool deathPositionSaved = false;

    [Header("Configuración")]
    [SerializeField] private float deathAnimationDuration = 1.2f;
    [SerializeField] private float fadeStartDelay = 0.6f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>(); // NUEVO
    }

    private string LogTime() =>
        $"[{System.DateTime.Now:HH:mm:ss.fff}] [Scene:{SceneManager.GetActiveScene().name}]";

    public void Die()
    {
        if (isDying)
        {
            Debug.Log($"{LogTime()} PlayerDeathHandler: Die() ignorado porque ya está muriendo");
            return;
        }

        isDying = true;
        cancelledByRespawn = false;

        deathPosition = transform.position;
        deathPositionSaved = true;

        // NUEVO: Desactivar collider para evitar más colisiones
        if (playerCollider != null)
            playerCollider.enabled = false;

        // Desactivar movimiento y físicas
        movement.enabled = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Activar animación de muerte
        animator.SetTrigger("Die");
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsJumping", false);
        animator.SetBool("IsDashing", false);

        Debug.Log($"{LogTime()} PlayerDeathHandler: Die() -> Posición de muerte guardada: {deathPosition}");

        deathCoroutine = StartCoroutine(DeathSequence());
    }

    private void LateUpdate()
    {
        if (isDying && deathPositionSaved)
        {
            transform.position = deathPosition;
        }
    }

    public void ResetAfterRespawn()
    {
        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
            deathCoroutine = null;
        }

        cancelledByRespawn = true;
        isDying = false;
        deathPositionSaved = false;

        // NUEVO: Reactivar collider
        if (playerCollider != null)
            playerCollider.enabled = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (movement != null)
            movement.enabled = true;

        Debug.Log($"{LogTime()} PlayerDeathHandler: ResetAfterRespawn ejecutado — coroutine cancelada y flags reseteados");

        animator.ResetTrigger("Die");
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsJumping", false);
        animator.SetBool("IsDashing", false);
        animator.Play("Idle", 0, 0f);
        animator.Update(0f);
    }

    private IEnumerator DeathSequence()
    {
        Debug.Log($"{LogTime()} PlayerDeathHandler: DeathSequence iniciada");

        yield return new WaitForSeconds(fadeStartDelay);

        Debug.Log($"{LogTime()} PlayerDeathHandler: mitad de animación — fadeStartDelay alcanzado");

        yield return new WaitForSeconds(deathAnimationDuration - fadeStartDelay);

        if (cancelledByRespawn)
        {
            Debug.Log($"{LogTime()} PlayerDeathHandler: DeathSequence cancelada por respawn, no se resta vida");
            yield break;
        }

        Debug.Log($"{LogTime()} PlayerDeathHandler: Notificando LifeManager -> LoseLife(1)");
        LifeManager.Instance?.LoseLife(1);
    }

}