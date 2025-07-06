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
    public LayerMask wallLayer; // ����ǽ�ڲ�
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

    // ͬ��λ�ñ���
    private Vector2 syncStartPos;
    private Vector2 syncEndPos;
    private bool isLaserActive = false;

    // ʹ�û�����˺�����������ֵ
    private float DamagePerSecond => damageDrainMultiplier*damage;
    private float EnergyConsumptionPerSecond => energyCostPerShot * energyDrainMultiplier;

    protected override void Awake()
    {
        base.Awake();
        InitializeLaserBeam();

        // ��ȡ��������� PhotonView
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
        // ֻ�ñ�����ҿ�������
        if (parentPhotonView == null || !parentPhotonView.IsMine)
        {
            // ��������Ҷ���Ҫ���¼����Ӿ�Ч��
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

        // �������Ҳ��Ҫ���¼����Ӿ�Ч��
        if (isLaserActive)
        {
            UpdateLaserVisuals();
        }
    }

    protected override void HandleShootingInput()
    {
        // ����ɫ�Ƿ��������ڶԻ���
        if ((healthSystem != null && healthSystem.IsDead) ||
            (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive))
        {
            if (isFiring) StopFiring();
            return;
        }

        // ��갴��ʱ��ʼ/��������
        if (Input.GetButton("Fire1"))
        {
            // ʹ�û������������ֵ��������
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
        // ����ɿ�ʱ����ֹͣ
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

        // ֹͣ�κ����ڽ��еĵ���Ч��
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

        // ����ǹ������Ч��
        if (muzzleFlash != null && !muzzleFlash.isPlaying)
        {
            muzzleFlash.Play();
        }

        // ʹ����������� PhotonView ���� RPC
        if (myPhotonView != null)
        {
            myPhotonView.RPC("RPC_StartLaser", RpcTarget.All);
        }
        else
        {
            // ���ػ���
            RPC_StartLaser();
        }

        // ��ʼ�����˺�Э��
        if (firingCoroutine == null)
        {
            firingCoroutine = StartCoroutine(FiringCoroutine());
        }
    }

    private void StopFiring()
    {
        if (!isFiring) return;

        isFiring = false;

        // ʹ����������� PhotonView ���� RPC
        if (myPhotonView != null)
        {
            myPhotonView.RPC("RPC_StopLaser", RpcTarget.All);
        }
        else
        {
            // ���ػ���
            RPC_StopLaser();
        }

        // ֹͣ�����˺�Э��
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
            // ������������ - ʹ�û������������ֵ
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
        // ���㼤�ⷽ��
        Vector2 fireDirection = direction;

        // ִ�����߼�� - ͬʱ���ǽ�ں͵���
        RaycastHit2D hit = Physics2D.Raycast(
            muzzlePos.position,
            fireDirection,
            maxLaserDistance,
            hitLayer | wallLayer // �ϲ�����
        );

        // ȷ�����е�
        currentHitPoint = hit.collider ? hit.point : (Vector2)muzzlePos.position + fireDirection * maxLaserDistance;

        // ����ͬ��λ�ã��������ֱ��ʹ��ʵ��λ�ã�
        syncStartPos = muzzlePos.position;
        syncEndPos = currentHitPoint;

        // ���±��ؼ����Ӿ�Ч��
        UpdateLaserVisuals();

        // ��������Ч��
        UpdateImpactParticles(hit.collider != null, currentHitPoint);

        // �����˺� - ʹ�û�����˺�ֵ����ÿ���˺�
        if (hit.collider && parentPhotonView != null && parentPhotonView.IsMine)
        {
            // ����Ƿ����ǽ��
            bool isWallHit = (wallLayer.value & (1 << hit.collider.gameObject.layer)) != 0;

            // �������ǽ�ڣ���Ӧ���˺�
            if (!isWallHit)
            {
                ApplyDamage(hit.collider);
            }
        }

        // ͬ������λ�õ������ͻ���
        if (myPhotonView != null)
        {
            myPhotonView.RPC("RPC_UpdateLaserPosition", RpcTarget.Others,
                (Vector2)muzzlePos.position, currentHitPoint);
        }
        else
        {
            // ���ػ���
            RPC_UpdateLaserPosition((Vector2)muzzlePos.position, currentHitPoint);
        }
    }

    // ���¼����Ӿ�Ч����������Ҷ���Ҫ���ã�
    private void UpdateLaserVisuals()
    {
        if (!isLaserActive) return;

        laserBeam.SetPosition(0, new Vector3(syncStartPos.x, syncStartPos.y, 0));
        laserBeam.SetPosition(1, new Vector3(syncEndPos.x, syncEndPos.y, 0));

        // ��������λ��
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
            // ʹ�û�����˺�ֵ����ÿ���˺�
            float damagePerFrame = DamagePerSecond * Time.deltaTime;

            // ����bypassInvulnerable=true����
            character.photonView.RPC("TakeLaserDamage", RpcTarget.All, damagePerFrame);
        }
    }

    [PunRPC]
    public void RPC_StartLaser()
    {
        // ȷ������ȷ����Ϸ������ִ��
        if (this == null || !this || !gameObject.activeInHierarchy) return;

        // ֹͣ����Ч��������У�
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
        // ȷ������ȷ����Ϸ������ִ��
        if (this == null || !this || !gameObject.activeInHierarchy) return;

        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
        }

        // ���ڶ��󼤻�ʱ����Э��
        if (gameObject.activeInHierarchy)
        {
            fadeOutCoroutine = StartCoroutine(FadeOutLaser());
        }
        else
        {
            // ֱ�ӽ��ü���
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
        // ȷ������ȷ����Ϸ������ִ��
        if (this == null || !this || !gameObject.activeInHierarchy) return;

        // ����ͬ��λ��
        syncStartPos = startPos;
        syncEndPos = endPos;
        isLaserActive = true;

        // ���¼����Ӿ�Ч��
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