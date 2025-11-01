using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementPlatform : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 2f;
    public float amplitud = 5f;
    private Vector3 posicionInicial;

    public enum EjeMovimiento { X, Y, Z }
    public EjeMovimiento eje = EjeMovimiento.X;

    [Header("Rotación")]
    public bool enableRotation = false;
    public Vector3 rotationSpeed; // grados por segundo

    private Rigidbody rb;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private Rigidbody playerRb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // Movida por script
        posicionInicial = rb.position;
        lastPosition = rb.position;
        lastRotation = rb.rotation;
    }

    void FixedUpdate()
    {
        // --- Movimiento ---
        float desplazamiento = Mathf.Sin(Time.time * speed) * amplitud;
        Vector3 nuevaPosicion = posicionInicial;

        switch (eje)
        {
            case EjeMovimiento.X: nuevaPosicion.x += desplazamiento; break;
            case EjeMovimiento.Y: nuevaPosicion.y += desplazamiento; break;
            case EjeMovimiento.Z: nuevaPosicion.z += desplazamiento; break;
        }

        rb.MovePosition(nuevaPosicion);

        // --- Rotación ---
        if (enableRotation)
        {
            Quaternion deltaRot = Quaternion.Euler(rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * deltaRot);
        }

        // --- Aplicar movimiento de plataforma al jugador ---
        if (playerRb != null)
        {
            // Diferencia de posición
            Vector3 delta = rb.position - lastPosition;

            // Diferencia de rotación
            Quaternion rotDelta = rb.rotation * Quaternion.Inverse(lastRotation);
            Vector3 posRelativa = playerRb.position - rb.position;
            Vector3 rotada = rotDelta * posRelativa;

            // Nueva posición del jugador (se mueve con la plataforma)
            Vector3 nuevaPosJugador = rb.position + rotada + delta;

            playerRb.MovePosition(nuevaPosJugador);
        }

        // Guardar estado actual para siguiente frame
        lastPosition = rb.position;
        lastRotation = rb.rotation;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerRb = other.gameObject.GetComponent<Rigidbody>();
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerRb = null;
        }
    }

}
