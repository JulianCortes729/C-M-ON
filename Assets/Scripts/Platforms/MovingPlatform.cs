using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class MovingPlatform : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Puntos por donde se moverá. Los null serán ignorados automáticamente.")]
    public Transform[] waypoints;

    [Tooltip("Velocidad de movimiento en unidades por segundo.")]
    public float speed = 2f;

    [Tooltip("Si vuelve al inicio (loop) o hace ping-pong entre extremos.")]
    public bool loop = true;

    [Header("Configuración")]
    [Tooltip("Distancia a la que se considera que la plataforma ha alcanzado el waypoint.")]
    public float reachThreshold = 0.1f;

    // Estado interno
    private readonly List<Transform> validWaypoints = new List<Transform>();
    private int currentWaypoint = 0;
    private bool movingForward = true;
    private float reachThresholdSqr;

    // Rigidbody de la plataforma para moverla con física (evita jitter)
    private Rigidbody rb;

    void OnValidate()
    {
        // Evita valores negativos y mantiene consistencia en el editor
        speed = Mathf.Max(0f, speed);
        reachThreshold = Mathf.Max(0f, reachThreshold);
    }

    void Start()
    {
        // Preparar lista de waypoints válidos (ignorando nulls)
        validWaypoints.Clear();
        if (waypoints != null)
        {
            foreach (var t in waypoints)
            {
                if (t != null) validWaypoints.Add(t);
            }
        }

        // Cachear umbral al cuadrado para evitar sqrt en cada frame
        reachThresholdSqr = reachThreshold * reachThreshold;

        // Obtener Rigidbody y configurar kinematic para control seguro desde script
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // controlamos movimiento manualmente con MovePosition
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Si no hay waypoints válidos, desactivar el componente para ahorrar CPU
        if (validWaypoints.Count == 0)
        {
            enabled = false;
            return;
        }

        // Asegurar índice inicial válido
        currentWaypoint = Mathf.Clamp(currentWaypoint, 0, validWaypoints.Count - 1);

        // Asegurar posición inicial exacta en caso de diferencia
        rb.position = transform.position;
    }

    void FixedUpdate()
    {
        // Si solo hay un waypoint, nada que hacer (puede usarse como punto fijo)
        int count = validWaypoints.Count;
        if (count <= 1) return;

        Vector3 targetPos = validWaypoints[currentWaypoint].position;

        // Mover usando Rigidbody.MovePosition para que el motor de física lo procese correctamente
        Vector3 newPos = Vector3.MoveTowards(rb.position, targetPos, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // Usar distancia al cuadrado para eficiencia
        float sqrDist = (rb.position - targetPos).sqrMagnitude;
        if (sqrDist <= reachThresholdSqr)
        {
            AdvanceWaypoint(count);
        }
    }

    /// <summary>
    /// Avanza el índice de waypoint según el modo (loop o ping-pong).
    /// Mantiene currentWaypoint dentro de límites válidos.
    /// </summary>
    private void AdvanceWaypoint(int count)
    {
        if (loop)
        {
            currentWaypoint = (currentWaypoint + 1) % count;
            return;
        }

        // Ping-pong
        if (movingForward)
        {
            currentWaypoint++;
            if (currentWaypoint >= count)
            {
                // Llegamos al final: retroceder
                currentWaypoint = Mathf.Max(0, count - 2);
                movingForward = false;
            }
        }
        else
        {
            currentWaypoint--;
            if (currentWaypoint < 0)
            {
                // Llegamos al inicio: avanzar
                currentWaypoint = Mathf.Min(1, count - 1);
                movingForward = true;
            }
        }

        // Clamp por seguridad
        currentWaypoint = Mathf.Clamp(currentWaypoint, 0, Mathf.Max(0, count - 1));
    }
}