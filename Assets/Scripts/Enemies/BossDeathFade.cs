using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BossDeathFade : MonoBehaviour
{
    [Header("Configuración")]
    public float fadeDuration = 2.0f;

    [Header("Material Transparente")]
    public Material transparentMaterial; // <--- ARRASTRA AQUÍ TU MATERIAL 'Boss_Transparent_Mat'

    public void StartFadeOut()
    {
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();

        // 1. EL CAMBIAZO: Ponemos el material transparente
        foreach (Renderer r in allRenderers)
        {
            // Creamos un array temporal para guardar los nuevos materiales
            Material[] newMats = new Material[r.materials.Length];

            for (int i = 0; i < r.materials.Length; i++)
            {
                // Asignamos el material transparente a todas las ranuras
                // (Nota: Esto crea una COPIA del material para poder modificar su alfa individualmente)
                newMats[i] = new Material(transparentMaterial);

                // Opcional: Si el jefe tiene texturas diferentes en cada parte, 
                // aquí habría que copiar la textura del material viejo al nuevo.
                // newMats[i].mainTexture = r.materials[i].mainTexture;
            }

            // Aplicamos el cambio al renderer
            r.materials = newMats;
        }

        // 2. HACER EL FADE (Bajando el alfa)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            foreach (Renderer r in allRenderers)
            {
                foreach (Material m in r.materials)
                {
                    if (m.HasProperty("_BaseColor"))
                    {
                        Color c = m.GetColor("_BaseColor");
                        c.a = newAlpha;
                        m.SetColor("_BaseColor", c);
                    }
                }
            }
            yield return null;
        }

        // 3. ADIÓS
        Destroy(gameObject);

        // 4. Cambiar a la escena "ContinueScene" si existe el SceneLoader o como fallback usar SceneManager
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(GameScenes.Win);
        }
        else
        {
            // Intentamos cargar por nombre como respaldo
            SceneManager.LoadScene(GameScenes.Win.ToString());
        }
    }
}