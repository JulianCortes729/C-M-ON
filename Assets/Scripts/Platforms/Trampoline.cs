using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline : MonoBehaviour
{

    [SerializeField] private float bounceForce = 15f; // Altura fija del salto

    private void OnCollisionEnter(Collision collision)
    {
        // Solo reacciona si el objeto tiene el tag "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody rb = collision.rigidbody;
            if (rb != null && rb.velocity.y <= 0) // Solo si venía cayendo
            {
                // Resetea la velocidad vertical y aplica el impulso
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
            }
        }
    }
}
