using UnityEngine;
using Cinemachine;

/// <summary>
/// Manages cursor locking/unlocking and camera sensitivity across game scenes.
/// Automatically pauses in non-gameplay scenes and persists user sensitivity preferences.
/// </summary>
public class CursorController : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private CinemachineFreeLook freeLookCamera;
    [SerializeField] private GameObject sensitivityPanel;

    [Header("Input Keys")]
    [SerializeField] private KeyCode settingsKey = KeyCode.Tab;
    [SerializeField] private KeyCode unlockKey = KeyCode.Escape;

    [Header("Default Sensitivity Values")]
    [SerializeField] private float defaultHorizontalSpeed = 2f;
    [SerializeField] private float defaultVerticalSpeed = 0.5f;

    #endregion

    #region Private Fields

    private float horizontalSpeed;
    private float verticalSpeed;
    private bool isSettingsOpen;
    private bool isPaused;

    // PlayerPrefs keys
    private const string PREF_KEY_HORIZONTAL = "MouseSensitivityX";
    private const string PREF_KEY_VERTICAL = "MouseSensitivityY";

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        LoadSavedSensitivity();
        CheckScene();
        ApplySpeeds();

        if (sensitivityPanel != null)
        {
            sensitivityPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (isPaused) return;

        HandleInput();
        ApplySpeeds();
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// Called when a new scene is loaded. Updates cursor state based on scene type.
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        CheckScene();
    }

    /// <summary>
    /// Determines if current scene is gameplay and adjusts cursor accordingly.
    /// </summary>
    private void CheckScene()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isGameplayScene = currentScene.Contains("Level1");

        if (!isGameplayScene)
        {
            UnlockCursor();
            isPaused = true;
        }
        else
        {
            FindCameraIfNeeded();
            LoadSavedSensitivity();
            ApplySpeeds();
            LockCursor();
            isPaused = false;
        }
    }

    /// <summary>
    /// Searches for FreeLook camera in scene if reference is lost.
    /// Useful when controller persists across scene loads via DontDestroyOnLoad.
    /// </summary>
    private void FindCameraIfNeeded()
    {
        if (freeLookCamera == null)
        {
            freeLookCamera = FindObjectOfType<CinemachineFreeLook>();

            if (freeLookCamera == null)
            {
                Debug.LogError("[CursorController] CinemachineFreeLook not found in scene!");
            }
        }
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Processes user input for settings menu and cursor unlocking.
    /// Time Complexity: O(1)
    /// </summary>
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

    /// <summary>
    /// Toggles sensitivity settings panel visibility and cursor state.
    /// </summary>
    private void ToggleSettings()
    {
        isSettingsOpen = !isSettingsOpen;

        if (isSettingsOpen)
        {
            UnlockCursor();
            if (sensitivityPanel != null)
            {
                sensitivityPanel.SetActive(true);
            }
        }
        else
        {
            if (sensitivityPanel != null)
            {
                sensitivityPanel.SetActive(false);
            }
            LockCursor();
        }
    }

    #endregion

    #region Sensitivity Management

    /// <summary>
    /// Loads saved sensitivity values from PlayerPrefs or uses defaults.
    /// Time Complexity: O(1)
    /// </summary>
    private void LoadSavedSensitivity()
    {
        horizontalSpeed = PlayerPrefs.GetFloat(PREF_KEY_HORIZONTAL, defaultHorizontalSpeed);
        verticalSpeed = PlayerPrefs.GetFloat(PREF_KEY_VERTICAL, defaultVerticalSpeed);
    }

    /// <summary>
    /// Sets horizontal camera sensitivity and persists to PlayerPrefs.
    /// Called by UI slider events.
    /// Time Complexity: O(1)
    /// </summary>
    public void SetHorizontalSpeed(float value)
    {
        horizontalSpeed = value;
        PlayerPrefs.SetFloat(PREF_KEY_HORIZONTAL, horizontalSpeed);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Sets vertical camera sensitivity and persists to PlayerPrefs.
    /// Called by UI slider events.
    /// Time Complexity: O(1)
    /// </summary>
    public void SetVerticalSpeed(float value)
    {
        verticalSpeed = value;
        PlayerPrefs.SetFloat(PREF_KEY_VERTICAL, verticalSpeed);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Applies current sensitivity values to the FreeLook camera.
    /// Automatically finds camera reference if lost.
    /// Time Complexity: O(1) if camera cached, O(n) if search needed
    /// </summary>
    private void ApplySpeeds()
    {
        if (freeLookCamera == null)
        {
            freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
        }

        if (freeLookCamera != null)
        {
            freeLookCamera.m_XAxis.m_MaxSpeed = horizontalSpeed;
            freeLookCamera.m_YAxis.m_MaxSpeed = verticalSpeed;
        }
    }

    #endregion

    #region Cursor State Management

    /// <summary>
    /// Locks cursor to center of screen and hides it.
    /// Essential for FPS-style camera control.
    /// Time Complexity: O(1)
    /// </summary>
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Unlocks cursor and makes it visible for UI interaction.
    /// Time Complexity: O(1)
    /// </summary>
    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion
}