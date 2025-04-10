using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    public GameObject[] guns;       //Gun list
    int gunNum = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        guns[0].SetActive(true);    //default gun0 active
    }

    void Update()
    {
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
        if (movement.x < 0)
        {
            transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f)); // Face right
        }
        else if (movement.x > 0)
        {
            transform.rotation = Quaternion.Euler(new Vector3(0f, 180f, 0f)); // Face left
        }
    }

    void FixedUpdate()
    {
        // 应用移动（保留原有物理逻辑）
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
    private void UpdateAnimation(Vector2 movement)
    {
        // 计算动画参数
        bool isMoving = movement.magnitude > 0; // 判断角色是否在移动
        animator.SetBool("isMoving", isMoving); // 设置动画参数
    }

    void SwitchGun(){
        if(Input.GetKeyDown(KeyCode.Q)){            // Q and E switch gun
            guns[gunNum].SetActive(false);
            if (--gunNum < 0){
                gunNum = guns.Length - 1;
            }
            guns[gunNum].SetActive(true);
        }
        if(Input.GetKeyDown(KeyCode.E)){
            guns[gunNum].SetActive(false);
            if (++gunNum > guns.Length - 1){
                gunNum = 0;
            }
            guns[gunNum].SetActive(true);
        }
    }
}