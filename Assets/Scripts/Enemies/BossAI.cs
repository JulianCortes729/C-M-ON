using UnityEngine;
using System.Collections;

public class BossAI : MonoBehaviour
{
    [Header("Referencias")]
    public BossJumpAttack jumpAttack;
    public BossShootingAttack shootingAttack;
    public Transform player;
    public BossHealth bossHealth; // <--- Agrega esta referencia para mostrar la barra

    [Header("Configuración de IA")]
    public float actionCooldown = 2.5f;
    public float minJumpDistance = 4f;

    // Nueva variable para controlar si la pelea empezó
    private bool isBattleActive = false;

    void Start()
    {
        // --- CAMBIO IMPORTANTE ---
        // Ya NO iniciamos el BehaviorLoop aquí.
        // El jefe se queda quieto esperando.
        isBattleActive = false;
    }

    // --- NUEVA FUNCIÓN PÚBLICA ---
    // Esta función la llama el Trigger de la azotea
    public void ActivateBoss()
    {
        if (isBattleActive) return; // Si ya está peleando, ignorar

        isBattleActive = true;

        // 1. Mostrar la barra de vida (si estaba oculta)
        if (bossHealth != null)
        {
            bossHealth.ShowHealthBar();
        }

        // 2. (Opcional) Hacer un rugido o animación de entrada aquí
        // animator.SetTrigger("Roar");
        // Invoke("StartLoop", 2.0f); // Esperar a que termine el rugido

        // 3. Iniciar el bucle de ataques
        StartCoroutine(BehaviorLoop());
    }

    // Esta función detiene todo inmediatamente
    public void DeactivateBoss()
    {
        isBattleActive = false; // Detiene la condición del While
        StopAllCoroutines();    // Detiene el bucle de ataques inmediatamente

        // Opcional: Si el jefe estaba disparando o saltando, detenemos esos scripts específicos también
        if (jumpAttack != null) StopCoroutine("JumpRoutine"); // O resetear variables del script de salto
        if (shootingAttack != null) shootingAttack.StopAttack(); // Si tienes el método público de parar disparo

        // Volver a Idle en el Animator para que no se quede congelado en pose de ataque
        // animator.Play("Idle"); 

        Debug.Log("Boss: Volviendo a dormir...");
    }

    IEnumerator BehaviorLoop()
    {
        // Pequeña espera al inicio para que el jugador se prepare
        yield return new WaitForSeconds(1f);

        while (isBattleActive && player != null) // Bucle infinito (mientras el jefe viva)
        {
            if (player == null) yield break; // Si el player murió, dejamos de atacar

            // 1. MEDIR DISTANCIA
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // 2. DECIDIR ATAQUE
            if (distanceToPlayer < minJumpDistance)
            {
                // -- ESTRATEGIA DE CERCA: APLASTAR --
                // Forzamos al jefe a mirar al jugador antes de saltar si es necesario
                LookAtPlayer();

                // Ejecutamos el salto
                jumpAttack.TriggerAttack();

                // Calculamos cuánto esperar (duración del salto + descanso)
                // Asumimos que el salto dura unos 2s en total (ida + vuelta)
                yield return new WaitForSeconds(2.0f);
            }
            else
            {
                // -- ESTRATEGIA DE LEJOS: DISPARAR --
                shootingAttack.TriggerShoot();

                // Esperamos lo que dura la ráfaga (aprox) antes de descansar
                yield return new WaitForSeconds(1.5f);
            }

            // 3. TIEMPO DE RECUPERACIÓN (Cooldown)
            // El jefe camina o respira un momento antes del siguiente ataque
            yield return new WaitForSeconds(actionCooldown);
        }
    }

    void LookAtPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);
    }
}