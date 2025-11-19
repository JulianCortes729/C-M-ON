using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCameraStep : MonoBehaviour
{
    public Transform targetCamera;
    public float threshold = 15f;   // distancia antes de reubicar
    public float fixedHeight = 110f; // altura en Y para la lluvia

    void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 cameraPos = targetCamera.position;

        // Distancia solo en XZ (plano horizontal)
        Vector2 rainXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 camXZ = new Vector2(cameraPos.x, cameraPos.z);

        if (Vector2.Distance(rainXZ, camXZ) > threshold)
        {
            transform.position = new Vector3(cameraPos.x, fixedHeight, cameraPos.z);
        }
    }

}
