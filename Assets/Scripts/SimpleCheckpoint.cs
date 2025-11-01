using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCheckpoint : MonoBehaviour
{
    [SerializeField] private bool destroyOnCollect = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CheckpointManager.Instance.SaveCheckpoint(transform.position, transform.rotation);

            if (destroyOnCollect)
            {
                Destroy(gameObject);
            }
        }
    }
}
