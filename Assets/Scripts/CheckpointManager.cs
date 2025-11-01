using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Vector3 currentCheckpoint;
    private Quaternion currentCheckpointRotation;
    private bool hasCheckpoint = false;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetStartPosition(Vector3 position, Quaternion rotation)
    {
        startPosition = position;
        startRotation = rotation;
        Debug.Log("Start position guardada: " + position);
    }

    public void SaveCheckpoint(Vector3 position, Quaternion rotation)
    {
        currentCheckpoint = position;
        currentCheckpointRotation = rotation;
        hasCheckpoint = true;
        Debug.Log("Checkpoint guardado: " + position);
    }

    public void GetRespawnPoint(out Vector3 position, out Quaternion rotation)
    {
        if (hasCheckpoint)
        {
            position = currentCheckpoint;
            rotation = currentCheckpointRotation;
            Debug.Log("Respawneando en checkpoint: " + position);
        }
        else
        {
            position = startPosition;
            rotation = startRotation;
            Debug.Log("Respawneando en inicio: " + position);
        }
    }

    public void ClearCheckpoints()
    {
        hasCheckpoint = false;
        Debug.Log("Checkpoints limpiados");
    }
}
