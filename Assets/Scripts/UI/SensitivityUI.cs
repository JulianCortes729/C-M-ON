using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the sensitivity settings UI panel.
/// Synchronizes slider values with CursorController and updates display labels.
/// </summary>
public class SensitivityUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private CursorController cursorController;

    [Header("UI Elements")]
    [SerializeField] private Slider horizontalSlider;
    [SerializeField] private Slider verticalSlider;
    [SerializeField] private TextMeshProUGUI horizontalLabel;
    [SerializeField] private TextMeshProUGUI verticalLabel;

    #endregion

    #region Constants

    private const string PREF_KEY_HORIZONTAL = "MouseSensitivityX";
    private const string PREF_KEY_VERTICAL = "MouseSensitivityY";
    private const float DEFAULT_HORIZONTAL = 2f;
    private const float DEFAULT_VERTICAL = 0.5f;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        LoadSavedValues();
        ValidateReferences();
    }

    /// <summary>
    /// Reloads saved values when panel is activated.
    /// Ensures UI reflects current settings after scene transitions.
    /// </summary>
    private void OnEnable()
    {
        LoadSavedValues();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Validates that all required references are assigned.
    /// Logs warnings for missing components to aid debugging.
    /// Time Complexity: O(1)
    /// </summary>
    private void ValidateReferences()
    {
        if (cursorController == null)
            Debug.LogWarning("[SensitivityUI] CursorController reference not assigned!");

        if (horizontalSlider == null)
            Debug.LogWarning("[SensitivityUI] Horizontal Slider reference not assigned!");

        if (verticalSlider == null)
            Debug.LogWarning("[SensitivityUI] Vertical Slider reference not assigned!");

        if (horizontalLabel == null)
            Debug.LogWarning("[SensitivityUI] Horizontal Label reference not assigned!");

        if (verticalLabel == null)
            Debug.LogWarning("[SensitivityUI] Vertical Label reference not assigned!");
    }

    /// <summary>
    /// Loads saved sensitivity values from PlayerPrefs and updates UI.
    /// Time Complexity: O(1)
    /// </summary>
    private void LoadSavedValues()
    {
        float savedHorizontal = PlayerPrefs.GetFloat(PREF_KEY_HORIZONTAL, DEFAULT_HORIZONTAL);
        float savedVertical = PlayerPrefs.GetFloat(PREF_KEY_VERTICAL, DEFAULT_VERTICAL);

        if (horizontalSlider != null)
        {
            horizontalSlider.value = savedHorizontal;
        }

        if (verticalSlider != null)
        {
            verticalSlider.value = savedVertical;
        }

        UpdateLabels();
    }

    #endregion

    #region Public Methods (Called by UI Events)

    /// <summary>
    /// Updates horizontal camera sensitivity.
    /// Called by horizontal slider OnValueChanged event.
    /// Time Complexity: O(1)
    /// </summary>
    /// <param name="value">New horizontal sensitivity value</param>
    public void UpdateHorizontalSpeed(float value)
    {
        if (cursorController != null)
        {
            cursorController.SetHorizontalSpeed(value);
        }

        UpdateLabels();
    }

    /// <summary>
    /// Updates vertical camera sensitivity.
    /// Called by vertical slider OnValueChanged event.
    /// Time Complexity: O(1)
    /// </summary>
    /// <param name="value">New vertical sensitivity value</param>
    public void UpdateVerticalSpeed(float value)
    {
        if (cursorController != null)
        {
            cursorController.SetVerticalSpeed(value);
        }

        UpdateLabels();
    }

    #endregion

    #region Label Updates

    /// <summary>
    /// Updates label text to display current slider values.
    /// Time Complexity: O(1)
    /// </summary>
    private void UpdateLabels()
    {
        if (horizontalLabel != null && horizontalSlider != null)
        {
            horizontalLabel.text = $"Sensibilidad Horizontal: {horizontalSlider.value:F1}";
        }

        if (verticalLabel != null && verticalSlider != null)
        {
            verticalLabel.text = $"Sensibilidad Vertical: {verticalSlider.value:F1}";
        }
    }

    #endregion
}