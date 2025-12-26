using UnityEngine;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton; // Supongamos que tienes uno

    // YA NO NECESITAMOS ARRASTRAR EL SCENELOADER
    // [SerializeField] private SceneLoader sceneLoader; <--- BORRADO

    private void Start()
    {
        // BINDING POR CÓDIGO:
        // Le decimos al botón qué hacer usando C#. Esto es mucho más estable.

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(HandleRestart);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(HandleQuit);
        }
    }

    // Funciones "Wrapper" para llamar a los Singletons
    private void HandleRestart()
    {
        // 1. Resetear lógica
        ResetGameState();

        // 2. Usar el Singleton directamente
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(GameScenes.Level1);
        }
        else
        {
            Debug.LogError("SceneLoader Instance no encontrada. Asegúrate de iniciar el juego desde el MainMenu o que exista el GameManager.");
            // Fallback de emergencia
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
        }
    }

    private void HandleQuit()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.QuitGame();
    }

    private void ResetGameState()
    {
        // Asegurar tiempo normal
        Time.timeScale = 1f;

        // Resetear Managers usando sus Singletons
        if (LifeManager.Instance != null) LifeManager.Instance.ResetLives();
        if (CoinManager.Instance != null) CoinManager.Instance.ResetCoins();
    }

    // Limpieza de eventos para evitar errores de memoria
    private void OnDestroy()
    {
        if (restartButton != null) restartButton.onClick.RemoveListener(HandleRestart);
        if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuit);
    }
}
