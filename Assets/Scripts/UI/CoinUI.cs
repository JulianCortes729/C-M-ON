using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    private void Start()
    {
        // Actualización inicial
        UpdateCoins(0);

        // Suscripción segura al Singleton
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.SubscribeUI(UpdateCoins);
            // Sincronizar valor actual al nacer
            UpdateCoins(CoinManager.Instance.CoinCount);
        }
    }

    private void OnDestroy()
    {
        // Desuscripción segura
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.UnsubscribeUI(UpdateCoins);
        }
    }

    private void UpdateCoins(int amount)
    {
        if (coinText != null) coinText.text = "Coins: " + amount;
    }
}
