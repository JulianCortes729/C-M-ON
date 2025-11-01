using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LifeUI : MonoBehaviour
{
    private LifeManager lifeManager;
    [SerializeField] private TextMeshProUGUI textLife;
    [SerializeField] private SceneLoader sceneLoader;

    private void Awake()
    {
        lifeManager = LifeManager.Instance;
    }

    private void OnEnable()
    {
        lifeManager = LifeManager.Instance;
        if (lifeManager == null) return;

        lifeManager.SubscribeUIUpdateLife(UpdateUI);
        lifeManager.SubscribeGameOver(() => sceneLoader.LoadScene(GameScenes.GameOver));

        UpdateUI();
    }

    private void OnDisable()
    {
        if (lifeManager == null) return;
        lifeManager.UnsubscribeUIUpdateLife(UpdateUI);
    }

    private void UpdateUI() =>
        textLife.text = $"Lives: {lifeManager.GetLives()}";
}
