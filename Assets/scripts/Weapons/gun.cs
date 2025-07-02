using UnityEngine;
using Photon.Pun;

public abstract class gun : MonoBehaviour
{
    [Header("通用射击设置")]
    public float interval = 0.5f;
    public GameObject bulletPrefab;
    public GameObject shellPrefab;
    public float energyCostPerShot = 1f;
    public AudioClip shootSound;
    public float damage = 10f;

    [Header("通用组件参考")]
    [SerializeField] protected Transform muzzlePos;
    [SerializeField] protected Transform shellPos;

    protected Vector2 mousePos;
    protected Vector2 direction;
    protected float timer = 0;
    protected float flipY;
    protected Animator animator;
    protected PlayerMovement playerMovement;
    protected HealthSystem healthSystem;
    protected PhotonView parentPhotonView;

    // 公共接口，用於從外部安全地獲取彈殼生成位置
    public Transform ShellPosition => shellPos;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        if (muzzlePos == null) muzzlePos = transform.Find("Muzzle");
        if (shellPos == null) shellPos = transform.Find("BulletShell");
    }

    protected virtual void Start()
    {
        flipY = transform.localScale.y;
        parentPhotonView = GetComponentInParent<PhotonView>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        healthSystem = GetComponentInParent<HealthSystem>();
    }

    protected virtual void Update()
    {
        // 只讓本地玩家控制自己的武器
        if (parentPhotonView == null || !parentPhotonView.IsMine) return;

        if (Camera.main == null) return;
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        UpdateWeaponRotation();

        if (playerMovement != null && !playerMovement.isOpen)
        {
            HandleShootingInput();
        }
    }

    protected virtual void UpdateWeaponRotation()
    {
        direction = (mousePos - (Vector2)transform.position).normalized;
        transform.right = direction;

        if (mousePos.x < transform.position.x)
        {
            transform.localScale = new Vector3(transform.localScale.x, -flipY, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(transform.localScale.x, flipY, transform.localScale.z);
        }
    }

    protected virtual void HandleShootingInput()
    {
        if ((healthSystem != null && healthSystem.IsDead) ||
            (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive))
            return;

        if (timer > 0)
        {
            timer -= Time.deltaTime;
            return;
        }

        if (Input.GetButton("Fire1") && timer <= 0)
        {
            if (healthSystem == null || healthSystem.HasEnoughEnergy(energyCostPerShot))
            {
                Fire();
                timer = interval;
                if (healthSystem != null)
                    healthSystem.RPC_ConsumeEnergy(energyCostPerShot);
            }
        }
    }

    /// <summary>
    /// 預設的開火行為，觸發單發子彈的 RPC。子類可以重寫此方法以實現不同邏輯。
    /// </summary>
    protected virtual void Fire()
    {
        if (muzzlePos == null || parentPhotonView == null) return;

        // 呼叫 RPC 在所有客戶端生成子彈
        parentPhotonView.RPC("RPC_FireSingle", RpcTarget.All, muzzlePos.position, transform.rotation, this.damage);

        // 呼叫 RPC 在所有客戶端播放特效
        parentPhotonView.RPC("PlayFireEffects", RpcTarget.All, muzzlePos.position);
    }
}
