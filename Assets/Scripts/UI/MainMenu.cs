using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
// No necesitamos SceneManager aquí, lo manejará el Player/Fader

public class MainMenu : MonoBehaviour
{
    [Header("Scene Name")]
    [SerializeField] private string introScene = "Intro";

    [Header("References")]
    [SerializeField] private MainMenuPlayer menuPlayer; // <-- ARRASTRA TU PLAYER DEL MENÚ AQUÍ

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject controlsPanel;

    [DllImport("__Internal")]
    private static extern void AbrirEnMismaPestana(string url);

    private void Start()
    {
        ShowMainMenu();

        // Búsqueda automática por seguridad si se te olvida arrastrarlo
        if (menuPlayer == null)
            menuPlayer = FindObjectOfType<MainMenuPlayer>();
    }

    // -------------------------
    //      Panel Handling
    // -------------------------
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        controlsPanel.SetActive(false);
    }

    public void ShowControls()
    {
        mainMenuPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    // -------------------------
    //      Button Actions
    // -------------------------
    public void StartGame()
    {
        if (menuPlayer != null)
        {
            // Delegamos la acción al player para que haga la animación
            menuPlayer.PlayStartSequence(introScene);
        }
        else
        {
            Debug.LogWarning("No se asignó MainMenuPlayer, cargando directo.");
            // Fallback usando tu SceneFader directamente si no hay player
            SceneFader.Instance.FadeToScene(introScene);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game solicitado...");

        // 1. Si estamos probando en el Editor de Unity
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;

        // 2. Si estamos en WebGL (Navegador / Unity Play)
#elif UNITY_WEBGL
        // En web no se puede cerrar la ventana, pero podemos redirigir.
        // Puedes poner aquí tu perfil de Unity Play, itch.io, o Google.
        // Ojo: Si estás en un iframe (como itch.io), a veces bloquean esto también.
        
        AbrirEnMismaPestana("https://play.unity.com/en/user/553cb41c-e608-457d-adad-00e00adbaf48"); 
        
        // OPCIONAL: Si prefieres no irte, puedes mostrar un texto de "Gracias por jugar"
        // y desactivar el panel del menú.
        
        // 3. Si es un juego de PC/Consola (Build normal)
#else
        Application.Quit();
#endif
    }
}
