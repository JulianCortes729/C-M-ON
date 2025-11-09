using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controla el comportamiento de una moneda recolectable.
/// - Detecta colisión con el jugador.
/// - Oculta la moneda y reproduce un efecto de partículas usando ParticlePool.
/// - Dispara el evento estático OnCoinCollected.
/// - Complejidad O(1) por recolección.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    // Evento global que notifica a otros sistemas (CoinManager, UI, etc.)
    public static UnityEvent OnCoinCollected = new UnityEvent();

    [Header("Efectos (usando MultiParticlePool)")]
    [Tooltip("Clave del efecto normal en el MultiParticlePool (ej: 'coin')")]
    [SerializeField] private string normalEffectKey = "coin";

    [Tooltip("Clave del efecto explosivo en el MultiParticlePool (ej: 'explosion')")]
    [SerializeField] private string explosionEffectKey = "coinExplosion";

    [Tooltip("Pool de partículas a usar al recoger la moneda.")]
    [SerializeField] private MultiParticlePool pool;

    [Header("Ajustes")]
    [Tooltip("Si true, se omite el efecto visual y se destruye la moneda inmediatamente.")]
    [SerializeField] private bool skipEffect = false;

    // Cache de componentes (O(1))
    private Collider _collider;
    private Renderer[] _renderers;
    private ExplosiveItem explosive;

    private void Awake()
    {
        // Cachear componentes para evitar búsquedas costosas.
        _collider = GetComponent<Collider>();
        _renderers = GetComponents<Renderer>();
        explosive = GetComponent<ExplosiveItem>();

        // cachea el pool una sola vez (opcional si hay muchos objetos)
        pool = FindObjectOfType<MultiParticlePool>();
    }

    /// <summary>
    /// Detecta al jugador y ejecuta la recolección.
    /// - Oculta visuales.
    /// - Llama al evento global.
    /// - Reproduce partículas desde el pool.
    /// - Destruye la moneda.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        bool isExplosive = explosive != null && explosive.IsExplosive;

        //explosiva -> solo explosion + muerte del jugador
        if (isExplosive)
        {
            pool?.PlayParticle(explosionEffectKey, transform.position, Quaternion.identity);
            MultiAudioPool.Instance?.Play("coinExplosion", transform.position);
            other.GetComponent<PlayerDeathHandler>()?.Die();
            Destroy(gameObject);
            return;
        }

        //normal -> comportamiento clásico
        _collider.enabled = false;

        foreach (var r in _renderers)
            if (r != null) r.enabled = false;

        OnCoinCollected?.Invoke();

        if (skipEffect)
        {
            Destroy(gameObject);
            return;
        }

        pool?.PlayParticle(normalEffectKey, transform.position, Quaternion.identity);
        MultiAudioPool.Instance?.Play("coin", transform.position);
        Destroy(gameObject);
    }
}
