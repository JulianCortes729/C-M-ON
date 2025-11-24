using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 2f;

    private float timer;

    private TrailRenderer trail;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
    }

    void OnEnable()
    {
        timer = 0f;

        //Esto evita el "retroceso" del trail cuando sale del pool
        trail.Clear();
        trail.emitting = true;

    }

    void OnDisable()
    {
        // Apagar trail ANTES de mover la bala
        trail.emitting = false;
        trail.Clear();
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
            gameObject.SetActive(false);
    }

    void OnCollisionEnter(Collision other)
    {
        gameObject.SetActive(false);
    }
}
