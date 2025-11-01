using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class LifeManager : MonoBehaviour
{
    public static LifeManager Instance { get; private set; }

    [SerializeField] private int startingLives = 3;
    private int currentLives;
    [SerializeField] private GameObject player;

    [SerializeField] private UnityEvent OnUpdateLife = new UnityEvent();
    [SerializeField] private UnityEvent OnGameOver = new UnityEvent();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentLives = startingLives;
        OnUpdateLife?.Invoke();

        SceneManager.sceneLoaded += OnSceneLoaded;
        FindPlayer();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindPlayer();
    }

    private bool IsPlayerValid()
    {
        return player != null && player.gameObject != null;
    }

    private void FindPlayer()
    {
        if (!IsPlayerValid())
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
            {
                player = foundPlayer;
                Debug.Log("LifeManager: Jugador encontrado - " + player.name);
            }
            else
            {
                Debug.LogWarning("LifeManager: No se encontró jugador con tag 'Player'");
            }
        }
    }

    private void OnEnable()
    {
        Life.OnLifeCollected.AddListener(AddLife);
    }

    private void OnDisable()
    {
        Life.OnLifeCollected.RemoveListener(AddLife);
    }

    public void LoseLife(int amount)
    {
        currentLives -= amount;

        if (currentLives > 0)
        {
            // Tiene vidas: respawnear
            StartCoroutine(RespawnPlayer());
        }
        else
        {
            // Game Over: limpiar checkpoints
            Debug.Log("GAME OVER - Limpiando checkpoints");
            CheckpointManager.Instance.ClearCheckpoints();
            OnGameOver?.Invoke();
        }

        OnUpdateLife?.Invoke();
    }

    private IEnumerator RespawnPlayer()
    {
        // Fade a negro
        yield return SceneFader.Instance.Fade(1);
        yield return new WaitForSeconds(0.2f);

        // Buscar player si no lo tenemos
        if (!IsPlayerValid())
        {
            FindPlayer();
        }

        // Hacer respawn
        if (IsPlayerValid())
        {
            // Resetear death handler
            player.GetComponent<PlayerDeathHandler>()?.ResetAfterRespawn();

            // Hacer respawn
            SimplePlayerRespawn respawn = player.GetComponent<SimplePlayerRespawn>();
            if (respawn != null)
            {
                respawn.Respawn();
            }
        }

        yield return new WaitForSeconds(0.2f);

        // Fade desde negro
        yield return SceneFader.Instance.Fade(0);
    }

    public int GetLives() => currentLives;

    public void AddLife(int amount)
    {
        currentLives += amount;
        OnUpdateLife?.Invoke();
    }

    public void ResetLives()
    {
        currentLives = startingLives;
        OnUpdateLife?.Invoke();
        Debug.Log("LifeManager: Vidas reseteadas a " + startingLives);
    }

    public void SubscribeUIUpdateLife(UnityAction listener) => OnUpdateLife.AddListener(listener);
    public void UnsubscribeUIUpdateLife(UnityAction listener) => OnUpdateLife.RemoveListener(listener);
    public void SubscribeGameOver(UnityAction listener) => OnGameOver.AddListener(listener);
    public void UnsubscribeGameOver(UnityAction listener) => OnGameOver.RemoveListener(listener);
}