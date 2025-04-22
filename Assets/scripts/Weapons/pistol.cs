using UnityEngine;

public class pistol : MonoBehaviour
{
    public float interval;
    public GameObject bulletPrefab;
    public GameObject shellPrefab;
    public float energyCostPerShot = 1f;
    public AudioClip shootSound; // 新增的音效变量
    private Transform muzzlePos;
    private Transform shellPos;
    private Vector2 mousePos;
    private Vector2 direction;
    private float timer = 0;
    private float flipY;
    private Animator animator;
    private PlayerMovement playerMovement;
    private HealthSystem healthSystem;

    void Start()
    {
        animator = GetComponent<Animator>();
        muzzlePos = transform.Find("Muzzle");
        shellPos = transform.Find("BulletShell");
        flipY = transform.localScale.y;
        healthSystem = GetComponent<HealthSystem>(); 
        playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
        }
        if (healthSystem == null)
        {
            healthSystem = FindObjectOfType<HealthSystem>();
            Debug.Log(healthSystem == null ? "警告：未找到HealthSystem组件" : "已关联HealthSystem");
        }
    }

    void Update()
    {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (mousePos.x < transform.position.x)
        {
            transform.localScale = new Vector3(flipY, -flipY, 1);
        }
        else
        {
            transform.localScale = new Vector3(-flipY, flipY, 1);
        }
        if (!playerMovement.isOpen)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            return;
        direction = (mousePos - new Vector2(transform.position.x, transform.position.y)).normalized;
        transform.right = direction;
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }

        if ((Input.GetButton("Fire1") || Input.GetButtonDown("Fire1")) && timer <= 0)
        {
            // Check if we have enough energy to shoot
            if (healthSystem != null && healthSystem.HasEnoughEnergy(energyCostPerShot))
            {
                Fire();
                timer = interval;
                healthSystem.ConsumeEnergy(energyCostPerShot);
            }
            else
            {
                // Optional: Play a "no energy" sound or show feedback
                Debug.Log("Not enough energy to shoot!");
            }
        }
    }

    void Fire()
    {
        animator.SetTrigger("Shoot");
        GameObject bullet = Instantiate(bulletPrefab, muzzlePos.position, Quaternion.identity);
        bullet.GetComponent<Bullet>().SetSpeed(direction);

        // 新增音效播放代码
        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, muzzlePos.position);
        }
    }
}