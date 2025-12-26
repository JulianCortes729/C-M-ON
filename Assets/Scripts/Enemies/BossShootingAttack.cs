using UnityEngine;
using System.Collections;

public class BossShootingAttack : MonoBehaviour
{
    [Header("Pool y Referencias")]
    public ObjectPool bossBulletPool;
    public Transform firePoint;
    public Transform playerTarget;
    public Animator animator;

    [Header("Configuración Ráfaga")]
    public int shotsPerBurst = 3;
    public float fireRate = 0.5f;
    public float targetHeightOffset = 1.2f;
    public float rotationSpeed = 15f; // AUMENTADO: Gira más rápido

    [Header("Animación")]
    public string aimBoolName = "IsAiming";
    public string shootTriggerName = "Shoot";
    public float aimDuration = 1.0f;
    public float shootDelay = 0.2f; // Tiempo exacto del "fogonazo" en la animación

    private bool isShooting = false;

    public void TriggerShoot()
    {
        if (!isShooting) StartCoroutine(ShootSequence());
    }

    IEnumerator ShootSequence()
    {
        isShooting = true;

        // 1. PREPARAR
        if (animator) animator.SetBool(aimBoolName, true);

        // Esperamos apuntando (Rotando agresivamente)
        yield return StartCoroutine(WaitAndRotate(aimDuration));

        // 2. DISPARAR RÁFAGA
        for (int i = 0; i < shotsPerBurst; i++)
        {
            // Disparar Trigger de animación
            if (animator) animator.SetTrigger(shootTriggerName);

            // IMPORTANTE: Seguimos rotando mientras esperamos el momento exacto que sale la bala
            yield return StartCoroutine(WaitAndRotate(shootDelay));

            // Lógica de bala
            if (bossBulletPool != null)
            {
                Vector3 targetPoint = playerTarget.position + Vector3.up * targetHeightOffset;
                Vector3 aimDirection = (targetPoint - firePoint.position).normalized;
                Quaternion bulletRotation = Quaternion.LookRotation(aimDirection);

                bossBulletPool.GetObjectAt(firePoint.position, bulletRotation);
            }

            // Esperar para el siguiente tiro (siguiendo al player)
            float timeToNextShot = fireRate - shootDelay;
            if (timeToNextShot > 0)
            {
                yield return StartCoroutine(WaitAndRotate(timeToNextShot));
            }
        }

        // 3. RECUPERACIÓN
        yield return new WaitForSeconds(0.5f); // Aquí ya no rota, se queda quieto tras disparar

        if (animator) animator.SetBool(aimBoolName, false);
        isShooting = false;
    }

    // --- NUEVA FUNCIÓN MÁGICA ---
    // En lugar de detener el script, cuenta el tiempo MIENTRAS rota al jefe
    IEnumerator WaitAndRotate(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            RotateBossYBody();
            yield return null; // Espera un frame y sigue
        }
    }

    void RotateBossYBody()
    {
        if (playerTarget == null) return;

        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0; // Mantenerlo derecho

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // Slerp más rápido (rotationSpeed) para que no se quede atrás
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // Función pública para forzar el apagado de la animación (usada por el salto)
    public void StopAimingImmediate()
    {
        isShooting = false;
        StopAllCoroutines();
        if (animator) animator.SetBool(aimBoolName, false);
    }


    public void StopAttack()
    {
        // 1. Detiene inmediatamente la ráfaga, el apuntado y las esperas
        StopAllCoroutines();

        // 2. Resetea variables lógicas
        isShooting = false;

        // 3. Limpia la animación (para que no se quede con el brazo levantado si muere apuntando)
        if (animator)
        {
            animator.SetBool(aimBoolName, false);
            // Opcional: Si quieres que deje de disparar visualmente al instante
            // animator.ResetTrigger(shootTriggerName); 
        }
    }
}