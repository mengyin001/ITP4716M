using UnityEngine;
using UnityEngine.UI;

public class ScreenModeController : MonoBehaviour
{
    [Header("UI References")]
    public Toggle fullscreenToggle;
    public Toggle windowedToggle;
    public ToggleGroup screenModeGroup;

    void Start()
    {
        // Initialize the toggle group
        screenModeGroup = GetComponent<ToggleGroup>();

        // Set initial states based on current screen mode
        bool isFullscreen = Screen.fullScreen;
        fullscreenToggle.isOn = isFullscreen;
        windowedToggle.isOn = !isFullscreen;

        // Make sure only one can be selected
        fullscreenToggle.group = screenModeGroup;
        windowedToggle.group = screenModeGroup;

        // Add listeners
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        windowedToggle.onValueChanged.AddListener(SetWindowed);
    }

    public void SetFullscreen(bool isOn)
    {
        if (isOn)
        {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
            Debug.Log("Fullscreen mode activated");
        }
    }

    public void SetWindowed(bool isOn)
    {
        if (isOn)
        {
            Screen.fullScreen = false;
            // Set to 90% of screen size for nice windowed mode
            int width = (int)(Screen.currentResolution.width * 0.9f);
            int height = (int)(Screen.currentResolution.height * 0.9f);
            Screen.SetResolution(width, height, false);
            Debug.Log("Windowed mode activated");
        }
    }

    void OnDestroy()
    {
        // Clean up listeners
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        windowedToggle.onValueChanged.RemoveAllListeners();
    }

    public void SaveScreenMode()
    {
        PlayerPrefs.SetInt("FullscreenMode", fullscreenToggle.isOn ? 1 : 0);
    }

    public void LoadScreenMode()
    {
        bool fullscreen = PlayerPrefs.GetInt("FullscreenMode", 1) == 1;
        fullscreenToggle.isOn = fullscreen;
        windowedToggle.isOn = !fullscreen;

        // Apply immediately
        if (fullscreen) SetFullscreen(true);
        else SetWindowed(true);
    }

}


