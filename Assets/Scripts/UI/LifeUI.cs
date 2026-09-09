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

    private void Awake()
    {
        lifeManager = LifeManager.Instance;
    }

    private void OnEnable()
    {
        lifeManager = LifeManager.Instance;
        if (lifeManager == null) return;

        lifeManager.SubscribeUIUpdateLife(UpdateUI);
        // 📖 Método con nombre, no lambda: RemoveListener busca por Target + MethodInfo,
        // 📖 y cada lambda escrita en el código es un método distinto — irremovible.
        lifeManager.SubscribeGameOver(HandleGameOver);

        UpdateUI();
    }

    private void OnDisable()
    {
        if (lifeManager == null) return;
        lifeManager.UnsubscribeUIUpdateLife(UpdateUI);
        lifeManager.UnsubscribeGameOver(HandleGameOver);
    }

    private void HandleGameOver()
    {
        // 📖 Instance y no un [SerializeField]: hay un SceneLoader dentro de Level1, GameOver y
        // 📖 Win, y cada Awake se autodestruye si ya existe un Instance vivo. Una referencia
        // 📖 serializada apunta siempre al de la escena — al que se suicida — no al que sobrevive.
        // 📖 Andamio temporal: en el hito 2 esta dependencia la inyecta el composition root.
        if (SceneLoader.Instance == null)
        {
            // 📖 LogError y no un return mudo: si falla el cableado tiene que gritar.
            // 📖 El segundo argumento resalta este objeto en la jerarquía al clickear el error.
            Debug.LogError("[LifeUI] No hay SceneLoader.Instance: no se puede cargar el GameOver.", this);
            return;
        }

        SceneLoader.Instance.LoadScene(GameScenes.GameOver);
    }

    private void UpdateUI() =>
        textLife.text = $"Lives: {lifeManager.GetLives()}";
}
