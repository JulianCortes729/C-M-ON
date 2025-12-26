using UnityEngine;

public class BossActivator : MonoBehaviour
{
    [Header("Referencias")]
    public BossAI bossAI;   // Arrastra aquí al Jefe
    public BossHealth bossHealth; // Arrastra aquí el script de vida del jefe
    public GameObject wallToClose; // Opcional: Una pared invisible para encerrar al player

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("¡Player entró en la zona! Jefe Activado.");

            // 1. Activar lógica del Jefe
            if (bossAI != null)
            {
                bossAI.ActivateBoss();
            }

            // 2. Mostrar Barra de Vida
            if (bossHealth != null)
            {
                bossHealth.ShowHealthBar();
            }

            // (Opcional) Cerrar pared
            if (wallToClose != null) wallToClose.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("¡Player salió de la zona! Jefe Desactivado.");

            // 1. Desactivar lógica del Jefe
            if (bossAI != null)
            {
                bossAI.DeactivateBoss(); // <--- Necesitamos crear esta función en BossAI
            }

            // 2. Ocultar Barra de Vida
            if (bossHealth != null)
            {
                bossHealth.HideHealthBar(); // <--- Necesitamos crear esta función en BossHealth
            }

            // (Opcional) Abrir pared si el player huye
            if (wallToClose != null) wallToClose.SetActive(false);
        }
    }
}
