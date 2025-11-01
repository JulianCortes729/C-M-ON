using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CoinManager : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private int coinsForSecretLevel = 20;

    // Evento privado (solo CoinManager y CoinUI lo usan)
    [SerializeField] private UnityEvent<int> onCoinsChanged = new();

    private int _coinCount;

    public static CoinManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // mantiene las monedas entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable() => Coin.OnCoinCollected.AddListener(AddCoin);
    private void OnDisable() => Coin.OnCoinCollected.RemoveListener(AddCoin);

    private void AddCoin()
    {
        _coinCount++;
        onCoinsChanged?.Invoke(_coinCount);

        if (_coinCount >= coinsForSecretLevel)
            UnlockSecretLevel();
    }

    private void UnlockSecretLevel()
    {
        Debug.Log("¡Nivel secreto desbloqueado!");
        // Aquí podrías habilitar un portal, puerta o cargar una escena.
    }


    public void ResetCoins()
    {
        _coinCount = 0;
        onCoinsChanged?.Invoke(_coinCount);
    }

    public void SubscribeUI(UnityAction<int> listener) => onCoinsChanged.AddListener(listener);
    public void UnsubscribeUI(UnityAction<int> listener) => onCoinsChanged.RemoveListener(listener);


    public int CoinCount => _coinCount; // Propiedad de solo lectura
}
