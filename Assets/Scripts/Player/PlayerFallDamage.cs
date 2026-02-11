using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFallDamage : MonoBehaviour
{
    [Header("Configuración de Caída")]
    [SerializeField] private float lethalImpactVelocity = 18f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private PlayerDeathHandler deathHandler;
    private Rigidbody playerRigidbody;


    //Guardamos la referencia para no hacer GetComponent en el momento del choque.
    private void Awake()
    {
        deathHandler = GetComponent<PlayerDeathHandler>();
        playerRigidbody = GetComponent<Rigidbody>();
    }

    //el metodo se activa al momento de una colisión.
    //más eficiente que chequear "isGrounded" todo el tiempo para daño.
    private void OnCollisionEnter(Collision collision)
    {
        if (deathHandler.isDying) return;


        //Solo nos importa si el golpe viene principalmente de ABAJO (eje Y).
        //collision.contacts[0].normal es la dirección de la superficie con la que chocamos.
        //Si la normal apunta hacia arriba (aprox 1), es suelo.
        if (collision.contactCount > 0 && collision.contacts[0].normal.y < 0.5f)
        {
            return; //Chocamos contra una pared o techo, no es caída.
        }

        // se calcula automáticamente con qué fuerza chocaron los dos objetos.
        // No necesitamos guardar "previousVelocity" manualmente.
        float impactForce = collision.relativeVelocity.magnitude;

        // Verificamos si la fuerza vertical fue suficiente para matar.
        // Usamos Math.Abs para ignorar el signo, aunque la magnitud siempre es positiva.
        if (impactForce >= lethalImpactVelocity)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[PlayerFallDamage] Impacto letal detectado! Fuerza: {impactForce}");
            }

            deathHandler.Die();
        }
        else if (showDebugLogs && impactForce > 5f)
        {
            // Log informativo para ayudarte a calibrar el número lethalImpactVelocity
            Debug.Log($"[PlayerFallDamage] Aterrizaje seguro. Fuerza: {impactForce}");
        }
    }
}

