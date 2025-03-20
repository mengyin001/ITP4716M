using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // 移动速度
    private Rigidbody2D rb; // Rigidbody2D 组件
    private Vector2 movement; // 存储移动输入

    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); // 获取 Rigidbody2D 组件
    }

    void Update()
    {
        // 获取水平和垂直输入
        movement.x = Input.GetAxis("Horizontal");
        movement.y = Input.GetAxis("Vertical");
    }

    void FixedUpdate()
    {
        // 移动角色
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}