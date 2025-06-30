using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class TimeScaleSlider : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Maximum time scale (e.g., 4 = 4x speed)")]
    [SerializeField] private float maxTimeScale = 4f;

    private Slider timeSlider;

    void Start()
    {
        // Get reference to slider component
        timeSlider = GetComponent<Slider>();

        // Setup slider parameters
        timeSlider.minValue = 1f;        // Normal speed
        timeSlider.maxValue = maxTimeScale;
        timeSlider.value = Time.timeScale;

        // Add listener for value changes
        timeSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnSliderChanged(float newValue)
    {
        // Update time scale directly
        Time.timeScale = newValue;

        // Maintain physics consistency
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    void OnDestroy()
    {
        // Clean up event listener
        timeSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }
}