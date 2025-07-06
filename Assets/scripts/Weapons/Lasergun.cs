using UnityEngine;
using Photon.Pun;
using System.Collections;

public class LaserGun : gun
{
    [Header("Laser Settings")]
    public LineRenderer laserBeam;
    public float laserWidth = 0.1f;
    public Color laserColor = Color.green;
    public float maxLaserDistance = 100f;
    public LayerMask hitLayer;
    public LayerMask wallLayer; // 新增墙壁层
    public float laserFadeTime = 0.2f;
    public float energyDrainMultiplier = 20f;
    public float damageDrainMultiplier = 20f;

    [Header("Laser Visual Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem beamImpactParticles;

    public bool isFiring = false;
    private Coroutine firingCoroutine;
    private Coroutine fadeOutCoroutine;
    private Vector2 currentHitPoint;
    private PhotonView myPhotonView;

    // 同步位置变量
    private Vector2 syncStartPos;
    private Vector2 syncEndPos;
    private bool isLaserActive = false;

    // 使用基类的伤害和能量消耗值
    private float DamagePerSecond => damageDrainMultiplier*damage;
    private float EnergyConsumptionPerSecond => energyCostPerShot * energyDrainMultiplier;

    protected override void Awake()
    {
        base.Awake();
        InitializeLaserBeam();

        // 获取武器自身的 PhotonView
        myPhotonView = GetComponent<PhotonView>();
        if (myPhotonView == null)
        {
            Debug.LogError("LaserGun requires a PhotonView component on the same GameObject!");
        }
    }

    private void InitializeLaserBeam()
    {
        if (laserBeam == null)
        {
            laserBeam = gameObject.AddComponent<LineRenderer>();
            laserBeam.positionCount = 2;
        }

        laserBeam.startWidth = laserWidth;
        laserBeam.endWidth = laserWidth;
        laserBeam.material = new Material(Shader.Find("Sprites/Default")) { color = laserColor };
        laserBeam.enabled = false;
    }

    protected override void Update()
    {
        // 只让本地玩家控制输入
        if (parentPhotonView == null || !parentPhotonView.IsMine)
        {
            // 但所有玩家都需要更新激光视觉效果
            if (isLaserActive)
            {
                UpdateLaserVisuals();
            }
            return;
        }

        if (Camera.main == null) return;

        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        UpdateWeaponRotation();

        if (playerMovement != null &&
            (UIManager.Instance == null || !UIManager.Instance.IsBagOpen))
        {
            HandleShootingInput();
        }

        // 本地玩家也需要更新激光视觉效果
        if (isLaserActive)
        {
            UpdateLaserVisuals();
        }
    }

    protected override void HandleShootingInput()
    {
        // 检查角色是否死亡或处于对话中
        if ((healthSystem != null && healthSystem.IsDead) ||
            (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive))
        {
            if (isFiring) StopFiring();
            return;
        }

        // 鼠标按下时开始/持续开火
        if (Input.GetButton("Fire1"))
        {
            // 使用基类的能量消耗值计算消耗
            float energyPerFrame = EnergyConsumptionPerSecond * Time.deltaTime;
            bool canFire = healthSystem == null || healthSystem.HasEnoughEnergy(energyPerFrame);

            if (canFire && !isFiring)
            {
                StartFiring();
            }
            else if (!canFire && isFiring)
            {
                StopFiring();
            }
        }
        // 鼠标松开时立即停止
        else if (isFiring)
        {
            StopFiring();
        }
    }

    private void StartFiring()
    {
        if (!gameObject.activeInHierarchy) return;

        isFiring = true;
        isLaserActive = true;

        // 停止任何正在进行的淡出效果
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
            laserBeam.enabled = true;
            laserBeam.startWidth = laserWidth;
            laserBeam.endWidth = laserWidth;
        }
        else
        {
            laserBeam.enabled = true;
        }

        // 播放枪口闪光效果
        if (muzzleFlash != null && !muzzleFlash.isPlaying)
        {
            muzzleFlash.Play();
        }

        // 使用武器自身的 PhotonView 调用 RPC
        if (myPhotonView != null)
        {
            myPhotonView.RPC("RPC_StartLaser", RpcTarget.All);
        }
        else
        {
            // 本地回退
            RPC_StartLaser();
        }

        // 开始持续伤害协程
        if (firingCoroutine == null)
        {
            firingCoroutine = StartCoroutine(FiringCoroutine());
        }
    }

    private void StopFiring()
    {
        if (!isFiring) return;

        isFiring = false;

        // 使用武器自身的 PhotonView 调用 RPC
        if (myPhotonView != null)
        {
            myPhotonView.RPC("RPC_StopLaser", RpcTarget.All);
        }
        else
        {
            // 本地回退
            RPC_StopLaser();
        }

        // 停止持续伤害协程
        if (firingCoroutine != null)
        {
            StopCoroutine(firingCoroutine);
            firingCoroutine = null;
        }
    }

    private IEnumerator FiringCoroutine()
    {
        while (isFiring && gameObject.activeInHierarchy)
        {
            // 持续消耗能量 - 使用基类的能量消耗值
            if (healthSystem != null)
            {
                float energyPerFrame = EnergyConsumptionPerSecond * Time.deltaTime;
                healthSystem.RPC_ConsumeEnergy(energyPerFrame);
            }

            UpdateLaser();
            yield return null;
        }
    }

    private void UpdateLaser()
    {
        // 计算激光方向
        Vector2 fireDirection = direction;

        // 执行射线检测 - 同时检测墙壁和敌人
        RaycastHit2D hit = Physics2D.Raycast(
            muzzlePos.position,
            fireDirection,
            maxLaserDistance,
            hitLayer | wallLayer // 合并检测层
        );

        // 确定命中点
        currentHitPoint = hit.collider ? hit.point : (Vector2)muzzlePos.position + fireDirection * maxLaserDistance;

        // 更新同步位置（本地玩家直接使用实际位置）
        syncStartPos = muzzlePos.position;
        syncEndPos = currentHitPoint;

        // 更新本地激光视觉效果
        UpdateLaserVisuals();

        // 处理粒子效果
        UpdateImpactParticles(hit.collider != null, currentHitPoint);

        // 处理伤害 - 使用基类的伤害值计算每秒伤害
        if (hit.collider && parentPhotonView != null && parentPhotonView.IsMine)
        {
            // 检查是否击中墙壁
            bool isWallHit = (wallLayer.value & (1 << hit.collider.gameObject.layer)) != 0;

            // 如果不是墙壁，则应用伤害
            if (!isWallHit)
            {
                ApplyDamage(hit.collider);
            }
        }

        // 同步激光位置到其他客户端
        if (myPhotonView != null)
        {
            myPhotonView.RPC("RPC_UpdateLaserPosition", RpcTarget.Others,
                (Vector2)muzzlePos.position, currentHitPoint);
        }
        else
        {
            // 本地回退
            RPC_UpdateLaserPosition((Vector2)muzzlePos.position, currentHitPoint);
        }
    }

    // 更新激光视觉效果（所有玩家都需要调用）
    private void UpdateLaserVisuals()
    {
        if (!isLaserActive) return;

        laserBeam.SetPosition(0, new Vector3(syncStartPos.x, syncStartPos.y, 0));
        laserBeam.SetPosition(1, new Vector3(syncEndPos.x, syncEndPos.y, 0));

        // 更新粒子位置
        if (beamImpactParticles != null)
        {
            if (isFiring || beamImpactParticles.isPlaying)
            {
                beamImpactParticles.transform.position = syncEndPos;

                if (!beamImpactParticles.isPlaying)
                {
                    beamImpactParticles.Play();
                }
            }
            else
            {
                if (beamImpactParticles.isPlaying)
                {
                    beamImpactParticles.Stop();
                }
            }
        }
    }

    private void UpdateImpactParticles(bool hitSomething, Vector2 hitPosition)
    {
        if (beamImpactParticles == null) return;

        if (hitSomething)
        {
            if (!beamImpactParticles.isPlaying)
            {
                beamImpactParticles.Play();
            }
            beamImpactParticles.transform.position = hitPosition;
        }
        else
        {
            if (beamImpactParticles.isPlaying)
            {
                beamImpactParticles.Stop();
            }
        }
    }

    private void ApplyDamage(Collider2D targetCollider)
    {
        Character character = targetCollider.GetComponent<Character>();
        if (character != null)
        {
            // 使用基类的伤害值计算每秒伤害
            float damagePerFrame = DamagePerSecond * Time.deltaTime;

            // 传递bypassInvulnerable=true参数
            character.photonView.RPC("TakeLaserDamage", RpcTarget.All, damagePerFrame);
        }
    }

    [PunRPC]
    public void RPC_StartLaser()
    {
        // 确保在正确的游戏对象上执行
        if (this == null || !this || !gameObject.activeInHierarchy) return;

        // 停止淡出效果（如果有）
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
        }

        laserBeam.enabled = true;
        laserBeam.startWidth = laserWidth;
        laserBeam.endWidth = laserWidth;
        isLaserActive = true;

        if (muzzleFlash != null) muzzleFlash.Play();
    }

