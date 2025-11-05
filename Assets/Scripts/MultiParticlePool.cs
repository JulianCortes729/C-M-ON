using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool que maneja múltiples tipos de partículas, identificados por un nombre o clave.
/// Permite reproducir efectos diferentes sin crear nuevos objetos.
/// </summary>
public class MultiParticlePool : MonoBehaviour
{
    [System.Serializable]
    public class PoolEntry
    {
        public string key;                   // Ej: "coin", "explosion"
        public ParticleSystem prefab;
        public int poolSize = 5;
    }

    [SerializeField] private bool allowExpand = true;
    [SerializeField] private List<PoolEntry> particleEntries = new List<PoolEntry>();

    private readonly Dictionary<string, Queue<ParticleSystem>> pools = new();
    private readonly Dictionary<string, Transform> poolParents = new();

    private void Awake()
    {
        foreach (var entry in particleEntries)
        {
            if (entry.prefab == null || string.IsNullOrEmpty(entry.key)) continue;

            var queue = new Queue<ParticleSystem>();
            var parent = new GameObject($"[Pool_{entry.key}]").transform;
            parent.SetParent(transform);
            poolParents[entry.key] = parent;

            for (int i = 0; i < entry.poolSize; i++)
                queue.Enqueue(CreateNewInstance(entry.prefab, parent));

            pools[entry.key] = queue;
        }
    }

    public void PlayParticle(string key, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogWarning($"[MultiParticlePool] No existe pool con clave '{key}'.");
            return;
        }

        var ps = GetAvailableParticle(key);
        if (ps == null)
        {
            Debug.LogWarning($"[MultiParticlePool] No hay partículas disponibles para '{key}'.");
            return;
        }

        var t = ps.transform;
        t.position = position;
        t.rotation = rotation;

        ps.gameObject.SetActive(true);
        ps.Play();
        StartCoroutine(ReturnWhenFinished(key, ps));
    }

    private ParticleSystem GetAvailableParticle(string key)
    {
        var queue = pools[key];
        if (queue.Count > 0)
            return queue.Dequeue();

        if (!allowExpand) return null;
        var prefab = particleEntries.Find(e => e.key == key)?.prefab;
        return prefab != null ? CreateNewInstance(prefab, poolParents[key]) : null;
    }

    private ParticleSystem CreateNewInstance(ParticleSystem prefab, Transform parent)
    {
        var ps = Instantiate(prefab, parent);
        ps.gameObject.SetActive(false);
        return ps;
    }

    private IEnumerator ReturnWhenFinished(string key, ParticleSystem ps)
    {
        var main = ps.main;
        float duration = main.duration + main.startLifetime.constantMax;
        yield return new WaitForSeconds(duration + 0.05f);

        ps.Stop();
        ps.Clear();
        ps.gameObject.SetActive(false);
        pools[key].Enqueue(ps);
    }
}
