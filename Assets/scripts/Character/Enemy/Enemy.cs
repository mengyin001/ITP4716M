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

    [SerializeField] private float chaseDistance = 3f;//׷������
    [SerializeField] private float attackDistance = 0.8f;//��������

    private Seeker seeker;
    private List<Vector3> pathPointList;//·�����б�
    private int currentIndex = 0;//·���������
    private float pathGenerateInterval = 0.5f; //ÿ0.5������һ��·��
    private float pathGenerateTimer = 0f;//��ʱ��

    [Header("����")]
    public float meleetAttackDamage;//��ս�����˺�
    public LayerMask playerLayer;//��ʾ���ͼ��
    public float AttackCooldownDuration = 2f;//��ȴʱ��

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

        if (distance < chaseDistance) // С��׷������
        {
            AutoPath();
            if (pathPointList == null)
                return;
            if (distance <= attackDistance) // �Ƿ��ڹ�������
            {

                // �������
                OnMovementInput?.Invoke(Vector2.zero); // ֹͣ�ƶ�
                if (isAttack)
                {
                    isAttack = false;
                    OnAttack?.Invoke();// ���������¼�
                    StartCoroutine(nameof(AttackCooldownCoroutine));// ����������ȴʱ��
                }

                //���﷭ת
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
                // ׷�����
                //Vector2 direction = player.position - transform.position;
                if (currentIndex >= 0 && currentIndex < pathPointList.Count)
                {
                    Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
                    OnMovementInput?.Invoke(direction.normalized);
                }
                else
                {
                    // ����������Ч�������������������
                    currentIndex = 0; // ���������ѡ�������߼�
                }

            }
        }
        else
        {
            // ����׷��
            OnMovementInput?.Invoke(Vector2.zero);
        }
    }
    //�Զ�Ѱ·
    private void AutoPath()
    {
        pathGenerateTimer += Time.deltaTime;

        //���һ��ʱ������ȡ·����
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            GeneratePath(player.position);
            pathGenerateTimer = 0;//���ü�ʱ��
        }


        //��·�����б�Ϊ��ʱ������·������
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            GeneratePath(player.position);
        }//�����˵��ﵱǰ·����ʱ����������currentIndex������·������
        else if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= pathPointList.Count)
                GeneratePath(player.position);
        }
    }

    //��ȡ·����
    private void GeneratePath(Vector3 target)
    {
        currentIndex = 0;
        //������������㡢�յ㡢�ص�����
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath;//Path.vectorPath�����˴���㵽�յ������·��
        });
    }
    //���˽�ս����
    private void MeleeAttackEvent()
    {
        //�����ײ
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            hitCollider.GetComponent<PlayerHealth>().TakeDamage(meleetAttackDamage);
        }
    }
    //������ȴʱ��

    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration);// �ȴ���ȴʱ��
        isAttack = true;// ���ù���״̬
    }

    public void OnDrawGizmosSelected()
    {
        //��ʾ������Χ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
        //��ʾ׷����Χ
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

    }
}