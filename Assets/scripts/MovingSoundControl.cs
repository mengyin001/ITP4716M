using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class MovingSoundControl : MonoBehaviourPun, IPunObservable
{
    [Header("Sound Configuration")]
    [SerializeField] private AudioClip movingSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Rigidbody2D rb;

    [Header("Movement Detection")]
    [SerializeField] private float velocityThreshold = 0.05f;
    [SerializeField] private float positionChangeThreshold = 0.01f;
    [SerializeField] private float stopDelay = 0.2f;
    [SerializeField] private bool useInputDetection = true;

    [Header("Debugging")]
    [SerializeField] private Text debugText;
    [SerializeField] private bool showSceneDebug = true;
    [SerializeField] private bool enableLogging = true;

    private bool isMoving = false;
    private float stopTimer;
    private bool shouldPlayAudio = false;
    private Vector2 networkVelocity;
    private Vector2 lastPosition;
    private float lastPositionCheckTime;
    private string debugMessage = "";
    private float inputValue;

    void Start()
    {
        LogMessage("Script initialized");

        // Get components if not set
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Configure audio
        if (movingSound != null)
        {
            audioSource.clip = movingSound;
            LogMessage($"Audio clip set: {movingSound.name}");
        }
        else
        {
            LogWarning("No moving sound assigned!");
        }

        audioSource.loop = true;

        // Initialize position tracking
        lastPosition = transform.position;
        lastPositionCheckTime = Time.time;

        // Disable physics on remote clients
        if (!photonView.IsMine && rb != null)
        {
            rb.isKinematic = true;
            LogMessage("Remote client: Physics disabled");
        }
    }

    void Update()
    {
        // Only owner handles movement detection
        if (photonView.IsMine)
        {
            DetectMovement();
        }

        // Handle audio for all clients
        HandleAudio();

        // Update debug display
        UpdateDebugDisplay();
    }

    private void DetectMovement()
    {
        if (rb == null)
        {
            LogWarning("Rigidbody2D is null!");
            return;
        }

        bool positionChanged = false;
        bool velocityMoving = false;
        bool inputActive = false;

        // 1. Velocity-based detection
        float velocityX = Mathf.Abs(rb.linearVelocity.x);
        velocityMoving = velocityX > velocityThreshold;

        // 2. Position-change detection (fallback)
        if (Time.time - lastPositionCheckTime > 0.1f)
        {
            float positionDelta = Vector2.Distance(transform.position, lastPosition);
            positionChanged = positionDelta > positionChangeThreshold;
            lastPosition = transform.position;
            lastPositionCheckTime = Time.time;
        }

        // 3. Input-based detection
        if (useInputDetection)
        {
            inputValue = Input.GetAxis("Horizontal");
            inputActive = Mathf.Abs(inputValue) > 0.1f;
        }

        // Combined movement detection
        bool currentlyMoving = velocityMoving || positionChanged || inputActive;

        // Log detailed movement info
        LogMessage($"Movement: V={velocityX:F3} | ΔP={Vector2.Distance(transform.position, lastPosition):F3} | Input={inputValue:F2}");

        if (currentlyMoving)
        {
            if (!isMoving) LogMessage($"Movement started! (V:{velocityX:F3}, ΔP:{Vector2.Distance(transform.position, lastPosition):F3})");

            isMoving = true;
            stopTimer = 0f;
            shouldPlayAudio = true;
        }
        else if (isMoving)
        {
            stopTimer += Time.deltaTime;
            if (stopTimer >= stopDelay)
            {
                LogMessage($"Movement stopped");
                isMoving = false;
                shouldPlayAudio = false;
            }
        }
    }

    private void HandleAudio()
    {
        if (audioSource == null)
        {
            LogWarning("AudioSource is null!");
            return;
        }

        if (shouldPlayAudio)
        {
            if (!audioSource.isPlaying)
            {
                LogMessage("Playing audio");
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                LogMessage("Stopping audio");
                audioSource.Stop();
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Owner sends audio state
            stream.SendNext(shouldPlayAudio);
            if (rb != null) stream.SendNext(rb.linearVelocity);
            LogMessage($"Sending: Audio={shouldPlayAudio}, V={rb.linearVelocity}");
        }
        else
        {
            // Remote clients receive state
            shouldPlayAudio = (bool)stream.ReceiveNext();
            if (rb != null) networkVelocity = (Vector2)stream.ReceiveNext();

            // Apply network velocity for visual consistency
            if (rb != null) rb.linearVelocity = networkVelocity;

            LogMessage($"Received: Audio={shouldPlayAudio}, V={networkVelocity}");
        }
    }

    #region Debugging Tools
    private void LogMessage(string message)
    {
        debugMessage = message;
        //if (enableLogging) Debug.Log($"[MovingSound] {message}", this);
    }

    private void LogWarning(string message)
    {
        debugMessage = "WARNING: " + message;
        //if (enableLogging) Debug.LogWarning($"[MovingSound] {message}", this);
    }

    private void UpdateDebugDisplay()
    {
        if (debugText != null)
        {
            string ownerStatus = photonView.IsMine ? "Owner" : "Remote";
            string audioStatus = audioSource.isPlaying ? "PLAYING" : "STOPPED";
            string movementStatus = isMoving ? "MOVING" : "IDLE";
            string velocity = rb != null ? rb.linearVelocity.x.ToString("F3") : "N/A";

            debugText.text = $"Moving Sound Debug:\n" +
                             $"Status: {ownerStatus}\n" +
                             $"Audio: {audioStatus}\n" +
                             $"Movement: {movementStatus}\n" +
                             $"Should Play: {shouldPlayAudio}\n" +
                             $"Velocity: {velocity}\n" +
                             $"Input: {inputValue:F2}\n" +
                             $"Message: {debugMessage}";
        }
    }

    // Draw debug information in the Scene view - Editor only
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showSceneDebug || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontSize = 12;
        
        Vector3 position = transform.position + Vector3.up * 1.5f;
        
        string ownerStatus = photonView.IsMine ? "Owner" : "Remote";
        string audioStatus = audioSource != null && audioSource.isPlaying ? "PLAYING" : "STOPPED";
        string movementStatus = isMoving ? "MOVING" : "IDLE";
        string velocity = rb != null ? rb.linearVelocity.x.ToString("F3") : "N/A";
        
        string debugInfo = $"{ownerStatus} | {movementStatus}\n" +
                           $"Audio: {audioStatus}\n" +
                           $"Velocity: {velocity}\n" +
                           $"Input: {inputValue:F2}";
        
        UnityEditor.Handles.Label(position, debugInfo, style);
    }
#endif
    #endregion
}