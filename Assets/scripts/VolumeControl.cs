using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add this line for TextMeshPro

public class VolumeControl : MonoBehaviour
{
    public AudioSource audioSource;    // Assign your AudioSource in the Inspector
    public Slider volumeSlider;         // Assign your Slider in the Inspector
    public TMP_Text volumeText;         // Use TMP_Text for TextMeshPro

    void Start()
    {
        // Initialize the slider value to the current audio volume
        volumeSlider.value = audioSource.volume;
        UpdateVolumeText(volumeSlider.value);

        // Add a listener to the slider to call UpdateVolume when the value changes
        volumeSlider.onValueChanged.AddListener(UpdateVolume);
    }

    void UpdateVolume(float value)
    {
        // Update the audio source volume
        audioSource.volume = value;
        UpdateVolumeText(value);
    }

    void UpdateVolumeText(float value)
    {
        // Update the text display with the slider value
        volumeText.text = "Volume: " + (value * 100).ToString("F0") + "%";
    }
}