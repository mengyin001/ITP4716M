using UnityEngine;

public class MovingSoundControl : MonoBehaviour
{
    [Header("­µ®Ä°t¸m")]
    [SerializeField] private AudioClip MovingSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 5f;

    private float dirX;
    private bool isMoving = false;
    private HealthSystem healthSystem;
    private bool wasDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        // Get reference to HealthSystem component
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("HealthSystem component not found on the same GameObject!");
        }
    }

    void Update()
    {
        // If health system is missing or character is dead, disable sound and movement
        if (healthSystem == null || healthSystem.IsDead)
        {
            if (!wasDead)
            {
                // First frame detecting death
                audioSource.Stop();
                enabled = false; // Disable this entire component
                wasDead = true;
            }
            return;
        }

        dirX = Input.GetAxis("Horizontal") * moveSpeed;

        if (rb.linearVelocity.x != 0)
            isMoving = true;
        else
            isMoving = false;

        if (isMoving)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
            audioSource.Stop();
    }

    void FixedUpdate()
    {
        // Skip if dead
        if (healthSystem != null && healthSystem.IsDead) return;

        rb.linearVelocity = new Vector2(dirX, rb.linearVelocity.y);
    }
}