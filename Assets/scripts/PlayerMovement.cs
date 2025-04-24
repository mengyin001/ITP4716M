using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    public GameObject[] guns;       //Gun list
    public int gunNum = 0;
    private Vector2 mousePos;
    private float flipY;
    public GameObject myBag;
    public bool isOpen;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        guns[0].SetActive(true);    //default gun0 active
        flipY = transform.localScale.y;
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gunNum = PlayerData.CurrentGunIndex;
        guns[gunNum].SetActive(true);

        // 确保加载库存
        var inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.LoadInventory();
        }

        if (myBag == null)
        {
            Debug.LogError("myBag reference is not set in PlayerMovement script.");
        }
    }

    void OnDisable()
    {
        PlayerData.CurrentGunIndex = gunNum;
    }

    void Update()
    {
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            return;
        if (ShopManager.Instance != null && ShopManager.Instance.isOPen)
            return;
        OpenMyBag();
        SwitchGun();
        movement.x = Input.GetAxis("Horizontal");
        movement.y = Input.GetAxis("Vertical");

        float speed = movement.magnitude;
        animator.SetFloat("speed", speed);
        UpdateAnimation(movement);

        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (mousePos.x < transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void OpenMyBag()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            isOpen = !isOpen;
            if (myBag != null)
            {
                myBag.SetActive(isOpen);
            }
            else
            {
                Debug.LogWarning("myBag is null, cannot set active state.");
            }
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void UpdateAnimation(Vector2 movement)
    {
        bool isWalk = movement.magnitude > 0;
        animator.SetBool("isWalk", isWalk);
    }

    void SwitchGun()
    {
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            return;
        if (ShopManager.Instance != null && ShopManager.Instance.isOPen)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (guns[gunNum] != null)
            {
                guns[gunNum].SetActive(false);
            }
            if (--gunNum < 0)
            {
                gunNum = guns.Length - 1;
            }
            if (guns[gunNum] != null)
            {
                guns[gunNum].SetActive(true);
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (guns[gunNum] != null)
            {
                guns[gunNum].SetActive(false);
            }
            if (++gunNum > guns.Length - 1)
            {
                gunNum = 0;
            }
            if (guns[gunNum] != null)
            {
                guns[gunNum].SetActive(true);
            }
        }
    }
}