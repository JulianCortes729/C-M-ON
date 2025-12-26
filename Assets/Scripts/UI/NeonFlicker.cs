using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class NeonFlicker : MonoBehaviour
{

    private TMP_Text textMesh;


    [Range(0f, 100f)]
    public float chanceToFlicker = 5f;

    [Range(0f, 1f)]
    public float dimFactor = 0.1f;

    [Header("Ritmo")]
    public float minWaitTime = 0.05f;
    public float maxWaitTime = 0.2f;

    private Color baseColor;


    // Start is called before the first frame update
    void Start()
    {
        textMesh = GetComponent<TMP_Text>();
        
        if(textMesh != null)
        {
            baseColor = textMesh.color;
            StartCoroutine(FlickerRoutine());
        }
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            if(Random.Range(0f,100f) < chanceToFlicker)
            {
                textMesh.color = new Color(baseColor.r , baseColor.g, baseColor.b , dimFactor);
                yield return new WaitForSeconds(Random.Range(0.01f, 0.1f));

                //prender
                textMesh.color = baseColor;

                if(Random.value > 0.5f)
                {
                    yield return new WaitForSeconds(Random.Range(0.01f, 0.05f));

                    textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, dimFactor);

                    yield return new WaitForSeconds(Random.Range(0.01f, 0.1f));

                    textMesh.color = baseColor;


                }
            }
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        }
    }
}
