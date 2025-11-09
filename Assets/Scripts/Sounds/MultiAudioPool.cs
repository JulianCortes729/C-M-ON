using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema centralizado para reproducir sonidos reutilizando AudioSources.
/// Similar a MultiParticlePool, pero para audio.
/// </summary>
public class MultiAudioPool : MonoBehaviour
{
    public static MultiAudioPool Instance;

    [System.Serializable]
    public class AudioEntry
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;

        [Header("Ajustes 3D")]
        [Range(0f, 1f)] public float spatialBlend = 1f; // 1 = 3D, 0 = 2D
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        public float minDistance = 1f;
        public float maxDistance = 20f;
        [Range(0f, 5f)] public float dopplerLevel = 0.5f;
        [Range(0f, 180f)] public float spread = 0f;
    }

    [SerializeField] private List<AudioEntry> audioEntries = new List<AudioEntry>();
    private Dictionary<string, AudioEntry> audioMap;
    private AudioSource[] sources;
    private int currentSource = 0;

    [Header("Pool Settings")]
    [Tooltip("Cantidad de AudioSources preinstanciados.")]
    [SerializeField] private int poolSize = 10;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        audioMap = new Dictionary<string, AudioEntry>();
        foreach (var entry in audioEntries)
            audioMap[entry.key] = entry;

        sources = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.parent = transform;
            sources[i] = go.AddComponent<AudioSource>();
            sources[i].spatialBlend = 1f; // 3D por defecto
            sources[i].playOnAwake = false;
        }
    }

    public void Play(string key, Vector3 position)
    {
        if (!audioMap.TryGetValue(key, out var entry))
            return;

        var source = sources[currentSource];
        currentSource = (currentSource + 1) % sources.Length;

        source.transform.position = position;
        source.clip = entry.clip;
        source.volume = entry.volume;

        // Aplicar ajustes 3D personalizados
        source.spatialBlend = entry.spatialBlend;
        source.rolloffMode = entry.rolloffMode;
        source.minDistance = entry.minDistance;
        source.maxDistance = entry.maxDistance;
        source.dopplerLevel = entry.dopplerLevel;
        source.spread = entry.spread;

        source.Play();
    }
}
