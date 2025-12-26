using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class IntroScroll : MonoBehaviour
{
    [Header("Configuración")]
    public float velocidadScroll = 20f; // Ajusta esto según qué tan rápido quieras que suba
    public float posicionFinalY = 1500f; // La altura a la que termina el texto
    public string nombreSiguienteEscena = "Level1"; // El nombre de tu escena de juego

    [Header("Opciones")]
    public bool permitirSaltar = true; // Si el jugador aprieta una tecla, salta la intro

    private RectTransform rectTransform;

    private bool yaTermino = false; // Para que no llame al fade muchas veces

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (yaTermino) return; // Si ya está terminando, no hace nada más

        // Mover el texto hacia arriba en su eje local
        transform.Translate(Vector3.up * velocidadScroll * Time.deltaTime, Space.Self);

        // Chequear si el texto ya pasó la altura límite
        if (rectTransform.anchoredPosition.y > posicionFinalY)
        {
            TerminarIntro();
        }

        // Opción para saltar la intro con cualquier tecla
        if (permitirSaltar && Input.GetKeyDown(KeyCode.Q))
        {
            TerminarIntro();
        }
    }

    void TerminarIntro()
    {
        if (yaTermino) return;
        yaTermino = true;

        // Verifica si existe el SceneFader (debería venir desde el menú)
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(nombreSiguienteEscena);
        }
        else
        {
            // Fallback por si probaste la escena suelta sin pasar por el menú
            Debug.LogWarning("No se encontró SceneFader. Cargando directo.");
            SceneManager.LoadScene(nombreSiguienteEscena);
        }
    }
}