    [PunRPC]
    public void RPC_StopLaser()
    {
        // 确保在正确的游戏对象上执行
        if (this == null || !this || !gameObject.activeInHierarchy) return;

        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
        }

        // 仅在对象激活时启动协程
        if (gameObject.activeInHierarchy)
        {
            fadeOutCoroutine = StartCoroutine(FadeOutLaser());
        }
        else
        {
            // 直接禁用激光
            laserBeam.enabled = false;
            isLaserActive = false;
        }

        if (muzzleFlash != null) muzzleFlash.Stop();
        if (beamImpactParticles != null) beamImpactParticles.Stop();
    }

    private IEnumerator FadeOutLaser()
    {
        float startWidth = laserBeam.startWidth;
        float endWidth = laserBeam.endWidth;
        float timer = 0f;

        while (timer < laserFadeTime && gameObject.activeInHierarchy)
        {
            timer += Time.deltaTime;
            float progress = timer / laserFadeTime;
            float currentWidth = Mathf.Lerp(startWidth, 0f, progress);

            laserBeam.startWidth = currentWidth;
            laserBeam.endWidth = currentWidth;

            yield return null;
        }

        if (gameObject.activeInHierarchy)
        {
            laserBeam.enabled = false;
            isLaserActive = false;
            laserBeam.startWidth = startWidth;
            laserBeam.endWidth = endWidth;
        }

        fadeOutCoroutine = null;
    }

    [PunRPC]
    public void RPC_UpdateLaserPosition(Vector2 startPos, Vector2 endPos)
    {
        // 确保在正确的游戏对象上执行
        if (this == null || !this || !gameObject.activeInHierarchy) return;

        // 更新同步位置
        syncStartPos = startPos;
        syncEndPos = endPos;
        isLaserActive = true;

        // 更新激光视觉效果
        UpdateLaserVisuals();
    }

    private void OnDisable()
    {
        if (isFiring)
        {
            StopFiring();
        }
    }

    private void OnDestroy()
    {
        if (laserBeam != null)
        {
            if (laserBeam.material != null)
            {
                Destroy(laserBeam.material);
            }
            Destroy(laserBeam);
        }
    }
}