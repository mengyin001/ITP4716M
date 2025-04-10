using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;

public class Enemy : Character
{

    public UnityEvent<Vector2> OnMovementInput;
    public UnityEvent OnAttack;

    [SerializeField]
    private Transform player;

    [SerializeField] private float chaseDistance = 3f;//追击距离
    [SerializeField] private float attackDistance = 0.8f;//攻击距离

    private Seeker seeker;
    private List<Vector3> pathPointList;//路径点列表
    private int currentIndex = 0;//路径点的索引
    private float pathGenerateInterval = 0.5f; //每0.5秒生成一次路径
    private float pathGenerateTimer = 0f;//计时器

    [Header("攻击")]
    public float meleetAttackDamage;//近战攻击伤害
    public LayerMask playerLayer;//表示玩家图层
    public float AttackCooldownDuration = 2f;//冷却时间

    private bool isAttack = true;

    private SpriteRenderer sr;
    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance = Vector2.Distance(player.position, transform.position);

        if (distance < chaseDistance) // 小于追击距离
        {
            AutoPath();
            if (pathPointList == null)
                return;
            if (distance <= attackDistance) // 是否处于攻击距离
            {

                // 攻击玩家
                OnMovementInput?.Invoke(Vector2.zero); // 停止移动
                if (isAttack)
                {
                    isAttack = false;
                    OnAttack?.Invoke();// 触发攻击事件
                    StartCoroutine(nameof(AttackCooldownCoroutine));// 启动攻击冷却时间
                }

                //人物翻转
                float x = player.position.x - transform.position.x;
                if (x > 0)
                {
                    sr.flipX = true;
                }
                else
                {
                    sr.flipX = false;
                }
            }
            else
            {
                // 追击玩家
                //Vector2 direction = player.position - transform.position;
                if (currentIndex >= 0 && currentIndex < pathPointList.Count)
                {
                    Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
                    OnMovementInput?.Invoke(direction.normalized);
                }
                else
                {
                    // 处理索引无效的情况，例如重置索引
                    currentIndex = 0; // 或者你可以选择其他逻辑
                }

            }
        }
        else
        {
            // 放弃追击
            OnMovementInput?.Invoke(Vector2.zero);
        }
    }
    //自动寻路
    private void AutoPath()
    {
        pathGenerateTimer += Time.deltaTime;

        //间隔一定时间来获取路径点
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            GeneratePath(player.position);
            pathGenerateTimer = 0;//重置计时器
        }


        //当路径点列表为空时，进行路径计算
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            GeneratePath(player.position);
        }//当敌人到达当前路径点时，递增索引currentIndex并进行路径计算
        else if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= pathPointList.Count)
                GeneratePath(player.position);
        }
    }

    //获取路径点
    private void GeneratePath(Vector3 target)
    {
        currentIndex = 0;
        //三个参数：起点、终点、回调函数
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath;//Path.vectorPath包含了从起点到终点的完整路径
        });
    }
    //敌人近战攻击
    private void MeleeAttackEvent()
    {
        //检测碰撞
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            hitCollider.GetComponent<PlayerHealth>().TakeDamage(meleetAttackDamage);
        }
    }
    //攻击冷却时间

    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration);// 等待冷却时间
        isAttack = true;// 重置攻击状态
    }

    public void OnDrawGizmosSelected()
    {
        //显示攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
        //显示追击范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

    }
}