using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public abstract class gun : MonoBehaviourPun // 改为继承 MonoBehaviourPun
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
    protected float flipX;
    protected Animator animator;
    protected PlayerMovement playerMovement;
    protected HealthSystem healthSystem;
    protected PhotonView parentPhotonView;

    // 公共接口，用於從外部安全地獲取彈殼生成位置
    public Transform ShellPosition => shellPos;

    protected virtual void Awake()
    {
        flipY = Mathf.Abs(transform.localScale.y);
        flipX = Mathf.Abs(transform.localScale.x);
        animator = GetComponent<Animator>();
        if (muzzlePos == null) muzzlePos = transform.Find("Muzzle");
        if (shellPos == null) shellPos = transform.Find("BulletShell");
    }

    protected virtual void Start()
    {
        flipY = transform.localScale.y;
        flipX = transform.localScale.x;
        parentPhotonView = GetComponentInParent<PhotonView>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        healthSystem = GetComponentInParent<HealthSystem>();
    }

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
    }

    protected virtual void UpdateWeaponRotation()
    {
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

    protected virtual void HandleShootingInput()
    {
        // 原有的状态检查（死亡/对话/商店）
        if ((healthSystem != null && healthSystem.IsDead) ||
            (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive) ||
            (ShopManager.Instance.IsOpen))
            return;

        if (timer > 0)
        {
            timer -= Time.deltaTime;
            return;
        }

        if (Input.GetButton("Fire1") && timer <= 0)
        {
            // 更精确的UI检测 - 只检测有Button组件的UI元素
            bool canShoot = true;

            if (EventSystem.current != null)
            {
                // 创建指针事件数据
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                // 收集所有被射线击中的UI结果
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                // 检查是否有可交互的UI元素（如按钮）
                foreach (var result in results)
                {
                    // 如果命中的UI有Button组件，则阻止射击
                    if (result.gameObject.GetComponent<UnityEngine.UI.Button>() != null)
                    {
                        canShoot = false;
                        break;
                    }
                }
            }

            if (canShoot)
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
    }
    /// <summary>
    /// 預設的開火行為，現在改為只在本地生成子彈
    /// </summary>
    protected virtual void Fire()
    {
        if (muzzlePos == null || parentPhotonView == null) return;

        // 只在本地客户端生成子弹（网络对象）
        if (parentPhotonView.IsMine)
        {
            // 使用 PhotonNetwork.Instantiate 创建网络同步的子弹
            GameObject bulletObj = PhotonNetwork.Instantiate(
                bulletPrefab.name,
                muzzlePos.position,
                transform.rotation
            );

            // 获取子弹组件并初始化
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Initialize(damage, parentPhotonView);
            }
        }

        // 播放开火效果（所有客户端）
        PlayFireEffects(muzzlePos.position);
    }

    /// <summary>
    /// 播放开火效果（PUN RPC方法）
    /// </summary>
    [PunRPC]
    protected void PlayFireEffects(Vector3 position)
    {
        // 生成弹壳（本地效果）
        if (shellPrefab != null)
        {
            Instantiate(shellPrefab, shellPos.position, shellPos.rotation);
        }

        // 播放射击音效
        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, position);
        }

        // 播放射击动画
        if (animator != null)
        {
            animator.SetTrigger("Fire");
        }
    }
}