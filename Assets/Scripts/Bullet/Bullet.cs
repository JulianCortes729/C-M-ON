using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    [Header("Configuración de Bala")]
    public float speed = 20f;
    public float lifeTime = 2f;

    [Tooltip("Escribe aquí el Tag de a quién debe matar esta bala (ej: 'Player' o 'Enemy')")]
    public string targetTag = "Player";

    private float timer;
    private TrailRenderer trail;

    private IObjectPool<Bullet> pool;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
    }

    /// <summary>
    /// Inicialización temprana del componente; se cachea el TrailRenderer si existe.
    /// </summary>

    void OnEnable()
    {
        timer = 0f;
        
        if (trail)
        {
            trail.Clear();
            trail.emitting = true;
        }
    }

    /// <summary>
    /// Se ejecuta cuando la bala se activa: reinicia el temporizador de vida y
    /// reactiva el rastro visual si existe.
    /// </summary>

    void OnDisable()
    {
        if (trail)
        {
            trail.emitting = false;
            trail.Clear();
        }
    }

    /// <summary>
    /// Se ejecuta cuando la bala se desactiva: detiene y limpia el rastro visual.
    /// </summary>

    void Update()
    {
        
        transform.position += transform.forward * speed * Time.deltaTime;

        
        timer += Time.deltaTime;
        if (timer >= lifeTime)
            Deactivate();
    }

    /// <summary>
    /// Movimiento sencillo de la bala y control de su tiempo de vida.
    /// Cuando expira el tiempo, la bala se devuelve al pool.
    /// </summary>

    
    void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag(targetTag))
        {
            
            if (targetTag == "Player")
            {
                other.GetComponentInParent<PlayerDeathHandler>()?.Die();
            }

            
            else if (targetTag == "Boss")
            {
                
                BossHealth bossHealth = other.GetComponentInParent<BossHealth>();
                if (bossHealth != null)
                {
                    bossHealth.TakeDamage(3);
                }
            }

            Deactivate();
        }
        
    }

    /// <summary>
    /// Handle de colisiones: si la bala impacta al target correcto, aplica el efecto
    /// (daño o muerte) y se devuelve al pool.
    /// </summary>

    public void AssignPool(IObjectPool<Bullet> objectPool)
    {
        pool = objectPool;
    } 

    public void Deactivate()
    {
        pool.Release(this);
    }
}