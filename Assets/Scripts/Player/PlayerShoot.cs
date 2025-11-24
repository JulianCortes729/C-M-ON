using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
   
    public ObjectPool bulletPool;
    public Transform shootPoint; // Donde sale la bala

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            anim.SetTrigger("Shoot");
        }
    }

    // Este lo llama la animación
    public void Shoot()
    {
        GameObject bullet = bulletPool.GetObjectAt(shootPoint.position,shootPoint.rotation);
    }


}
