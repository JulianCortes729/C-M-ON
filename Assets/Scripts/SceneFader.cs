using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance;

    [Header("Fade Config")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;

    private void Awake()
    {
        // Singleton persistente
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // mantiene el fader y su canvas juntos
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Config inicial
        if (fadeImage == null)
            fadeImage = GetComponentInChildren<Image>(true);

        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);

        StartCoroutine(Fade(0)); // Fade in al iniciar
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Solo hace fade-in cuando se carga nueva escena
        if (fadeImage != null)
            StartCoroutine(Fade(0));
    }

    // ====================== FADES ======================
    public IEnumerator Fade(float targetAlpha)
    {
        if (fadeImage == null) yield break;

        float startAlpha = fadeImage.color.a;
        Color c = fadeColor;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            if (fadeImage == null) yield break;

            c.a = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        if (fadeImage != null)
        {
            c.a = targetAlpha;
            fadeImage.color = c;
        }
    }

    // Transición entre escenas
    public void FadeToScene(GameScenes scene)
    {
        StartCoroutine(FadeSceneCoroutine(scene));
    }

    private IEnumerator FadeSceneCoroutine(GameScenes scene)
    {
        yield return Fade(1);
        SceneManager.LoadScene(scene.ToString());
    }

    // Fade de respawn
    public void FadeRespawn(Transform respawnPoint, GameObject player, float delay = 0.1f)
    {
        StartCoroutine(FadeRespawnCoroutine(respawnPoint, player, delay));
    }

    private IEnumerator FadeRespawnCoroutine(Transform point, GameObject player, float delay)
    {
        yield return Fade(1);
        yield return new WaitForSeconds(delay);
        if (player && point)
            player.transform.SetPositionAndRotation(point.position, point.rotation);
        yield return Fade(0);
    }

    public IEnumerator FadeRespawnSequence(Transform respawnPoint, GameObject player, float delay = 0.1f)
    {
        // Fade-out (pantalla a negro)
        yield return SceneFader.Instance.Fade(1);

        // Esperar pequeño delay por si hay animación o sonido
        yield return new WaitForSeconds(delay);

        if (player != null && respawnPoint != null)
        {
            // Desactivar colisión temporalmente
            Collider col = player.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Mover jugador y reiniciar animaciones/controles
            player.transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
            player.GetComponent<PlayerDeathHandler>()?.ResetAfterRespawn();

            // Reactivar colisión
            if (col != null) col.enabled = true;
        }

        // Fade-in
        yield return SceneFader.Instance.Fade(0);
    }
}
