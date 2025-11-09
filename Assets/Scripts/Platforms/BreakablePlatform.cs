using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakablePlatform : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float explosionForce = 5f;
    [SerializeField] private float destroyDelay = 3f;

    private bool broken = false;

    private void OnTriggerEnter(Collider other)
    {
        if (broken) return;
        if (!other.CompareTag(playerTag)) return;
        MultiAudioPool.Instance?.Play("breakPlatform", transform.position);
        Break();
    }

    public void Break()
    {

        // El primer hijo suele ser la plataforma entera
        Transform fullPlatform = transform.GetChild(0);
        Destroy(fullPlatform.gameObject);

        // Activar física de los fragmentos
        for (int i = 1; i < transform.childCount; i++)
        {
            Rigidbody rb = transform.GetChild(i).GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;

                // Fuerza aleatoria sutil para dispersión sin empujar al jugador
                Vector3 randomDir = Random.insideUnitSphere * 0.5f;
                rb.AddForce(randomDir * explosionForce);
            }
        }


        Destroy(gameObject, destroyDelay);
    }
}
