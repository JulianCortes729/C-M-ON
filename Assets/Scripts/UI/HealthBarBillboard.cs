using UnityEngine;

public class HealthBarBillboard : MonoBehaviour
{
    public Transform cam;

    void Start()
    {
        // Si no asignas cámara manual, busca la principal automáticamente
        if (cam == null) cam = Camera.main.transform;
    }

    // LateUpdate ocurre después de que el jefe se haya movido/rotado
    void LateUpdate()
    {
        // Hacemos que la barra mire en la misma dirección que la cámara
        transform.LookAt(transform.position + cam.forward);
    }
}
