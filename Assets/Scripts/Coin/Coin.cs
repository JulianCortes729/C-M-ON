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

    [Header("Efecto de partículas")]
    [Tooltip("Pool de partículas a usar al recoger la moneda.")]
    [SerializeField] private ParticlePool particlePool;

    [Header("Ajustes")]
    [Tooltip("Si true, se omite el efecto visual y se destruye la moneda inmediatamente.")]
    [SerializeField] private bool skipEffect = false;

    // Cache de componentes (O(1))
    private Collider _collider;
    private Renderer[] _renderers;

    private void Awake()
    {
        // Cachear componentes para evitar búsquedas costosas.
        _collider = GetComponent<Collider>();
        _renderers = GetComponents<Renderer>();
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
        // Solo reaccionar ante el jugador
        if (!other.CompareTag("Player"))
            return;

        // Evitar recolección doble
        if (_collider != null)
            _collider.enabled = false;

        // Ocultar visualmente la moneda
        foreach (var r in _renderers)
            if (r != null) r.enabled = false;

        // Disparar evento global
        OnCoinCollected?.Invoke();

        // Si se salta el efecto, destruir inmediatamente
        if (skipEffect)
        {
            Destroy(gameObject);
            return;
        }

        // Si existe el pool, reproducir efecto (O(1))
        if (particlePool != null)
            particlePool.PlayParticle(transform.position, Quaternion.identity);

        // Destruir moneda (sin esperar efecto)
        Destroy(gameObject);
    }
}
