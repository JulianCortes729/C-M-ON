using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private CoinManager coinManager;

    private void Awake()
    {
        // Si no lo asignaste por Inspector, lo busca automáticamente
        if (coinManager == null)
            coinManager = CoinManager.Instance;
    }

    private void OnEnable()
    {
        if (coinManager != null)
        {
            coinManager.SubscribeUI(UpdateUI);
            UpdateUI(coinManager.CoinCount); // Mostrar valor actual
        }
    }

    private void OnDisable()
    {
        if (coinManager != null)
            coinManager.UnsubscribeUI(UpdateUI);
    }
    private void UpdateUI(int coinCount)
    {
        if (coinText != null)
            coinText.text = $"Coins: {coinCount}";
    }
}
