using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de pooling genérico para reutilizar partículas y efectos visuales.
/// Evita Instantiate/Destroy para mejorar el rendimiento.
/// </summary>
public class MultiParticlePool : MonoBehaviour
{
    public static MultiParticlePool Instance { get; private set; }

    [System.Serializable]
    public class ParticleEntry
    {
        [Tooltip("Identificador único para este tipo de partícula")]
        public string key;

        [Tooltip("Prefab del sistema de partículas o efecto visual")]
        public GameObject prefab;

        [Tooltip("Cantidad de instancias pre-creadas en el pool")]
        [Range(1, 50)]
        public int poolSize = 5;

        [Tooltip("Si es true, se crearán más instancias si el pool se queda sin objetos")]
        public bool expandable = true;
    }

    [Header("Configuración del Pool")]
    [SerializeField] private List<ParticleEntry> particleEntries = new List<ParticleEntry>();

    // Estructura de datos para cada pool individual
    private class Pool
    {
        public GameObject prefab;
        public Queue<GameObject> available;
        public List<GameObject> inUse;
        public bool expandable;
        public Transform parent;

        public Pool(GameObject prefab, int size, bool expandable, Transform parent)
        {
            this.prefab = prefab;
            this.expandable = expandable;
            this.parent = parent;
            this.available = new Queue<GameObject>(size);
            this.inUse = new List<GameObject>(size);
        }
    }

    private Dictionary<string, Pool> pools;

    #region Initialization

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Crea todos los pools definidos en particleEntries.
    /// Time Complexity: O(n*m) donde n es el número de entries y m el poolSize de cada uno
    /// </summary>
    private void InitializePools()
    {
        pools = new Dictionary<string, Pool>();

        foreach (var entry in particleEntries)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning($"[MultiParticlePool] Entry '{entry.key}' tiene prefab nulo. Saltando.");
                continue;
            }

            if (string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning($"[MultiParticlePool] Entry con prefab '{entry.prefab.name}' no tiene key. Saltando.");
                continue;
            }

            if (pools.ContainsKey(entry.key))
            {
                Debug.LogWarning($"[MultiParticlePool] Key duplicada '{entry.key}'. Saltando.");
                continue;
            }

            // Crear contenedor para este pool
            GameObject poolContainer = new GameObject($"Pool_{entry.key}");
            poolContainer.transform.SetParent(transform);

            // Crear el pool
            Pool pool = new Pool(entry.prefab, entry.poolSize, entry.expandable, poolContainer.transform);

            // Pre-instanciar objetos
            for (int i = 0; i < entry.poolSize; i++)
            {
                GameObject obj = CreateNewInstance(pool, i);
                pool.available.Enqueue(obj);
            }

