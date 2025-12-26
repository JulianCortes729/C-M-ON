using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona la parte visual. Escucha al CursorController.
/// </summary>
public class SensitivityUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    // Referencia al panel HIJO que contiene las imágenes y sliders.
    // ESTE es el objeto que se apaga y prende.
    [SerializeField] private GameObject visualContent;

    [Header("UI Elements")]
    [SerializeField] private Slider horizontalSlider;
    [SerializeField] private Slider verticalSlider;
    [SerializeField] private TextMeshProUGUI horizontalLabel;
    [SerializeField] private TextMeshProUGUI verticalLabel;

    #endregion

    #region Private Fields

    private CursorController cursorController;

    private const string PREF_KEY_HORIZONTAL = "MouseSensitivityX";
    private const string PREF_KEY_VERTICAL = "MouseSensitivityY";
    private const float DEFAULT_HORIZONTAL = 2f;
    private const float DEFAULT_VERTICAL = 0.5f;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // 1. Buscar al cerebro (GameManager)
        cursorController = FindObjectOfType<CursorController>();

        if (cursorController == null)
            Debug.LogError("[SensitivityUI] ¡No se encontró CursorController en la escena!");
    }

    private void OnEnable()
    {
        if (cursorController != null)
        {
            // 2. Suscribirse al evento para escuchar cambios futuros
            cursorController.OnMenuStateChanged += HandleMenuStateChanged;
        }
    }

    private void Start()
    {
        // Cargar valores de los sliders
        LoadSavedValues();
        ValidateReferences();

        // 3. SINCRONIZACIÓN INICIAL (CRUCIAL)
        // Preguntamos al controller: "¿Cómo estás ahora mismo?" y actualizamos la visual.
        // Esto arregla el bug de que el menú quede abierto si la escena se reinicia.
        if (cursorController != null)
        {
            HandleMenuStateChanged(cursorController.IsSettingsOpen);
        }
    }

    private void OnDisable()
    {
        if (cursorController != null)
        {
            // 4. Desuscribirse para evitar errores de memoria
            cursorController.OnMenuStateChanged -= HandleMenuStateChanged;
        }
    }

    #endregion

    #region Event Handlers

    // Esta función se llama automáticamente cuando el Controller avisa, 
    // O manualmente en el Start para sincronizar.
    private void HandleMenuStateChanged(bool isOpen)
    {
        if (visualContent != null)
        {
            visualContent.SetActive(isOpen);
        }
    }

    public void UpdateHorizontalSpeed(float value)
    {
        if (cursorController != null) cursorController.SetHorizontalSpeed(value);
        UpdateLabels();
    }

    public void UpdateVerticalSpeed(float value)
    {
        if (cursorController != null) cursorController.SetVerticalSpeed(value);
        UpdateLabels();
    }

    #endregion

    #region Internal Logic

    private void LoadSavedValues()
    {
        float savedHorizontal = PlayerPrefs.GetFloat(PREF_KEY_HORIZONTAL, DEFAULT_HORIZONTAL);
        float savedVertical = PlayerPrefs.GetFloat(PREF_KEY_VERTICAL, DEFAULT_VERTICAL);

        if (horizontalSlider != null) horizontalSlider.value = savedHorizontal;
        if (verticalSlider != null) verticalSlider.value = savedVertical;

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        if (horizontalLabel != null && horizontalSlider != null)
            horizontalLabel.text = $"Sensibilidad Horizontal: {horizontalSlider.value:F1}";

        if (verticalLabel != null && verticalSlider != null)
            verticalLabel.text = $"Sensibilidad Vertical: {verticalSlider.value:F1}";
    }

    private void ValidateReferences()
    {
        if (visualContent == null) Debug.LogWarning("[SensitivityUI] ¡Falta asignar 'Visual Content'!");
        if (horizontalSlider == null) Debug.LogWarning("[SensitivityUI] Falta Slider Horizontal");
        // ... resto de validaciones
    }

    #endregion
}