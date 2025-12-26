using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class MainMenuPlayer : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce1 = 5f;
    [SerializeField] private float jumpForce2 = 8f;
    [SerializeField] private float delayBetweenJumps = 0.2f;
    [SerializeField] private float delayBeforeSceneLoad = 1.0f;

    [Header("References (Igual que en PlayerMovement)")]
    [SerializeField] private GameObject handTrailPrefab; // Arrastra aquí tu prefab de trail
    [SerializeField] private Transform leftHand;         // Arrastra el hueso de la mano izquierda
    [SerializeField] private Transform rightHand;        // Arrastra el hueso de la mano derecha

    private TrailRenderer leftTrail;
    private TrailRenderer rightTrail;

    private Rigidbody rb;
    private Animator anim;
    private bool isActionStarted = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        // 1. Configuración inicial del personaje
        anim.SetBool("IsRunning", false);
        anim.SetBool("IsGrounded", true);

        // 2. CREAR LOS TRAILS (Instanciarlos)
        // Esto es lo que faltaba: crear los efectos en las manos al iniciar
        if (handTrailPrefab != null)
        {
            if (leftHand != null)
            {
                GameObject lTrailObj = Instantiate(handTrailPrefab, leftHand.position, leftHand.rotation, leftHand);
                leftTrail = lTrailObj.GetComponent<TrailRenderer>();
                if (leftTrail != null) leftTrail.emitting = false; // Apagado al inicio
            }

            if (rightHand != null)
            {
                GameObject rTrailObj = Instantiate(handTrailPrefab, rightHand.position, rightHand.rotation, rightHand);
                rightTrail = rTrailObj.GetComponent<TrailRenderer>();
                if (rightTrail != null) rightTrail.emitting = false; // Apagado al inicio
            }
        }
    }

    public void PlayStartSequence(string sceneName)
    {
        if (isActionStarted) return;
        isActionStarted = true;

        StartCoroutine(DoubleJumpRoutine(sceneName));
    }

    private IEnumerator DoubleJumpRoutine(string sceneName)
    {
        // --- SALTO 1 ---
        anim.SetBool("IsJumping", true);
        anim.SetBool("IsGrounded", false);
        rb.velocity = Vector3.zero;
        rb.AddForce(Vector3.up * jumpForce1, ForceMode.Impulse);

        yield return new WaitForSeconds(delayBetweenJumps);

        // --- SALTO 2 (Jetpack) ---
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce2, ForceMode.Impulse);

        // ENCENDER TRAILS
        if (leftTrail != null) { leftTrail.Clear(); leftTrail.emitting = true; }
        if (rightTrail != null) { rightTrail.Clear(); rightTrail.emitting = true; }

        yield return new WaitForSeconds(delayBeforeSceneLoad);

        // --- CAMBIO DE ESCENA ---
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(sceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}