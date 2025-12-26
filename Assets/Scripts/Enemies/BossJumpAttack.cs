using UnityEngine;
using System.Collections;

public class BossJumpAttack : MonoBehaviour
{
    [Header("Configuración del Salto")]
    public Transform playerTarget;      // Arrastra aquí al Player
    public float jumpHeight = 5f;       // Altura del arco del salto
    public float jumpDuration = 0.8f;     // Cuánto tarda en llegar al jugador
    public float waitOnGround = 1f;     // Tiempo que espera antes de volver

    [Header("Animaciones")]
    public Animator animator;
    public string jumpTriggerName = "Jump"; // Nombre del trigger en tu Animator

    private Vector3 originalPosition;
    private bool isAttacking = false;   // Para saber si está en medio del ataque
    private bool isFalling = false;     // Para saber si está cayendo (momento letal)

    [Header("Tiempos de Animación")]
    public float jumpWindUpTime = 0.1f; // Tiempo que tarda la animación en "despegar" (agacharse)
    public float landRecoveryTime = 0.25f; // Tiempo que se queda en pose de "aterrizaje"

    // Agrega esta variable para referencia
    [Header("Referencias Extra")]
    public BossShootingAttack shootingScript; // <--- REFERENCIA DEL OTRO SCRIPT 
    public string aimBoolName = "IsAiming";   // El mismo nombre que en el otro script

    void Start()
    {
        originalPosition = transform.position; // Guardamos donde "vive" el jefe
    }

    // Llama a esta función para iniciar el ataque (desde tu script de IA o un botón de prueba)
    public void TriggerAttack()
    {
        if (!isAttacking)
        {
            StartCoroutine(JumpRoutine());
        }
    }

    IEnumerator JumpRoutine()
    {
        isAttacking = true;

        // --- CORRECCIÓN DE ANIMACIÓN ---
        // 1. Forzamos a que la variable de apuntar sea FALSA
        if (animator) animator.SetBool(aimBoolName, false);
        // 2. (Opcional pero recomendado) Detenemos el script de disparo si estaba corriendo
        if (shootingScript) shootingScript.StopAimingImmediate();
        // Esperamos un frame para que el Animator procese el cambio de bool
        yield return null;


        originalPosition = transform.position;
        Vector3 targetPosition = playerTarget.position;

        // --- FASE 1: ANTICIPACIÓN ---
        // Disparamos la animación
        if (animator) animator.SetTrigger(jumpTriggerName);

        // IMPORTANTE: Esperamos aquí lo que tarda la animación en hacer el gesto de "tomar impulso"
        // El jefe NO se mueve todavía, solo se anima.
        yield return new WaitForSeconds(jumpWindUpTime);

        // Preparamos físicas
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // --- FASE 2: EL SALTO (VUELO) ---
        // Ahora sí empezamos a moverlo. Ajusta 'jumpDuration' para que coincida con la parte de "aire" de tu animación
        yield return StartCoroutine(MoveParabola(transform.position, targetPosition, jumpDuration));

        // --- FASE 3: ATERRIZAJE E IMPACTO ---
        isFalling = false;

        // Aquí podrías disparar un Trigger de "Land" si tienes animación de impacto específica
        // if(animator) animator.SetTrigger("Land"); 

        // Esperamos un momento en el suelo (Recuperación del impacto)
        yield return new WaitForSeconds(waitOnGround);

        // --- FASE 4: SALTO DE VUELTA ---
        // Opcional: ¿Quieres anticipación para el salto de vuelta también?
        if (animator) animator.SetTrigger(jumpTriggerName);
        yield return new WaitForSeconds(jumpWindUpTime); // Usamos el mismo tiempo de impulso

        yield return StartCoroutine(MoveParabola(transform.position, originalPosition, jumpDuration));

        if (rb) rb.isKinematic = false;
        isAttacking = false;
    }

    // Esta función matemática crea el arco perfecto
    IEnumerator MoveParabola(Vector3 start, Vector3 end, float duration)
    {
        float time = 0;

        while (time < 1)
        {
            time += Time.deltaTime / duration;

            // Interpolación lineal para moverse de A a B
            //Vector3 linearPos = Vector3.Lerp(start, end, time);

            // Usa Lerp con "Ease In" para que empiece lento y acelere al caer:
            float acceleratedTime = time * time; // Cuadrático
            Vector3 linearPos = Vector3.Lerp(start, end, time); // Mantenemos movimiento lineal en X/Z
                                                                // Solo la altura cambia su velocidad visual


            // Añadimos altura usando una curva Seno (Sube y baja suavemente)
            // Si time es 0.5 (mitad del salto), Sin(PI * 0.5) es 1 (altura máxima)
            float heightCurve = Mathf.Sin(time * Mathf.PI) * jumpHeight;

            transform.position = new Vector3(linearPos.x, linearPos.y + heightCurve, linearPos.z);

            // Detectamos si estamos en la segunda mitad del salto (cayendo) para activar el daño
            if (time > 0.5f) isFalling = true;
            else isFalling = false;

            yield return null; // Esperar al siguiente frame
        }
    }

    // LÓGICA DE COLISIÓN PARA MATAR AL PLAYER
    private void OnCollisionEnter(Collision collision)
    {
        // Solo matamos si estamos atacando, cayendo (fase final del salto) y tocamos al player
        if (isAttacking && isFalling && collision.gameObject.CompareTag("Player"))
        {
            // TU CÓDIGO DE MUERTE
            collision.gameObject.GetComponent<PlayerDeathHandler>()?.Die();

            // Opcional: Detener el ataque o rebotar si mata al jugador
            Debug.Log("¡Jugador Aplastado!");
        }
    }
}
