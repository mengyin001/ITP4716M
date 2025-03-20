using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator; // 新增动画控制器引用

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // 初始化动画组件
    }

    void Update()
    {
        // 获取输入并计算移动速度
        movement.x = Input.GetAxis("Horizontal");
        movement.y = Input.GetAxis("Vertical");

        // 计算实际移动速度（矢量长度）
        float speed = Mathf.Sqrt(movement.x * movement.x + movement.y * movement.y);

        // 更新 Animator 参数
        animator.SetFloat("speed", speed);

        //反轉
        bool flipped = movement.x > 0;
        this.transform.rotation = Quaternion.Euler(new Vector3(0f, flipped ? -180f : 0f, 0f));
    }

    void FixedUpdate()
    {
        // 应用移动（保留原有物理逻辑）
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}