using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // PROPIEDAD ESTÁTICA: Acceso global desde cualquier script
    public static SceneLoader Instance { get; private set; }

    private SceneFader fader;

    private void Awake()
    {
        // Implementación Singleton clásica
        if (Instance == null)
        {
            Instance = this;
            // Opcional: Si este script va solo en un objeto, descomenta esto:
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); // Evitar duplicados si recargas la escena con el Manager dentro
            return;
        }

        // Buscamos el fader una sola vez al inicio
        fader = FindObjectOfType<SceneFader>();
    }

    public void LoadScene(GameScenes scene)
    {
        // Seguridad: Si el fader se perdió (porque era de una escena vieja), búscalo de nuevo
        if (fader == null) fader = FindObjectOfType<SceneFader>();

        if (fader != null)
            fader.FadeToScene(scene);
        else
            SceneManager.LoadScene(scene.ToString());
    }

    public void ReloadCurrentScene()
    {
        if (fader == null) fader = FindObjectOfType<SceneFader>();

        string currentScene = SceneManager.GetActiveScene().name;

        // Si quieres usar fade para recargar:
        // fader.FadeToScene(currentScene); 
        // Por simplicidad, carga directa:
        SceneManager.LoadScene(currentScene);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
