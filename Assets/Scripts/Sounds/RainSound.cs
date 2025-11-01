using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(AudioSource))]
public class RainSound : MonoBehaviour
{
    private ParticleSystem particles;
    private AudioSource audioSource;

    void Awake()
    {
        particles = GetComponent<ParticleSystem>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Si la lluvia está activa y el sonido no está sonando → reproducir
        if (particles.isPlaying && !audioSource.isPlaying)
        {
            audioSource.Play();
        }

        // Si la lluvia se detuvo y el sonido sigue → parar
        if (!particles.isPlaying && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
