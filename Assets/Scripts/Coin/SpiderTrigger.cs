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
        if (other.CompareTag("Spider"))
        {
            parentExplosive?.OnSpiderRangeEntered();
        }
    }
}
