// gun.cs (modified with fixes for derived classes)
using UnityEngine;

public abstract class gun : MonoBehaviour
{
    [Header("射击设置")]
    public float interval = 0.5f;
    public GameObject bulletPrefab;  // 确保在Inspector中赋值
    public GameObject shellPrefab;   // 可选
    public float energyCostPerShot = 1f;
    public AudioClip shootSound;     // 可选
    
    [Header("组件参考")]
    [SerializeField] protected Transform muzzlePos;  // 序列化以便调试
    [SerializeField] protected Transform shellPos;   // 可选
    
    protected Vector2 mousePos;
    protected Vector2 direction;
    protected float timer = 0;
    protected float flipY;
    protected Animator animator;
    protected PlayerMovement playerMovement;
    protected HealthSystem healthSystem;

    // Fixed virtual method signatures
    protected virtual void Awake()
    {
        // 确保关键组件在Awake中初始化
        animator = GetComponent<Animator>();
        
        // 确保枪口位置存在
        if (muzzlePos == null)
        {
            muzzlePos = transform.Find("Muzzle");
            if (muzzlePos == null)
            {
                Debug.LogError($"武器 {name} 缺少Muzzle子物体", this);
                muzzlePos = new GameObject("Muzzle").transform;
                muzzlePos.SetParent(transform);
                muzzlePos.localPosition = Vector3.right * 0.5f;
            }
        }
        
        // 弹壳位置可选
        if (shellPos == null)
        {
            shellPos = transform.Find("ShellPos");
        }
    }

    protected virtual void Start()
    {
        flipY = transform.localScale.y;
        
        // 动态查找组件
        playerMovement = GetComponentInParent<PlayerMovement>();
        healthSystem = GetComponentInParent<HealthSystem>();
        
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            Debug.LogWarning($"武器 {name} 动态查找PlayerMovement", this);
        }
        
        if (healthSystem == null)
        {
            healthSystem = FindObjectOfType<HealthSystem>();
            Debug.LogWarning($"武器 {name} 动态查找HealthSystem", this);
        }
        
        // 初始检查
        if (bulletPrefab == null)
        {
            Debug.LogError($"武器 {name} 未设置bulletPrefab", this);
        }
    }

<<<<<<< Updated upstream
    protected virtual void Update()
    {
        if (Camera.main == null) return;
        
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        UpdateWeaponRotation();
        
        if (playerMovement != null && !playerMovement.isOpen)
        {
            Shoot();
        }
    }

    protected virtual void UpdateWeaponRotation()
{
    direction = (mousePos - (Vector2)transform.position).normalized;
    
    // 计算基础角度
    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    
    // 鼠标在左侧
    if (mousePos.x < transform.position.x)
    {
        transform.localScale = new Vector3(-flipY, flipY, 1); // Y轴翻转使枪托向下
        transform.rotation = Quaternion.Euler(0, 0, angle + 180f); // 加180度使武器朝左
    }
    // 鼠标在右侧
    else
    {
        transform.localScale = new Vector3(-flipY, flipY, 1); // 保持原有右侧翻转
        transform.rotation = Quaternion.Euler(0, 0, angle); // 直接使用计算角度
=======
    // Fixed virtual method signature
    protected virtual void Update()
    {
        // 只让本地玩家控制自己的武器
        if (parentPhotonView == null || !parentPhotonView.IsMine) return;

        if (Camera.main == null) return;
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        UpdateWeaponRotation();

        // 修复这里的访问方式
        if (playerMovement != null &&
            (UIManager.Instance == null || !UIManager.Instance.IsBagOpen))
        {
            HandleShootingInput();
        }
>>>>>>> Stashed changes
    }

    protected virtual void Shoot()
    {
<<<<<<< Updated upstream
        if ((healthSystem != null && healthSystem.IsDead) || 
=======
        if (parentPhotonView == null || !parentPhotonView.IsMine)
        {
            // 对于远程玩家，PhotonTransformView 会自动同步其 Transform
            return;
        }

        direction = (mousePos - (Vector2)transform.position).normalized;
        transform.right = direction;

        if (mousePos.x < transform.position.x)
        {
            // 鼠标在左边，翻转Y轴
            transform.localScale = new Vector3(Mathf.Abs(flipX), -Mathf.Abs(flipY), transform.localScale.z);
        }
        else
        {
            // 鼠标在右边，保持Y轴正向
            transform.localScale = new Vector3(-Mathf.Abs(flipX), Mathf.Abs(flipY), transform.localScale.z);
        }
    }

    // Fixed virtual method signature
    protected virtual void HandleShootingInput()
    {
        if ((healthSystem != null && healthSystem.IsDead) ||
>>>>>>> Stashed changes
            (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive))
            return;
            
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            return;
        }

        if ((Input.GetButton("Fire1") || Input.GetButtonDown("Fire1")) && timer <= 0)
        {
            if (healthSystem == null || healthSystem.HasEnoughEnergy(energyCostPerShot))
            {
                Fire();
                timer = interval;
                
                if (healthSystem != null)
                    healthSystem.ConsumeEnergy(energyCostPerShot);
            }
            else
            {
                Debug.Log("能量不足！");
            }
        }
    }

    protected virtual void Fire()
    {
        // 安全检查
        if (animator != null)
            animator.SetTrigger("Shoot");
        else
            Debug.LogWarning($"武器 {name} 缺少Animator组件", this);
        
        if (bulletPrefab == null || muzzlePos == null)
        {
            Debug.LogError($"武器 {name} 无法射击 - bulletPrefab或muzzlePos未设置", this);
            return;
        }
        
        // 生成子弹
        GameObject bullet = Instantiate(bulletPrefab, muzzlePos.position, Quaternion.identity);
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.SetSpeed(direction);
        }
        else
        {
            Debug.LogWarning($"子弹预制体 {bulletPrefab.name} 缺少Bullet组件", this);
        }
        
        // 生成弹壳（可选）
        if (shellPrefab != null && shellPos != null)
        {
            Instantiate(shellPrefab, shellPos.position, Quaternion.identity);
        }
        
        // 播放音效
        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, muzzlePos.position);
        }
        else
        {
            Debug.LogWarning($"武器 {name} 缺少射击音效", this);
        }
    }
<<<<<<< Updated upstream
=======

    [PunRPC]
    protected void RPC_FireSingle(Vector3 position, Quaternion rotation, float damage)
    {
        GameObject bullet = Instantiate(bulletPrefab, position, rotation);
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.Initialize(damage, parentPhotonView);
        }
    }
>>>>>>> Stashed changes
}