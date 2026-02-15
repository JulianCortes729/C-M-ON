using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpikeTrapRise : MonoBehaviour
{
    private bool isActivated = false;

    [Header("Configuracion de tiempos")]
    [SerializeField] private float activeDuration;
    [SerializeField] private float riseDelay;
    private WaitForSeconds waitTimer;
    private WaitForSeconds waitDelay;

    

    [Header("Referencias Visuales y Físicas")]
    [SerializeField] private BoxCollider damageCollider;
    [SerializeField] private Animator spikesAnimator;


    [Header("Audio Keys")]
    [SerializeField] private string warnAudioKey = "TrapWarn";
    [SerializeField] private string attackAudioKey = "TrapAttack";
    [SerializeField] private string retractAudioKey = "TrapRetract";

    void Start()
    {
        waitTimer = new WaitForSeconds(activeDuration);
        waitDelay = new WaitForSeconds(riseDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivateTrap(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryActivateTrap(other);
    }
    
    void TryActivateTrap(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            StartCoroutine(ActivateSpikes());
        }
    }

    IEnumerator ActivateSpikes()
    {
        isActivated = true;
        spikesAnimator.SetTrigger("Warn");
        MultiAudioPool.Instance?.Play(warnAudioKey, transform.position);

        yield return waitDelay;

        spikesAnimator.SetBool("IsActivated", isActivated);
        MultiAudioPool.Instance?.Play(attackAudioKey, transform.position);
        damageCollider.enabled = true;

       

        yield return waitTimer;
       
        damageCollider.enabled = false;
        isActivated = false;
       
        spikesAnimator.SetBool("IsActivated", isActivated);
        MultiAudioPool.Instance?.Play(retractAudioKey, transform.position);



    }
}
