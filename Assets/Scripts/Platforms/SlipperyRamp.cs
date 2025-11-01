using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlipperyRamp : MonoBehaviour
{
    [SerializeField] private float slideForce = 10f; // fuerza de resbalada
    

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody rb = collision.rigidbody;
            if (rb != null)
            {
                // Calcula la dirección paralela a la rampa
                Vector3 slideDir = Vector3.ProjectOnPlane(Physics.gravity, collision.contacts[0].normal).normalized;

                // Aplica fuerza para que resbale en esa dirección
                rb.AddForce(slideDir * slideForce, ForceMode.Acceleration);
            }
        }
    }
}
