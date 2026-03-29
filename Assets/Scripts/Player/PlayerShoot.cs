using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
   
    public ObjectPoolBullet bulletPool;
    public Transform shootPoint; 

    private Animator anim;

    /// <summary>
    /// Inicializa referencias necesarias al comenzar la escena.
    /// Cachea el componente Animator para disparar las animaciones desde la entrada.
    /// </summary>
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Lee la entrada del jugador cada frame y dispara la animación de ataque
    /// cuando se pulsa el botón de disparo configurado en Input Manager ("Fire1").
    /// </summary>
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            anim.SetTrigger("Shoot");
        }
    }

    /// <summary>
    /// Método invocado (por la animación) para instanciar/obtener una bala
    /// desde el pool en el punto de disparo especificado y con la rotación
    /// del transform de disparo.
    /// </summary>
    public void Shoot()
    {
        Bullet bullet = bulletPool.GetObjectAt(shootPoint.position, shootPoint.rotation);
    }


}
