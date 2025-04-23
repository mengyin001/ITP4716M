using System;
using UnityEngine;

public class ChestInteraction2D : MonoBehaviour
{
    [Header("玩家交互设置")]
    public Transform player;
    public float interactionDistance = 3f;
    private float requiredChestZ = -1f;

    [Header("物品生成设置")]
    public GameObject[] item2DPrefabs;
    public Transform drop2DPoint;
    public float itemZPosition = -2f;
    public Vector2 colliderSize = new Vector2(0.8f, 0.8f); // 新增：碰撞器尺寸

    private Animator mAnimator;
    private bool isOpened = false;

    public event Action<ChestInteraction2D> OnChestOpened;
    public bool IsOpened => isOpened; // 新增状态访问器

    void Start()
    {
        mAnimator = GetComponent<Animator>();
        
        // 自动设置掉落点
        if (drop2DPoint == null)
            drop2DPoint = transform;
    }

    void Update()
    {
        // 计算X/Y平面的二维距离
        Vector2 chestPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 playerPos = new Vector2(player.position.x, player.position.y);
        float distance = Vector2.Distance(chestPos, playerPos);


        if (!isOpened && 
            distance <= interactionDistance &&
            Input.GetKeyDown(KeyCode.E))
        {
            OpenChest();
        }
    }

    void OpenChest()
    {
        mAnimator.SetTrigger("Open");
        Drop2DItem();
        isOpened = true;
        OnChestOpened?.Invoke(this);
    }

    void Drop2DItem()
    {
        if (item2DPrefabs.Length == 0)
        {
            Debug.LogError("未配置物品预制体！");
            return;
        }

        // 创建三维生成位置
        Vector3 spawnPos = new Vector3(
            drop2DPoint.position.x,
            drop2DPoint.position.y,
            itemZPosition
        );

        // 实例化物品
        GameObject selectedPrefab = item2DPrefabs[UnityEngine.Random.Range(0, item2DPrefabs.Length)];
        GameObject item = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

    }

    // 可视化调试
    void OnDrawGizmosSelected()
    {
        // 绘制交互范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        // 绘制有效Z轴区域
        Gizmos.color = Color.cyan;
        Vector3 chestPos = transform.position;
        chestPos.z = requiredChestZ;
        Gizmos.DrawWireCube(chestPos, new Vector3(1, 1, 0.1f));

        // 绘制物品生成位置
        if (drop2DPoint != null)
        {
            Gizmos.color = Color.green;
            Vector3 spawnPos = drop2DPoint.position;
            spawnPos.z = itemZPosition;
            Gizmos.DrawWireCube(spawnPos, colliderSize);
        }
    }
}