            pools[entry.key] = pool;
            Debug.Log($"[MultiParticlePool] Pool '{entry.key}' inicializado con {entry.poolSize} objetos.");
        }
    }

    /// <summary>
    /// Crea una nueva instancia para el pool.
    /// Time Complexity: O(1)
    /// </summary>
    private GameObject CreateNewInstance(Pool pool, int index)
    {
        GameObject obj = Instantiate(pool.prefab, pool.parent);
        obj.name = $"{pool.prefab.name}_{index}";
        obj.SetActive(false);
        return obj;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Reproduce una partícula desde el pool en la posición y rotación especificadas.
    /// Time Complexity: O(1) amortizado
    /// </summary>
    /// <param name="key">Identificador del tipo de partícula</param>
    /// <param name="position">Posición donde reproducir</param>
    /// <param name="rotation">Rotación del efecto</param>
    /// <returns>GameObject del efecto instanciado, o null si falla</returns>
    public GameObject PlayParticle(string key, Vector3 position, Quaternion rotation)
    {
        if (!pools.TryGetValue(key, out Pool pool))
        {
            Debug.LogWarning($"[MultiParticlePool] No existe pool con key '{key}'");
            return null;
        }

        GameObject obj = GetFromPool(pool);
        if (obj == null)
        {
            Debug.LogWarning($"[MultiParticlePool] No se pudo obtener objeto del pool '{key}'");
            return null;
        }

        // Configurar posición y rotación
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // Si tiene ParticleSystem, reproducirlo
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear();
            ps.Play();

            // Auto-retornar al pool cuando termine
            StartCoroutine(ReturnToPoolWhenDone(key, obj, ps));
        }
        else
        {
            // Si no es un ParticleSystem, buscar TrailRenderer
            TrailRenderer trail = obj.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }
        }

        return obj;
    }

    /// <summary>
    /// Obtiene un objeto del pool (versión sin rotación, usa Quaternion.identity).
    /// Time Complexity: O(1) amortizado
    /// </summary>
    public GameObject PlayParticle(string key, Vector3 position)
    {
        return PlayParticle(key, position, Quaternion.identity);
    }

    /// <summary>
    /// Devuelve manualmente un objeto al pool.
    /// Útil para efectos que no son ParticleSystems y necesitas controlar su duración.
    /// Time Complexity: O(1)
    /// </summary>
    /// <param name="key">Identificador del pool</param>
    /// <param name="obj">Objeto a devolver</param>
    public void ReturnToPool(string key, GameObject obj)
    {
        if (obj == null) return;

        if (!pools.TryGetValue(key, out Pool pool))
        {
            Debug.LogWarning($"[MultiParticlePool] No existe pool con key '{key}'");
            return;
        }

        if (!pool.inUse.Contains(obj))
        {
            Debug.LogWarning($"[MultiParticlePool] Objeto '{obj.name}' no está en uso en el pool '{key}'");
            return;
        }

        // Limpiar trails si los tiene
        TrailRenderer trail = obj.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
        }

        obj.SetActive(false);
        obj.transform.SetParent(pool.parent);

        pool.inUse.Remove(obj);
        pool.available.Enqueue(obj);
    }

    /// <summary>
    /// Limpia todos los pools (útil al cambiar de escena).
    /// Time Complexity: O(n) donde n es la cantidad total de objetos en todos los pools
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var kvp in pools)
        {
            Pool pool = kvp.Value;

            // Retornar todos los objetos en uso
            for (int i = pool.inUse.Count - 1; i >= 0; i--)
            {
                GameObject obj = pool.inUse[i];
                if (obj != null)
                {
                    obj.SetActive(false);
                    pool.available.Enqueue(obj);
                }
            }
            pool.inUse.Clear();
        }
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Obtiene un objeto disponible del pool, expandiéndolo si es necesario.
    /// Time Complexity: O(1) amortizado
    /// </summary>
    private GameObject GetFromPool(Pool pool)
    {
        GameObject obj = null;

        // Intentar obtener uno disponible
        if (pool.available.Count > 0)
        {
            obj = pool.available.Dequeue();
        }
        // Si no hay disponibles y el pool es expandible, crear uno nuevo
        else if (pool.expandable)
        {
            int newIndex = pool.inUse.Count + pool.available.Count;
            obj = CreateNewInstance(pool, newIndex);
            Debug.Log($"[MultiParticlePool] Pool expandido. Nuevo objeto creado: {obj.name}");
        }
        else
        {
            Debug.LogWarning($"[MultiParticlePool] Pool agotado y no es expandible. Considera aumentar poolSize.");
            return null;
        }

        pool.inUse.Add(obj);
        return obj;
    }

    /// <summary>
    /// Corrutina que retorna automáticamente un ParticleSystem al pool cuando termina.
    /// Time Complexity: O(1)
    /// </summary>
    private System.Collections.IEnumerator ReturnToPoolWhenDone(string key, GameObject obj, ParticleSystem ps)
    {
        // Esperar a que todas las partículas hayan terminado
        yield return new WaitWhile(() => ps.IsAlive(true));

        ReturnToPool(key, obj);
    }

    #endregion

    #region Debug & Utility

    /// <summary>
    /// Muestra estadísticas de uso de los pools en consola.
    /// </summary>
    [ContextMenu("Show Pool Statistics")]
    public void ShowPoolStatistics()
    {
        Debug.Log("=== MultiParticlePool Statistics ===");
        foreach (var kvp in pools)
        {
            Pool pool = kvp.Value;
            int total = pool.available.Count + pool.inUse.Count;
            Debug.Log($"Pool '{kvp.Key}': Total={total} | Disponibles={pool.available.Count} | En uso={pool.inUse.Count}");
        }
    }

    #endregion
}