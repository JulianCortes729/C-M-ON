using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderTrigger : MonoBehaviour
{
    private ExplosiveItem parentExplosive;
    private void Awake()
    {
        parentExplosive = GetComponentInParent<ExplosiveItem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger detectó: {other.name} con tag: {other.tag}");

        if (other.CompareTag("Spider"))
        {
            Debug.Log("¡Araña detectada! Activando explosivo");
            parentExplosive?.OnSpiderRangeEntered();
        }
    }
}
