using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Configuración de Bala")]
    public float speed = 20f;
    public float lifeTime = 2f;

    [Tooltip("Escribe aquí el Tag de a quién debe matar esta bala (ej: 'Player' o 'Enemy')")]
    public string targetTag = "Player";

    private float timer;
    private TrailRenderer trail;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
    }

    void OnEnable()
    {
        timer = 0f;
        // Reiniciar trail para el Pooling
        if (trail)
        {
            trail.Clear();
            trail.emitting = true;
        }
    }

    void OnDisable()
    {
        if (trail)
        {
            trail.emitting = false;
            trail.Clear();
        }
    }

    void Update()
    {
        // Mover hacia adelante
        transform.position += transform.forward * speed * Time.deltaTime;

        // Lógica de vida útil
        timer += Time.deltaTime;
        if (timer >= lifeTime)
            Deactivate();
    }

    // Usamos Trigger para que la bala atraviese y no empuje físicas
    void OnTriggerEnter(Collider other)
    {
        // 1. Si golpea al OBJETIVO (Player o Enemigo según configures)
        if (other.CompareTag(targetTag))
        {
            // Si el objetivo es el Player, ejecutamos su muerte
            if (targetTag == "Player")
            {
                other.GetComponentInParent<PlayerDeathHandler>()?.Die();
            }

            // 2. SI LE PEGAMOS AL ENEMIGO (JEFE)
            else if (targetTag == "Boss")
            {
                // Buscamos el script de vida en el objeto o sus padres
                BossHealth bossHealth = other.GetComponentInParent<BossHealth>();
                if (bossHealth != null)
                {
                    bossHealth.TakeDamage(3); // <--- DAÑO QUE HACE LA BALA
                }
            }

            Deactivate();
        }
        
    }
    void Deactivate()
    {
        gameObject.SetActive(false);
    }
}