using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    private SceneFader fader;

    private void Awake()
    {
        fader = FindObjectOfType<SceneFader>();
    }

    // Cargar usando enum
    public void LoadScene(GameScenes scene)
    {
        string sceneName = scene.ToString();
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"La escena '{sceneName}' no está en Build Settings.");
            return;
        }

        if (fader != null)
            fader.FadeToScene(scene);
        else
            SceneManager.LoadScene(sceneName);
    }

    // Recargar la escena actual
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Salir del juego
    public void QuitGame()
    {
        Debug.Log("Cerrando juego...");
        Application.Quit();
    }
}
