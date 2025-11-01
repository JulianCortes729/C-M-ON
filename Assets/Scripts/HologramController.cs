using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HologramController : MonoBehaviour
{

    [Header("Referencia al holograma")]
    public GameObject holograma; // arrastrar aquí el GameObject del holograma

    private void Start()
    {
        // Asegurarse de que el holograma esté activo al inicio
        if (holograma != null)
            holograma.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && holograma != null)
        {
            holograma.SetActive(false); // jugador entra -> apagar holograma
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && holograma != null)
        {
            holograma.SetActive(true); // jugador sale -> prender holograma
        }
    }

}
