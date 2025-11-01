using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagePlatform : MonoBehaviour
{
    public enum DamageMode
    {
        Always,      // Quita vida al tocar por cualquier lado
        FromAbove    // Solo quita vida si el jugador cae desde arriba
    }

    [Header("Configuración")]
    //[SerializeField] private int damage = 1;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool useTrigger = false;
    [SerializeField] private DamageMode damageMode = DamageMode.Always;

    private LifeManager lifeManager;

    private void Start()
    {
        lifeManager = LifeManager.Instance;
    }

    private void ApplyDamageTo(GameObject player, Vector3 contactNormal)
    {
        if (!player.CompareTag(playerTag)) return;

        // Si el modo es "FromAbove", solo daña si el player viene de arriba
        if (damageMode == DamageMode.FromAbove && contactNormal.y > -0.5f) return;

        SimplePlayerRespawn respawn = player.GetComponent<SimplePlayerRespawn>();
        if (lifeManager == null || respawn == null) return;

        player.GetComponent<PlayerDeathHandler>()?.Die();

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        if (!collision.gameObject.CompareTag(playerTag)) return;

        // Tomamos la normal promedio del contacto (dirección de la colisión)
        Vector3 avgNormal = Vector3.zero;
        foreach (var contact in collision.contacts)
            avgNormal += contact.normal;
        avgNormal.Normalize();

        ApplyDamageTo(collision.gameObject, avgNormal);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        ApplyDamageTo(other.gameObject, Vector3.down); // En triggers no hay normal, simulamos caída
    }
}
