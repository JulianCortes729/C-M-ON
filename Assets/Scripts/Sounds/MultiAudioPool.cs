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
        public string key;          // Identificador (ej: "coin", "explosion")
        public AudioClip clip;      // Clip asociado
        public float volume = 1f;   // Volumen individual
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
        source.Play();
    }
}
