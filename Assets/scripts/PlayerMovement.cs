using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class PlayerMovement : MonoBehaviourPun, IPunObservable
{
    [Header("移动参数")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    [Header("枪械")]
    public GameObject[] guns;       // Gun list
    private int gunNum = 0;
    private Vector2 mousePos;
    [Header("角色名字")]
    public TextMeshProUGUI playerName;
    private RectTransform playerNameRectTransform;
    private Vector3 originalNameScale;

    // 新增：存储当前朝向状态
    private bool isFacingRight = false;

    // 用于存储每个武器的状态
    private Dictionary<int, bool> weaponStates = new Dictionary<int, bool>();

    // 网络同步的武器索引
    private int currentWeaponIndex = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // 初始化武器状态
        InitializeWeapons();

        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (playerName != null)
        {
            playerNameRectTransform = playerName.GetComponent<RectTransform>();
            originalNameScale = playerNameRectTransform.localScale;

            // 初始化名字标签
            if (photonView.IsMine)
                playerName.text = PhotonNetwork.NickName;
            else
                playerName.text = photonView.Owner.NickName;

            // 初始更新名字标签
            UpdateNameTagOrientation();
        }
    }

    // 初始化武器状态（本地和网络）
    private void InitializeWeapons()
    {
        // 禁用所有武器
        for (int i = 0; i < guns.Length; i++)
        {
            guns[i].SetActive(false);
            weaponStates[i] = false;
        }

        // 激活默认武器
        if (guns.Length > 0)
        {
            // 使用 RPC 同步武器状态
            photonView.RPC("RPC_ActivateWeapon", RpcTarget.AllBuffered, 0);
        }
    }

    void Update()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
        {
            // 远程玩家也需要更新名字标签
            UpdateNameTagOrientation();
            return;
        }

        OpenMyBag();

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            return;

        if (ShopManager.Instance != null && ShopManager.Instance.IsOpen)
            return;

        SwitchGun();

        // 获取输入并计算移动速度
        movement.x = Input.GetAxis("Horizontal");
        movement.y = Input.GetAxis("Vertical");

        // 计算实际移动速度（矢量长度）
        float speed = movement.magnitude; // Use magnitude for speed

        // 更新 Animator 参数
        animator.SetFloat("speed", speed);
        UpdateAnimation(movement);

        // 反转角色
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);     //flip
        if (mousePos.x < transform.position.x)
        {
            // Face left
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            isFacingRight = false;
        }
        else
        {
            // Face right
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            isFacingRight = true;
        }

        // 本地玩家更新名字标签
        UpdateNameTagOrientation();
    }

    // 新增：统一更新名字标签方向的方法
    private void UpdateNameTagOrientation()
    {
        if (playerNameRectTransform == null) return;

        // 根据当前朝向设置名字标签缩放
        if (isFacingRight)
        {
            // 朝右时：X轴取反抵消父物体翻转
            playerNameRectTransform.localScale = new Vector3(
                -Mathf.Abs(originalNameScale.x),
                originalNameScale.y,
                originalNameScale.z
            );
        }
        else
        {
            // 朝左时：使用原始缩放
            playerNameRectTransform.localScale = originalNameScale;
        }
    }

    void OpenMyBag()
    {
        bool canToggle = true;

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            canToggle = false;

        if (ShopManager.Instance != null && ShopManager.Instance.IsOpen)
            canToggle = false;

        if (Input.GetKeyDown(KeyCode.B) && canToggle)
        {
            UIManager.Instance?.ToggleBag();
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
            return;

        // 移除了背包打开时的移动限制
        // 应用移动（保留原有物理逻辑）
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void UpdateAnimation(Vector2 movement)
    {
        // 计算动画参数
        bool isWalk = movement.magnitude > 0; // 判断角色是否在移动
        animator.SetBool("isWalk", isWalk); // 设置动画参数
    }

    void SwitchGun()
    {
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            return;

        if (ShopManager.Instance != null && ShopManager.Instance.IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            // 计算新武器索引
            int newGunNum = (gunNum + 1) % guns.Length;

            // 通过 RPC 同步武器切换
            photonView.RPC("RPC_SwitchWeapon", RpcTarget.AllBuffered, newGunNum);
        }
    }

    // 网络同步的武器切换方法
    [PunRPC]
    private void RPC_SwitchWeapon(int newWeaponIndex)
    {
        // 确保索引有效
        if (newWeaponIndex < 0 || newWeaponIndex >= guns.Length)
            return;

        // 禁用当前武器
        guns[gunNum].SetActive(false);
        weaponStates[gunNum] = false;

        // 更新索引
        gunNum = newWeaponIndex;
        currentWeaponIndex = newWeaponIndex;

        // 启用新武器
        guns[gunNum].SetActive(true);
        weaponStates[gunNum] = true;

        // 调试信息
        Debug.Log($"Switched to weapon {gunNum} on player {photonView.Owner.NickName}");
    }

    // 激活武器的网络方法
    [PunRPC]
    private void RPC_ActivateWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= guns.Length)
            return;

        // 禁用所有武器
        foreach (var gun in guns)
        {
            gun.SetActive(false);
        }

        // 激活指定武器
        guns[weaponIndex].SetActive(true);
        gunNum = weaponIndex;
        currentWeaponIndex = weaponIndex;
        weaponStates[weaponIndex] = true;
    }

    // 在 Photon 同步数据时调用 - 修复了同步问题
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 发送当前武器索引和朝向状态
            stream.SendNext(currentWeaponIndex);
            stream.SendNext(isFacingRight);
        }
        else
        {
            // 接收武器索引并更新
            currentWeaponIndex = (int)stream.ReceiveNext();

            // 接收朝向状态并更新
            isFacingRight = (bool)stream.ReceiveNext();

            // 更新名字标签方向
            UpdateNameTagOrientation();

            // 应用武器切换
            if (currentWeaponIndex != gunNum)
            {
                // 禁用当前武器
                if (gunNum >= 0 && gunNum < guns.Length)
                {
                    guns[gunNum].SetActive(false);
                }

                // 启用新武器
                gunNum = currentWeaponIndex;
                if (gunNum >= 0 && gunNum < guns.Length)
                {
                    guns[gunNum].SetActive(true);
                }
            }
        }
    }

    void OnDestroy()
    {
        // 确保使用 Photon 方式销毁
        if (photonView != null && photonView.IsMine && PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}