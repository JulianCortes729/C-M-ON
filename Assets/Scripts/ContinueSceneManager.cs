using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla la escena "To be continued" que aparece tras derrotar al jefe.
/// Muestra un botón para reiniciar el juego (volver al menú principal) y resetea managers.
/// </summary>
public class ContinueSceneManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button continueButton;

    void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);
    }

    private void OnContinuePressed()
    {
        // Asegurar tiempo normal
        Time.timeScale = 1f;

        // Resetear estado global (si existen los Singletons)
        if (LifeManager.Instance != null) LifeManager.Instance.ResetLives();
        if (CoinManager.Instance != null) CoinManager.Instance.ResetCoins();

        // Esto borra la memoria del último checkpoint guardado
        if (CheckpointManager.Instance != null) CheckpointManager.Instance.ClearCheckpoints();

        // Usar SceneLoader si está disponible para manejar transiciones/fade
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(GameScenes.MainMenu);
        }
        else
        {
            // Fallback directo
            SceneManager.LoadScene(GameScenes.MainMenu.ToString());
        }
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinuePressed);
    }
}