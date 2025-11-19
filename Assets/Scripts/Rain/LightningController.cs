using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningController : MonoBehaviour
{
    [Header("Luz del rayo")]
    public Light lightningLight;
    public float maxIntensity = 6f;

    [Header("Truenos")]
    public AudioSource thunderAudio;
    public AudioClip[] thunderClips;

    [Header("Frecuencia de rayos")]
    public float minDelay = 5f;
    public float maxDelay = 15f;

    [Header("Distancia de la tormenta")]
    public float stormDistance = 300f; // metros aprox.
    private const float soundSpeed = 343f; // m/s (velocidad del sonido)

    void Start()
    {
        lightningLight.intensity = 0f;
        StartCoroutine(StormRoutine());
    }

    IEnumerator StormRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
            StartCoroutine(LightningFlash());

            float thunderDelay = stormDistance / soundSpeed;
            yield return new WaitForSeconds(thunderDelay);

            PlayRandomThunder();
        }
    }

    IEnumerator LightningFlash()
    {
        // 1er destello breve
        lightningLight.intensity = maxIntensity;
        yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
        lightningLight.intensity = 0f;

        // Pequeño parpadeo extra
        yield return new WaitForSeconds(Random.Range(0.02f, 0.06f));
        lightningLight.intensity = maxIntensity * Random.Range(0.4f, 0.8f);
        yield return new WaitForSeconds(Random.Range(0.04f, 0.1f));
        lightningLight.intensity = 0f;
    }

    void PlayRandomThunder()
    {
        if (thunderClips.Length == 0) return;
        thunderAudio.PlayOneShot(thunderClips[Random.Range(0, thunderClips.Length)]);
    }
}
