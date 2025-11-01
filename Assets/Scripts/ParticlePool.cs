using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de pool genérico para ParticleSystems.
/// - Mantiene una lista de instancias pre-creadas.
/// - Reutiliza instancias inactivas en O(1) por búsqueda.
/// - Evita Instantiate/Destroy durante el juego (optimización de GC).
/// - Compatible con cualquier prefab de ParticleSystem.
/// 
/// Complejidad: 
///   - Inicialización: O(n) (una sola vez al inicio).
///   - Solicitud de efecto (Spawn): O(1).
/// </summary>
public class ParticlePool : MonoBehaviour
{
    [Header("Configuración del Pool")]
    [Tooltip("Prefab del ParticleSystem que se va a usar.")]
    [SerializeField] private ParticleSystem particlePrefab;

    [Tooltip("Cantidad inicial de partículas preinstanciadas.")]
    [SerializeField] private int poolSize = 10;

    [Tooltip("Si se habilita, el pool se expandirá automáticamente si no hay disponibles.")]
    [SerializeField] private bool allowExpand = true;

    // Cola de instancias disponibles para reuso (O(1) en enqueue/dequeue)
    private readonly Queue<ParticleSystem> availableParticles = new Queue<ParticleSystem>();

    // Lista de todas las instancias creadas (solo informativo o debug)
    private readonly List<ParticleSystem> allParticles = new List<ParticleSystem>();

    private Transform _parentContainer;

    private void Awake()
    {
        if (particlePrefab == null)
        {
            Debug.LogError("[ParticlePool] No se asignó prefab de partículas.", this);
            enabled = false;
            return;
        }

        // Crear contenedor para mantener jerarquía limpia
        _parentContainer = new GameObject($"[Pool_{particlePrefab.name}]").transform;
        _parentContainer.SetParent(transform);

        // Pre-instanciar pool (O(n) solo una vez)
        for (int i = 0; i < poolSize; i++)
            CreateNewInstance();
    }

    /// <summary>
    /// Obtiene un sistema de partículas del pool y lo activa en la posición/rotación dadas.
    /// </summary>
    /// <param name="position">Posición donde reproducir el efecto.</param>
    /// <param name="rotation">Rotación deseada.</param>
    public void PlayParticle(Vector3 position, Quaternion rotation)
    {
        ParticleSystem ps = GetAvailableParticle();
        if (ps == null)
        {
            Debug.LogWarning("[ParticlePool] No hay partículas disponibles y la expansión está deshabilitada.", this);
            return;
        }

        // Colocar y reproducir
        Transform t = ps.transform;
        t.position = position;
        t.rotation = rotation;

        ps.gameObject.SetActive(true);
        ps.Play();

        // Programar devolución al pool cuando termine
        StartCoroutine(ReturnWhenFinished(ps));
    }

    /// <summary>
    /// Obtiene una instancia libre del pool en O(1).
    /// Si no hay, crea una nueva si allowExpand = true.
    /// </summary>
    private ParticleSystem GetAvailableParticle()
    {
        if (availableParticles.Count > 0)
            return availableParticles.Dequeue();

        if (allowExpand)
            return CreateNewInstance();

        return null;
    }

    /// <summary>
    /// Crea una nueva instancia del prefab y la añade al pool (inactiva).
    /// </summary>
    private ParticleSystem CreateNewInstance()
    {
        ParticleSystem ps = Instantiate(particlePrefab, _parentContainer);
        ps.gameObject.SetActive(false);

        availableParticles.Enqueue(ps);
        allParticles.Add(ps);
        return ps;
    }

    /// <summary>
    /// Espera a que el ParticleSystem termine de emitir para devolverlo al pool.
    /// </summary>
    private System.Collections.IEnumerator ReturnWhenFinished(ParticleSystem ps)
    {
        var main = ps.main;

        // Esperar duración completa + tiempo de vida máximo de partículas
        float duration = main.duration + main.startLifetime.constantMax;
        yield return new WaitForSeconds(duration + 0.05f);

        ps.Stop();
        ps.Clear();
        ps.gameObject.SetActive(false);
        availableParticles.Enqueue(ps);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (particlePrefab != null)
            UnityEditor.Handles.Label(transform.position, $"Pool: {particlePrefab.name} ({availableParticles.Count}/{allParticles.Count})");
    }
#endif
}
