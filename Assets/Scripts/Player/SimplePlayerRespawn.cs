using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePlayerRespawn : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // Guardar posición inicial
        CheckpointManager.Instance.SetStartPosition(transform.position, transform.rotation);
    }

    public void Respawn()
    {
        // Desactivar física temporalmente
        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Obtener posición de respawn
        Vector3 spawnPos;
        Quaternion spawnRot;
        CheckpointManager.Instance.GetRespawnPoint(out spawnPos, out spawnRot);

        // Teleportar
        transform.SetPositionAndRotation(spawnPos, spawnRot);

        // Esperar un frame y reactivar física
        Invoke(nameof(ReactivatePhysics), 0.1f);
    }

    private void ReactivatePhysics()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (col != null) col.enabled = true;

        Debug.Log("Física reactivada en: " + transform.position);
    }
}
