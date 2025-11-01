using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrapRise : MonoBehaviour
{
    public Animator spikesAnimator;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            spikesAnimator.SetTrigger("Activate");
        }
    }
}
