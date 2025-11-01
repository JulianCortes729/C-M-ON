using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Life : MonoBehaviour
{
    public static UnityEvent<int> OnLifeCollected = new UnityEvent<int>();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Destroy(gameObject);

        OnLifeCollected?.Invoke(1);
    }
}
