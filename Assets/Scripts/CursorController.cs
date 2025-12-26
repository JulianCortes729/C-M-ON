using Cinemachine;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona el bloqueo del cursor, la sensibilidad y el estado del menú.
/// Actúa como la "Fuente de la Verdad" para la UI.
/// </summary>
public class CursorController : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private CinemachineFreeLook freeLookCamera;

    [Header("Input Keys")]
    [SerializeField] private KeyCode settingsKey = KeyCode.Tab;
    [SerializeField] private KeyCode unlockKey = KeyCode.Escape;

    [Header("Default Sensitivity Values")]
    [SerializeField] private float defaultHorizontalSpeed = 2f;
    [SerializeField] private float defaultVerticalSpeed = 0.5f;

    // EVENTO: La UI se suscribirá a esto para saber cuándo abrirse/cerrarse
    public event Action<bool> OnMenuStateChanged;

    #endregion

    #region Private Fields

    private float horizontalSpeed;
    private float verticalSpeed;
    private bool isSettingsOpen;
    private bool isPaused; // Determina si estamos en una escena que NO es de juego

    // Keys para guardar datos
    private const string PREF_KEY_HORIZONTAL = "MouseSensitivityX";
    private const string PREF_KEY_VERTICAL = "MouseSensitivityY";

    #endregion

    #region Public Properties

    public bool IsSettingsOpen => isSettingsOpen;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        isSettingsOpen = false;
    }

    private void Start()
    {
        LoadSavedSensitivity();
        CheckScene();
        ApplySpeeds();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // Si estamos en GameOver o Menú Principal, no procesamos inputs de bloqueo/desbloqueo
        if (isPaused) return;

        HandleInput();

        ApplySpeeds();
    }

    #endregion

    #region Scene Management

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckScene();
    }

    private void CheckScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        // IMPORTANTE: Asegúrate de que "Level1" es el nombre exacto de tu escena
        bool isGameplayScene = currentScene.Contains("Level1");

        if (!isGameplayScene)
        {
            // --- MODO MENÚ (GameOver, MainMenu, etc) ---
            isPaused = true; // Esto detiene el Update() para que los clics no re-bloqueen el cursor

            // 1. Lógicamente cerramos el menú de sensibilidad (sin efectos secundarios de cursor)
            isSettingsOpen = false;
            OnMenuStateChanged?.Invoke(false); // Avisamos a la UI para que se oculte si estaba abierta

            // 2. FÍSICAMENTE desbloqueamos el cursor para poder usar botones
            UnlockCursor();
        }
        else
        {
            // --- MODO JUEGO (Level1) ---
            isPaused = false;

            // Al entrar al nivel, usamos SetMenuState(false) 
            // Esto cierra el menú Y bloquea el cursor automáticamente para jugar
            SetMenuState(false);

            FindCameraIfNeeded();
            LoadSavedSensitivity();
            ApplySpeeds();
        }
    }

    private void FindCameraIfNeeded()
    {
        if (freeLookCamera == null)
        {
            freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
        }
    }

    #endregion

    #region Input Handling

    private void HandleInput()
    {
        if (Input.GetKeyDown(settingsKey))
        {
            ToggleSettings();
        }

        if (Input.GetKeyDown(unlockKey) && !isSettingsOpen)
        {
            UnlockCursor();
        }

        if (Input.GetMouseButtonDown(0) && !isSettingsOpen)
        {
            LockCursor();
        }
    }

    private void ToggleSettings()
    {
        SetMenuState(!isSettingsOpen);
    }

    /// <summary>
    /// Método centralizado que cambia el estado y avisa a todos los suscriptores.
    /// </summary>
    private void SetMenuState(bool isOpen)
    {
        isSettingsOpen = isOpen;

        if (isSettingsOpen)
        {
            UnlockCursor();
        }
        else
        {
            LockCursor();
        }

        OnMenuStateChanged?.Invoke(isSettingsOpen);
    }

    #endregion

    #region Sensitivity & Cursor Logic

    private void LoadSavedSensitivity()
    {
        horizontalSpeed = PlayerPrefs.GetFloat(PREF_KEY_HORIZONTAL, defaultHorizontalSpeed);
        verticalSpeed = PlayerPrefs.GetFloat(PREF_KEY_VERTICAL, defaultVerticalSpeed);
    }

    public void SetHorizontalSpeed(float value)
    {
        horizontalSpeed = value;
        PlayerPrefs.SetFloat(PREF_KEY_HORIZONTAL, horizontalSpeed);
        PlayerPrefs.Save();
    }

    public void SetVerticalSpeed(float value)
    {
        verticalSpeed = value;
        PlayerPrefs.SetFloat(PREF_KEY_VERTICAL, verticalSpeed);
        PlayerPrefs.Save();
    }

    private void ApplySpeeds()
    {
        if (freeLookCamera == null) FindCameraIfNeeded();

        if (freeLookCamera != null)
        {
            freeLookCamera.m_XAxis.m_MaxSpeed = horizontalSpeed;
            freeLookCamera.m_YAxis.m_MaxSpeed = verticalSpeed;
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion
}