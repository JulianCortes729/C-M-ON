using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightTriggerLayerChange : MonoBehaviour
{
    [SerializeField] private GameObject building;
    [SerializeField] private string newLayer = "Graund";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            building.layer = LayerMask.NameToLayer(newLayer);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            building.layer = LayerMask.NameToLayer("CameraCollision");
    }
}
