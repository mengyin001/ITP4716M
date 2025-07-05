using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // 添加PUN2命名空间
using Photon.Realtime;

public class EnemyController : MonoBehaviourPun, IPunObservable // 修改基类并实现同步接口
{
    [Header("Attack Setting")]
    [SerializeField] private float currentSpeed = 0;
    [SerializeField] private float attackCoolDuration = 1;
    public Vector2 MovementInput { get; set; }

    [Header("Sound Effects")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioSource audioSource;

    private Rigidbody2D rb;
    private Collider2D enemyCollider;
    private SpriteRenderer sr;
    private Animator anim;

    // 网络同步变量
    private Vector2 networkPosition;
    private float networkRotation;
    private bool isAttack = true;
    private bool isHurt;
    private bool isDie;

    // 平滑同步参数
    private float lastSynchronizationTime = 0f;
    private float syncDelay = 0f;
    private float syncTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void FixedUpdate()
    {
        // 只有主机控制敌人逻辑
        if (PhotonNetwork.IsMasterClient)
        {
            if (!isHurt && !isDie)
                Move();
        }
        else // 其他客户端进行位置插值
        {
            SyncRemoteEnemy();
        }

        SetAnimation();
    }

    void Move()
    {
        if (MovementInput.magnitude > 0.1f && currentSpeed >= 0)
        {
            Vector2 targetVelocity = MovementInput * currentSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);

            if (MovementInput.x < 0) sr.flipX = false;
            if (MovementInput.x > 0) sr.flipX = true;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // 同步远程敌人位置
    private void SyncRemoteEnemy()
    {
        syncTime += Time.deltaTime;
        rb.position = Vector2.Lerp(rb.position, networkPosition, syncTime / syncDelay);
    }

    public void Attack()
    {
        if (isAttack && photonView.IsMine)
        {
            isAttack = false;
            photonView.RPC("RPC_Attack", RpcTarget.All);
            StartCoroutine(nameof(AttackCoroutine));
        }
    }

    [PunRPC]
    private void RPC_Attack()
    {
        anim.SetTrigger("isAttack");
        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound);
    }

    IEnumerator AttackCoroutine()
    {
        yield return new WaitForSeconds(attackCoolDuration);
        isAttack = true;
    }

    public void EnemyHurt()
    {
        photonView.RPC("RPC_EnemyHurt", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_EnemyHurt()
    {
        anim.SetTrigger("isHurt");
    }

    public void EnemyDead()
    {
        photonView.RPC("RPC_EnemyDead", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_EnemyDead()
    {
        rb.linearVelocity = Vector2.zero;
        isDie = true;
        enemyCollider.enabled = false;
    }

    void SetAnimation()
    {
        anim.SetBool("isWalk", MovementInput.magnitude > 0);
        anim.SetBool("isDie", isDie);
    }

    public void DestroyEnemy()
    {
        if (photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
    }

    // 网络同步数据
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 主机发送数据
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            stream.SendNext(isDie);
            stream.SendNext(sr.flipX);
            stream.SendNext(MovementInput);
        }
        else
        {
            // 客户端接收数据
            networkPosition = (Vector2)stream.ReceiveNext();
            networkRotation = (float)stream.ReceiveNext();
            isDie = (bool)stream.ReceiveNext();
            sr.flipX = (bool)stream.ReceiveNext();
            MovementInput = (Vector2)stream.ReceiveNext();

            // 计算同步延迟
            syncTime = 0f;
            syncDelay = Time.time - lastSynchronizationTime;
            lastSynchronizationTime = Time.time;
        }
    }
}