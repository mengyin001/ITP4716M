using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPun
{
    [Header("移动参数")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    public GameObject[] guns;       //Gun list
    int gunNum = 0;
    private Vector2 mousePos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        guns[0].SetActive(true);    //default gun0 active
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    void Update()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
            return;
        OpenMyBag();
        bool isBagOpen = UIManager.Instance != null && UIManager.Instance.IsBagOpen;
        if (isBagOpen) 
            return;
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            return;
        if (ShopManager.Instance != null && ShopManager.Instance.isOpen)
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
        }
        else
        {
            // Face right
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }
    void OpenMyBag()
    {
        bool canToggle = true;

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            canToggle = false;

        if (ShopManager.Instance != null && ShopManager.Instance.isOpen)
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
        if (UIManager.Instance != null && UIManager.Instance.IsBagOpen)
            return;
        // 应用移动（保留原有物理逻辑）
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
    private void UpdateAnimation(Vector2 movement)
    {
        // 计算动画参数
        bool isWalk = movement.magnitude > 0; // 判断角色是否在移动
        animator.SetBool("isWalk", isWalk); // 设置动画参数
    }

    void SwitchGun(){
        if (UIManager.Instance != null && UIManager.Instance.IsBagOpen)
            return;
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            return;
        if (ShopManager.Instance != null && ShopManager.Instance.isOpen)
            return;

        if(Input.GetKeyDown(KeyCode.Q)){
            guns[gunNum].SetActive(false);
            if (++gunNum > guns.Length - 1){
                gunNum = 0;
            }
            guns[gunNum].SetActive(true);
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