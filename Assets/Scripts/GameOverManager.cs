using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private Button restartButton;
    [SerializeField] private SceneLoader sceneLoader;

    private void Start()
    {
        restartButton.onClick.AddListener(RestartGame);
    }

    private void RestartGame()
    {
        // Reiniciar sistemas persistentes
        if (LifeManager.Instance != null)
            LifeManager.Instance.ResetLives();

        if (CoinManager.Instance != null)
            CoinManager.Instance.ResetCoins();

        // Cargar la escena principal con fade
        sceneLoader.LoadScene(GameScenes.Level1);
    }
}
